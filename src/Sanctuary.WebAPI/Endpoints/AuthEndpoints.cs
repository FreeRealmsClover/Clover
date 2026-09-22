using System;
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

using BC = BCrypt.Net.BCrypt;

namespace Sanctuary.WebAPI.Endpoints;

public static class AuthEndpoints
{
    private static ILogger _logger = null!;

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

        _logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));

        app.MapPost("/login", LoginHandlerAsync);
        app.MapPost("/register", RegisterHandlerAsync);
        app.MapGet("/register/username-available", UsernameAvailableHandlerAsync);
        app.MapGet("/register/email-available", EmailAvailableHandlerAsync);
    }

    private static async Task<IResult> LoginHandlerAsync(
        LoginRequestModel request,
        CancellationToken cancellationToken,
        IOptionsSnapshot<WebAPIOptions> webAPIOptions,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

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
        RegisterRequestModel request,
        CancellationToken cancellationToken,
        IOptions<WebAPIOptions> webAPIOptions,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
            return Results.ValidationProblem(errors);

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

        var dbUser = new DbUser
        {
            Username = request.Username,
            Password = hashedPassword,
            Email = request.Email,
            IsMember = webAPIOptions.Value.MemberByDefault ?? true,
        };

        await dbContext.Users.AddAsync(dbUser, cancellationToken);

        if (await dbContext.SaveChangesAsync(cancellationToken) <= 0)
        {
            _logger.LogError("Failed to add new user: {Username}", request.Username);

            return Results.InternalServerError();
        }

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
}