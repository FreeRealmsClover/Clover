# FreeRealms: Clover

Clover's server emulator, a C# implementation of the Free Realms server, running as a Docker Compose stack of a login server, a gateway/world server, and a web API used by the [FreeRealms: Clover website](https://freerealmsclover.com) and launcher.

This is the actual source powering the live Clover server. It's open source under AGPL-3.0 - see [LICENSE](LICENSE) - and contributions are welcome.

## Services

| Service | Purpose | Port |
|---|---|---|
| `sanctuary.mysql` | MariaDB database | internal only — not exposed publicly |
| `sanctuary.webapi` | HTTP API used by the website/launcher (login, registration, portraits) | 20040/tcp |
| `sanctuary.gateway` | World/zone server the game client connects to after login | 20260/udp |
| `sanctuary.login` | Login/auth server | 20041-20042/udp |

## Getting Started

### Prerequisites

- Docker and Docker Compose
- A copy of `.env` with the required secrets (see below) — never commit this file

### Setup

1. Clone the repo:
   ```sh
   git clone https://github.com/FreeRealmsClover/Clover.git
   cd Clover
   ```
2. Create `src/Docker/.env` with the following, each set to your own generated values:
   ```
   MYSQL_ROOT_PASSWORD=<generate with e.g. openssl rand -base64 24>
   MYSQL_PASSWORD=<generate with e.g. openssl rand -base64 24>
   LOGIN_GATEWAY_CHALLENGE=<generate with e.g. openssl rand -hex 32>
   ```
3. From `src/Docker`:
   ```sh
   docker compose up -d
   ```
4. Point a Free Realms client and launcher at your server's login port.

### Production checklist

If you're standing this up for real players rather than local development:

- Put a TLS-terminating reverse proxy in front of `sanctuary.webapi` — don't expose it raw.
- Back up the `sanctuary.mysql` data volume regularly.
- Never expose MariaDB's port (3306) publicly — it's internal-only in this compose file by design, keep it that way.
- Set real, unique values for every secret in `.env` — don't reuse the examples above.

## Contributing

Contributions are welcome — this is meant to be a real community project, not just Clover's private codebase.

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes
4. Open a pull request describing what changed and why

A few notes for contributors:

- .NET 9 is the target framework.
- Keep pull requests focused — one fix or feature per PR is much easier to review than a large mixed changeset.
- Open an issue before starting on a large or architectural change, so it can be discussed first.

## Credits

Built on top of the open source [Sanctuary](https://github.com/Open-Source-Free-Realms/Sanctuary) Free Realms server emulator by Open Source Free Realms, licensed under AGPL-3.0. This repository is Clover's own actively developed codebase, not a passive mirror - see [LICENSE](LICENSE) for full attribution.

## License

AGPL-3.0 — see [LICENSE](LICENSE) for the full text.
