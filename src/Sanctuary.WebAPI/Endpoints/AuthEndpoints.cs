using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MiniValidation;

using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.WebAPI.Models;
using Sanctuary.WebAPI.Options;
using Sanctuary.WebAPI.Services;

using BC = BCrypt.Net.BCrypt;

namespace Sanctuary.WebAPI.Endpoints;

public static class AuthEndpoints
{
    private static ILogger _logger = null!;

    // How long a /forgot-password reset link stays valid. Matches the
    // "This link expires in 1 hour" copy in the Website's reset-password
    // email.
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

        _logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));

        app.MapPost("/login", LoginHandlerAsync);
        app.MapPost("/register", RegisterHandlerAsync);
        app.MapGet("/register/username-available", UsernameAvailableHandlerAsync);
        app.MapGet("/register/email-available", EmailAvailableHandlerAsync);
        app.MapPost("/confirm-email", ConfirmEmailHandlerAsync);
        app.MapPost("/forgot-password", ForgotPasswordHandlerAsync);
        app.MapPost("/reset-password", ResetPasswordHandlerAsync);
    }

    private static async Task<IResult> LoginHandlerAsync(
        HttpContext httpContext,
        LoginRequestModel request,
        CancellationToken cancellationToken,
        IOptionsSnapshot<WebAPIOptions> webAPIOptions,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        VpnDetectionService vpnDetectionService)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

        var clientIp = GetClientIp(httpContext);

        if (await vpnDetectionService.IsVpnOrProxyAsync(clientIp, cancellationToken))
        {
            _logger.LogWarning("Login blocked, VPN/proxy detected for IP {ClientIp}, username: {Username}", clientIp, request.Username);

            return Results.Text(
                "VPN/Proxy detected. Please turn off to continue.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == request.Username, cancellationToken);

        if (dbUser is null)
        {
            _logger.LogWarning("Login failed, user not found for username: {Username}", request.Username);

            return Results.Unauthorized();
        }

        if (!BC.Verify(request.Password, dbUser.Password))
        {
            _logger.LogWarning("Login failed, invalid password for username: {Username}", request.Username);

            return Results.Unauthorized();
        }

        // Same gate as the actual in-game login (see LoginRequestHandler.cs)
        // - an account can't do anything until its email is confirmed. Kept
        // as a distinct 403 (rather than reusing 401) so the Website can
        // show this specific message instead of "Incorrect username or
        // password."
        if (!dbUser.EmailConfirmed)
        {
            _logger.LogWarning("Login failed, email not confirmed for username: {Username}", request.Username);

            return Results.Text(
                "Please confirm your email before logging in. Check your inbox for the confirmation link.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        dbUser.Session = Guid.NewGuid().ToString("N");
        dbUser.SessionCreated = DateTimeOffset.UtcNow;

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to update session info for username: {Username}", dbUser.Username);

            return Results.InternalServerError();
        }

        return Results.Ok(new LoginResponseModel
        {
            SessionId = dbUser.Session,
            LaunchArguments = webAPIOptions.Value.LaunchArguments
        });
    }

    private static async Task<IResult> RegisterHandlerAsync(
        HttpContext httpContext,
        RegisterRequestModel request,
        CancellationToken cancellationToken,
        IOptions<WebAPIOptions> webAPIOptions,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        VpnDetectionService vpnDetectionService)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

        var clientIp = GetClientIp(httpContext);

        if (await vpnDetectionService.IsVpnOrProxyAsync(clientIp, cancellationToken))
        {
            _logger.LogWarning("Registration blocked, VPN/proxy detected for IP {ClientIp}, username: {Username}", clientIp, request.Username);

            return Results.Text(
                "VPN/Proxy detected. Please turn off to continue.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var usernameTaken = await dbContext.Users.AnyAsync(x => x.Username == request.Username, cancellationToken);

        if (usernameTaken)
        {
            _logger.LogWarning("Registration failed, username already taken {Username}", request.Username);

            return Results.Text("Username is already in use.", statusCode: StatusCodes.Status409Conflict);
        }

        var emailTaken = await dbContext.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (emailTaken)
        {
            _logger.LogWarning("Registration failed, email already in use {Email}", request.Email);

            return Results.Text("Email is already in use.", statusCode: StatusCodes.Status409Conflict);
        }

        var salt = BC.GenerateSalt();
        var hashedPassword = BC.HashPassword(request.Password, salt);

        // The Website emails this link itself (via Resend) right after this
        // call returns successfully - Sanctuary never sends email directly,
        // it only generates the token and hands it back in the response.
        var emailConfirmationToken = RandomNumberGenerator.GetHexString(64);

        var dbUser = new DbUser
        {
            Username = request.Username,
            Password = hashedPassword,
            Email = request.Email,
            IsMember = webAPIOptions.Value.MemberByDefault ?? true,
            EmailConfirmed = false,
            EmailConfirmationToken = emailConfirmationToken,
        };

        await dbContext.Users.AddAsync(dbUser, cancellationToken);

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to add new user: {Username}", request.Username);

            return Results.InternalServerError();
        }

        return Results.Ok(new { emailConfirmationToken = dbUser.EmailConfirmationToken });
    }

    private static async Task<IResult> ConfirmEmailHandlerAsync(
        ConfirmEmailRequestModel request,
        CancellationToken cancellationToken,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var dbUser = await dbContext.Users.FirstOrDefaultAsync(
            x => x.EmailConfirmationToken == request.Token, cancellationToken);

        if (dbUser is null)
        {
            _logger.LogWarning("Email confirmation failed, no user found for the given token.");

            return Results.NotFound();
        }

        dbUser.EmailConfirmed = true;
        dbUser.EmailConfirmationToken = null;

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to confirm email for username: {Username}", dbUser.Username);

            return Results.InternalServerError();
        }

        _logger.LogInformation("Email confirmed for username: {Username}", dbUser.Username);

        return Results.Ok();
    }

    private static async Task<IResult> ForgotPasswordHandlerAsync(
        ForgotPasswordRequestModel request,
        CancellationToken cancellationToken,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        // Deliberately still 200 OK with an empty body when no account
        // matches - the Website only sends the reset email when
        // passwordResetToken/username are both present, and always shows
        // the same generic "if that email is registered..." message either
        // way. Returning 404 here (or anything that looks different) would
        // let someone enumerate which emails are registered.
        if (dbUser is null)
        {
            _logger.LogWarning("Forgot-password requested for an email with no matching account: {Email}", request.Email);

            return Results.Ok();
        }

        dbUser.PasswordResetToken = RandomNumberGenerator.GetHexString(64);
        dbUser.PasswordResetTokenExpires = DateTimeOffset.UtcNow.Add(PasswordResetTokenLifetime);

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to save password reset token for username: {Username}", dbUser.Username);

            return Results.InternalServerError();
        }

        return Results.Ok(new { passwordResetToken = dbUser.PasswordResetToken, username = dbUser.Username });
    }

    private static async Task<IResult> ResetPasswordHandlerAsync(
        ResetPasswordRequestModel request,
        CancellationToken cancellationToken,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var dbUser = await dbContext.Users.FirstOrDefaultAsync(
            x => x.PasswordResetToken == request.Token, cancellationToken);

        if (dbUser is null || dbUser.PasswordResetTokenExpires is null || dbUser.PasswordResetTokenExpires < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Password reset failed, invalid or expired token.");

            return Results.NotFound();
        }

        var salt = BC.GenerateSalt();

        dbUser.Password = BC.HashPassword(request.NewPassword, salt);
        dbUser.PasswordResetToken = null;
        dbUser.PasswordResetTokenExpires = null;

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to reset password for username: {Username}", dbUser.Username);

            return Results.InternalServerError();
        }

        _logger.LogInformation("Password reset for username: {Username}", dbUser.Username);

        return Results.Ok();
    }

    // Lightweight "is this taken" checks used for as-you-type feedback on
    // the registration form, before a real /register submission ever
    // happens. Deliberately doesn't reuse MiniValidator/RegisterRequestModel
    // - an empty or malformed value here just isn't available rather than a
    // validation error, since the form itself still enforces the real
    // format rules before submit.
    private static async Task<IResult> UsernameAvailableHandlerAsync(
        string? username,
        CancellationToken cancellationToken,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Results.Ok(new { available = false });

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var taken = await dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken);

        return Results.Ok(new { available = !taken });
    }

    private static async Task<IResult> EmailAvailableHandlerAsync(
        string? email,
        CancellationToken cancellationToken,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.Ok(new { available = false });

        var dbContext = await dbContextFactory.CreateDbContextAsync();

        var taken = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);

        return Results.Ok(new { available = !taken });
    }

    // Prefers X-Forwarded-For, which the Website's own Node proxy sets to
    // the real visitor's IP before calling this API server-to-server
    // (otherwise this would only ever see the Website container's own
    // address). The Launcher hits this API directly with no proxy in
    // between, so it has no such header and falls through to the raw
    // connection address. Trusting this header is a deliberate tradeoff for
    // a project this size - it is spoofable by anyone who calls this API
    // directly with a fake header, but nothing downstream of the
    // VPN/proxy gate relies on this value for anything else.
    private static string? GetClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
