import os, subprocess, json, requests
import anthropic

def get_diff(base_sha, head_sha):
    result = subprocess.run(
        ["git", "diff", f"{base_sha}..{head_sha}",
         "--", "*.cs", "*.tsx", "*.ts"],
        capture_output=True, text=True
    )
    diff = result.stdout
    return diff[:6000] + ("\n...[truncated]" if len(diff) > 6000 else "")

def get_coverage_summary():
    path = "./coverage-report/Summary.txt"
    if os.path.exists(path):
        with open(path) as f:
            return f.read()[:2000]
    return "Coverage report not available."

def get_changed_files(base_sha, head_sha):
    result = subprocess.run(
        ["git", "diff", "--name-only", f"{base_sha}..{head_sha}"],
        capture_output=True, text=True
    )
    return result.stdout.strip()

def build_prompt(diff, coverage, changed_files):
    return f"""You are a senior engineer reviewing a pull request for Kollector Scum —
a multi-tenant vinyl/CD collection catalog SaaS.

Stack: .NET 8 (Clean Architecture) + Next.js 15 (App Router) + PostgreSQL (Supabase)
Architecture: Controllers → Services → Repositories → EF Core → PostgreSQL
Multi-tenancy: EVERY user-owned entity has UserId: Guid via IUserOwnedEntity.
  GenericCrudService<T> auto-scopes ALL queries by IUserContext. This is the
  most critical invariant — a missing UserId scope leaks cross-tenant data.
Auth: Google OAuth 2.0 → JWT (NameIdentifier = userId, IsAdmin role).
Storage: Cloudflare R2 — path format: cover-art-{{env}}/{{userId}}/{{objectName}}
Discogs: Async import queue (DiscogsImportBackgroundService), 60 req/min rate limit.
Testing: Moq for unit tests; WebApplicationFactory + TestAuthHandler for integration.

## Changed Files
{changed_files}

## Code Diff
```
{diff}
```

## Test Coverage Summary
{coverage}

## Review — cover ALL sections. Say "Nothing to flag." if clean.

### 1. 🏗️ Architecture & Code Quality
- Clean Architecture layering — no controller accessing a repo directly
- SOLID violations, unnecessary complexity, missing XML doc comments
- FluentValidation rules present for all new DTO inputs?

### 2. 🔒 Multi-Tenancy & Security  [CRITICAL]
- Any query missing .Where(x => x.UserId == userId) or GenericCrudService scoping?
- Any endpoint reachable without [Authorize]?
- Injection risks (SQL, XSS, command injection), hardcoded secrets

### 3. 🧪 Test Coverage
- Which new code paths lack test coverage?
- Are new service methods tested with Moq? Integration paths with WebApplicationFactory?
- Suggest 2–3 xUnit tests: Should_ExpectedResult_When_Condition naming

### 4. 🎵 Domain Logic (Vinyl/CD Catalog)
- Lookup tables (Artist, Genre, Label, Format, Country, Packaging, Store) per-user?
- Composite unique constraint (UserId, Name) respected?
- Discogs job state transitions correct? R2 path format correct?

### 5. ⚡ Performance
- N+1 risks in EF Core — missing .Include() or .ThenInclude()?
- Pagination applied before .ToList() / materialisation?
- 5-minute cache TTL respected for lookup tables in GenericCrudService?

### 6. 📋 Summary
Rate: APPROVE / REQUEST_CHANGES / COMMENT — one sentence justification.
"""

def call_claude(prompt):
    client = anthropic.Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
    msg = client.messages.create(
        model="claude-haiku-4-5-20251001",
        max_tokens=1500,
        messages=[{"role": "user", "content": prompt}]
    )
    return msg.content[0].text

def post_pr_comment(repo, pr_number, body):
    if not pr_number:
        print("No PR number — skipping comment.")
        return
    url = f"https://api.github.com/repos/{repo}/issues/{pr_number}/comments"
    headers = {
        "Authorization": f"Bearer {os.environ['GITHUB_TOKEN']}",
        "Accept": "application/vnd.github+json"
    }
    comment = f"## 🤖 Claude Code Review\n\n{body}\n\n---\n*claude-haiku-4-5 · Kollector Scum CI*"
    resp = requests.post(url, headers=headers, json={"body": comment})
    print(f"Posted comment: {resp.status_code}")

if __name__ == "__main__":
    base_sha = os.environ.get("BASE_SHA", "HEAD~1")
    head_sha = os.environ.get("HEAD_SHA", "HEAD")
    repo     = os.environ.get("REPO",     "")
    pr_num   = os.environ.get("PR_NUMBER", "")

    diff     = get_diff(base_sha, head_sha)
    coverage = get_coverage_summary()
    files    = get_changed_files(base_sha, head_sha)
    prompt   = build_prompt(diff, coverage, files)
    review   = call_claude(prompt)

    print("=== Claude Review ===")
    print(review)
    post_pr_comment(repo, pr_num, review)