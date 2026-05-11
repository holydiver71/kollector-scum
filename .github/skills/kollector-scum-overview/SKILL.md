---
name: kollector-scum-overview
version: 0.2.0
description: Overview of Kollector Scum repository, stack and agent priorities
tags: [copilot-skill, overview]
owner: @holydiver71
stability: stable
scope: repo-summary, stack, high-priority rules
entry_point: .github/skills/kollector-scum-overview/SKILL.md
last_updated: 2026-04-26
intent: [repo-orientation, priorities, architecture-highlights]
---

# Overview

Repo identity

- Full-stack music collection SaaS; invitation-only; fully multi-tenant.

Stack

- Backend: .NET 8 Web API (C#)
- Frontend: Next.js 15 App Router (TypeScript)
- DB: PostgreSQL (Supabase)
- Storage: Cloudflare R2; public serving via Cloudflare Worker

Purpose

- Catalog music releases, manage per-user lookup data, import from Discogs, serve cover art, provide stats/search.

What an agent should optimize for

- Preserve tenant isolation first, layering second.
- Keep DTO, validation and Swagger aligned.
- Prefer existing generic patterns; follow service/repository abstractions.
