# Backend Optimization and Security Plan

## Overview

This plan outlines the phased approach to reviewing, refactoring, and hardening the KollectorScum backend API. The goal is to improve code quality, performance, and security while maintaining backwards compatibility and comprehensive test coverage.

The plan was initiated to address technical debt accumulated during incremental feature development, ensuring the backend is not only functional but also secure, maintainable, and performant.

---

## Plan Status

| Phase | Title | Status |
|-------|-------|--------|
| Phase 1 | Security Hardening | ✅ Complete |
| Phase 2 | Code Quality and Refactoring | ⏳ Pending |
| Phase 3 | Performance Optimization | ✅ Complete (see Phase 3 - Performance Optimization Summary.md) |

---

## Phase 1: Security Hardening (OWASP Top 10)

**Goal**: Implement core security controls addressing OWASP Top 10 vulnerabilities.

### Phase 1.1 – Security Response Headers (OWASP A05 – Security Misconfiguration)

- [x] Create `SecurityHeadersMiddleware` with the following headers:
  - `X-Content-Type-Options: nosniff` – prevents MIME-type sniffing
  - `X-Frame-Options: DENY` – prevents clickjacking
  - `X-XSS-Protection: 1; mode=block` – legacy XSS filter for older browsers
  - `Content-Security-Policy` – restricts resource loading
  - `Referrer-Policy: strict-origin-when-cross-origin` – limits referrer information
  - `Permissions-Policy` – disables unused browser features
- [x] Register middleware in `Program.cs`
- [x] Unit tests for `SecurityHeadersMiddleware`

### Phase 1.2 – Rate Limiting (OWASP A04 – Insecure Design)

- [x] Add global rate limiting policy: 100 requests per minute per IP
- [x] Add strict rate limiting policy for authentication endpoints: 10 requests per minute per IP
- [x] Apply `[EnableRateLimiting]` attribute to auth endpoints (login, magic link, Google OAuth)
- [x] Return `429 Too Many Requests` with `Retry-After` header when limit exceeded
- [x] Register rate limiting in `Program.cs`

### Phase 1.3 – Fix Information Disclosure (OWASP A09 – Security Logging and Monitoring)

- [x] Update `ErrorHandlingMiddleware` to suppress internal exception details in non-development environments
- [x] Return generic error messages in staging/production while logging full details server-side
- [x] Unit tests for updated `ErrorHandlingMiddleware`

### Phase 1.4 – HTTPS Enforcement (OWASP A05 – Security Misconfiguration)

- [x] Enable HSTS (HTTP Strict Transport Security) in non-development environments
- [x] Enable HTTPS redirection in non-development environments
- [x] Register in `Program.cs`

---

## Phase 2: Code Quality and Refactoring

**Goal**: Reduce technical debt, remove duplicated code, and improve maintainability.

### Phase 2.1 – Remove Deprecated Services

- [ ] Remove `IMusicReleaseService` / `MusicReleaseService` (superseded by `IMusicReleaseCommandService` and `IMusicReleaseQueryService`)
- [ ] Remove `IDataSeedingService` / `DataSeedingService` (superseded by `IDataSeedingOrchestrator`)
- [ ] Remove `IMusicReleaseImportService` / `MusicReleaseImportService` (superseded by `IMusicReleaseImportOrchestrator`)
- [ ] Update DI registrations in `Program.cs`
- [ ] Update tests to remove references to deprecated services

### Phase 2.2 – AutoMapper Integration

- [ ] Add AutoMapper NuGet package
- [ ] Create mapping profiles for all DTOs (MusicRelease, Artist, Genre, Label, etc.)
- [ ] Replace manual property mapping in `MusicReleaseMapperService` and other services
- [ ] Update tests to cover mapping profiles

### Phase 2.3 – Specification Pattern for Queries

- [ ] Create `ISpecification<T>` interface
- [ ] Implement specifications for common queries (e.g., `MusicReleasesByUserSpecification`, `ArtistByNameSpecification`)
- [ ] Update repository layer to accept specifications
- [ ] Update `MusicReleaseQueryBuilder` to use specifications
- [ ] Unit tests for specification implementations

### Phase 2.4 – Consolidate Error Handling

- [ ] Standardise error response format using `ProblemDetails` (RFC 7807)
- [ ] Replace ad-hoc error returns in controllers with `ProblemDetails`
- [ ] Update Swagger documentation to reflect standardised error responses
- [ ] Update relevant controller tests

---

## Phase 3: Performance Optimization ✅ Complete

See [Phase 3 - Performance Optimization Summary.md](Phase%203%20-%20Performance%20Optimization%20Summary.md) for details.

### Summary of Completed Work:
- **Phase 3.2** – Response caching for lookup data (5-minute TTL, user-scoped, group invalidation)
- **Phase 3.4** – N+1 query fix in music release listing (batch loading reducing ~150 queries to 2)

### Remaining (Optional):
- [ ] Phase 3.1 – AutoMapper to reduce manual mapping boilerplate (tracked in Phase 2.2)
- [ ] Phase 3.3 – Specification pattern for composable query specs (tracked in Phase 2.3)
- [ ] Distributed cache (Redis) for multi-instance deployments (future)
- [ ] Cache warming on startup for frequently-accessed lookup data (future)

---

## OWASP Top 10 Coverage Matrix

| OWASP Risk | Description | Mitigation |
|---|---|---|
| A01 – Broken Access Control | Unauthorised data access | Multi-tenant user scoping, JWT auth, `[Authorize]` attributes |
| A02 – Cryptographic Failures | Weak encryption | JWT HS256 with configurable key, HTTPS via HSTS |
| A03 – Injection | SQL injection, etc. | EF Core parameterised queries, `SqlValidationService` for NL queries |
| A04 – Insecure Design | DoS, brute force | Rate limiting (Phase 1.2) |
| A05 – Security Misconfiguration | Missing headers, verbose errors | Security headers (Phase 1.1), error sanitisation (Phase 1.3), HSTS (Phase 1.4) |
| A06 – Vulnerable Components | Outdated packages | NuGet package references kept up-to-date |
| A07 – Identification and Auth Failures | Weak auth | JWT validation, magic link expiry, user existence check on each request |
| A08 – Software and Data Integrity | Tampered payloads | FluentValidation on all inputs, JWT signature validation |
| A09 – Security Logging and Monitoring | Missing audit trail | Structured logging, suppressed verbose errors in production |
| A10 – Server-Side Request Forgery | Internal service calls | HTTP client factory, no user-controlled URLs in outbound calls |
