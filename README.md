# Clover Server

**Public/open-source.** Setup notes for running the
[Sanctuary](https://github.com/Open-Source-Free-Realms/Sanctuary)
FreeRealms emulator for Clover.

This repo doesn't ship its own Docker config — Sanctuary has its own,
real one at `Sanctuary/src/Docker/docker-compose.yml`, confirmed working.
Use that directly instead.

## Setup

1. Clone Sanctuary alongside this repo:
   git clone https://github.com/Open-Source-Free-Realms/Sanctuary.git
2. Edit `Sanctuary/src/Docker/docker-compose.yml`: find the two
   `Server__ServerAddress=localhost:20260` lines (shared via the
   `common-env` anchor) and change `localhost` to your VPS's real IP or
   domain.
3. **Security note:** the file as shipped exposes MariaDB's port 3306
   to the public internet for no reason — nothing outside the other
   containers needs to reach it directly. Remove the `ports: - "3306:3306"`
   line under the `sanctuary.mysql` service before deploying.
4. From `Sanctuary/src/Docker/`, run: docker compose up -d --build


## Confirmed real values (as of first successful deploy)

- Uses **MariaDB**, not SQLite, despite what Sanctuary's own top-level
  README suggests for local dev
- Four services: `sanctuary.mysql`, `sanctuary.webapi`,
  `sanctuary.gateway`, `sanctuary.login`
- **Client-facing ports** (open these in your firewall):
  - `20042/udp` — Login (players connect here first, for auth)
  - `20260/udp` — Gateway (players connect here after login succeeds)
- `20041/udp` is internal only (Login↔Gateway service-to-service) — not
  something a player's client touches directly, no need to expose it
  beyond what's needed for the containers to reach each other
- `20040/tcp` — WebAPI

## Production checklist

- [x] Confirmed real ports/services against actual container logs
- [x] Removed public MariaDB port exposure
- [ ] Put this behind TLS/a reverse proxy rather than serving plain
      traffic in production
- [ ] Set up regular MariaDB backups (a real DB now, not a flat SQLite
      file — back it up like one)