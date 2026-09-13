# Clover Server

**Public/open-source.** Docker Compose setup for running the
[Sanctuary](https://github.com/Open-Source-Free-Realms/Sanctuary)
FreeRealms emulator for Clover.

This repo is just the game server. Launcher-side tooling (manifest
generation, news/content hosting) lives in `Launcher`'s
`content-server/` folder instead, since that's launcher support
infrastructure, not part of Sanctuary itself.

## Setup

1. Clone Sanctuary alongside this config:
   ```
   git clone https://github.com/Open-Source-Free-Realms/Sanctuary.git
   ```
2. Confirm the Login/Gateway ports in `docker-compose.yml` match
   Sanctuary's actual config — the values here are placeholders until
   verified against the real Sanctuary source.
3. `docker compose up`

## Production checklist

- [ ] Confirm Sanctuary's real Login/Gateway ports and update
      `docker-compose.yml`
- [ ] Point DNS for your domain at this VPS
- [ ] Put this behind TLS rather than serving plain traffic in production
