---
name: kollector-scum-frontend
version: 0.2.0
description: Frontend conventions and contribution guidance
tags: [copilot-skill, frontend, nextjs]
owner: @holydiver71
stability: stable
scope: Next.js app router, auth helpers, API conventions
entry_point: .github/skills/kollector-scum-frontend/SKILL.md
last_updated: 2026-04-26
intent: [frontend-guidance, auth-integration, api-patterns]
---

# Frontend Guidance

- Next.js 15 App Router. Routes under frontend/app/.
- Centralize API calls in frontend/app/lib/api.ts; reuse fetchJson.
- Shared auth helpers in frontend/app/lib/auth.ts. Do not invent alternate token stores.
- Auth token key: auth_token (localStorage).
- fetchJson attaches Authorization: Bearer <token> and X-Admin-Act-As when impersonation is active.

Safe contribution rule

- If frontend needs new backend data: add DTO, update backend endpoint, expose via fetchJson, then update component.

State guidance

- No global Redux/Zustand. Use React context and local component state.
