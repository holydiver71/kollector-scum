# kollector-scrum

kollector-scrum is a music collection web app used to catalogue a user's music collection.

## Getting Started

See the full Getting Started guide in [documentation/Getting Started.md](documentation/Getting%20Started.md).
# or: ./backend/scripts/run-api-local.sh --local
```

- Install and run frontend:

```bash
cd frontend
npm ci
npm run dev
```

Or from the repo root:

```bash
npm --prefix frontend ci
npm --prefix frontend run dev
```

Security note: do not commit your real `.env`. Use GitHub Secrets for CI and only export secrets to local files temporarily.
