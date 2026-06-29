import os, subprocess, json, requests
import anthropic

CATEGORY_PRIORITY = {
    "security":        1,
    "bug":             2,
    "performance":     3,
    "maintainability": 4,
    "style":           5,
}

LABELS = [
    {"name": "review: security",        "color": "d73a4a", "description": "Security finding raised by automated code review"},
    {"name": "review: bug",             "color": "e4694a", "description": "Bug or correctness issue raised by automated code review"},
    {"name": "review: performance",     "color": "f0c040", "description": "Performance issue raised by automated code review"},
    {"name": "review: maintainability", "color": "0075ca", "description": "Maintainability finding raised by automated code review"},
    {"name": "review: style",           "color": "cfd3d7", "description": "Style/cosmetic issue raised by automated code review"},
]

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

Respond ONLY with valid JSON in this exact shape — no markdown fences, no explanation:
{{
  "findings": [
    {{
      "category": "security|bug|performance|maintainability|style",
      "title": "One-line issue title (max 80 chars)",
      "body": "Detailed description. Reference file paths and line numbers where visible."
    }}
  ],
  "summary": "One paragraph overall review summary.",
  "verdict": "APPROVE|REQUEST_CHANGES|COMMENT"
}}
If there are no findings in a category, omit those entries. If there are no findings at all, return an empty findings array.
"""

def call_claude(prompt):
    client = anthropic.Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
    msg = client.messages.create(
        model="claude-haiku-4-5-20251001",
        max_tokens=2000,
        messages=[{"role": "user", "content": prompt}]
    )
    return msg.content[0].text

def gh_headers():
    return {
        "Authorization": f"Bearer {os.environ['GITHUB_TOKEN']}",
        "Accept": "application/vnd.github+json"
    }

def ensure_labels(repo):
    resp = requests.get(
        f"https://api.github.com/repos/{repo}/labels?per_page=100",
        headers=gh_headers()
    )
    existing = {l["name"] for l in resp.json()} if resp.ok else set()
    for label in LABELS:
        if label["name"] not in existing:
            r = requests.post(
                f"https://api.github.com/repos/{repo}/labels",
                headers=gh_headers(),
                json=label
            )
            print(f"Created label '{label['name']}': {r.status_code}")
        else:
            print(f"Label '{label['name']}' already exists")

def create_issue(repo, pr_number, finding):
    category = finding.get("category", "style")
    pr_ref = f" PR #{pr_number}" if pr_number else ""
    title = f"[{category.capitalize()}]{pr_ref}: {finding['title']}"
    body = (
        f"{finding['body']}\n\n"
        f"---\n"
        f"_Raised by Claude review{' on PR #' + str(pr_number) if pr_number else ''}._"
    )
    resp = requests.post(
        f"https://api.github.com/repos/{repo}/issues",
        headers=gh_headers(),
        json={"title": title, "body": body, "labels": [f"review: {category}"]}
    )
    if resp.ok:
        print(f"Created issue: {resp.json()['html_url']}")
    else:
        print(f"Failed to create issue: {resp.status_code} {resp.text}")

def create_issues_from_findings(repo, pr_number, findings):
    if not repo:
        print("No REPO — skipping issue creation.")
        return
    sorted_findings = sorted(
        findings,
        key=lambda f: CATEGORY_PRIORITY.get(f.get("category", "style"), 5)
    )
    for finding in sorted_findings:
        create_issue(repo, pr_number, finding)

def post_pr_comment(repo, pr_number, summary, verdict, issue_count):
    if not pr_number:
        print("No PR number — skipping comment.")
        return
    issue_note = (
        f"\n\n**{issue_count} issue(s) raised** — see the "
        f"[Issues tab](https://github.com/{repo}/issues) for details."
        if issue_count > 0 else "\n\nNo actionable issues found."
    )
    comment = (
        f"## 🤖 Claude Code Review\n\n"
        f"**Verdict: {verdict}**\n\n"
        f"{summary}"
        f"{issue_note}\n\n"
        f"---\n*claude-haiku-4-5 · Kollector Scum CI*"
    )
    resp = requests.post(
        f"https://api.github.com/repos/{repo}/issues/{pr_number}/comments",
        headers=gh_headers(),
        json={"body": comment}
    )
    print(f"Posted comment: {resp.status_code}")

if __name__ == "__main__":
    base_sha = os.environ.get("BASE_SHA",  "HEAD~1")
    head_sha = os.environ.get("HEAD_SHA",  "HEAD")
    repo     = os.environ.get("REPO",      "")
    pr_num   = os.environ.get("PR_NUMBER", "")

    diff     = get_diff(base_sha, head_sha)
    coverage = get_coverage_summary()
    files    = get_changed_files(base_sha, head_sha)
    prompt   = build_prompt(diff, coverage, files)
    raw      = call_claude(prompt)

    print("=== Claude Raw Response ===")
    print(raw)

    try:
        data     = json.loads(raw)
        findings = data.get("findings", [])
        summary  = data.get("summary", "")
        verdict  = data.get("verdict", "COMMENT")
        print(f"Parsed {len(findings)} finding(s). Verdict: {verdict}")
        ensure_labels(repo)
        create_issues_from_findings(repo, pr_num, findings)
        post_pr_comment(repo, pr_num, summary, verdict, len(findings))
    except json.JSONDecodeError as e:
        print(f"WARNING: JSON parse failed ({e}) — falling back to plain PR comment.")
        post_pr_comment(repo, pr_num, raw, "COMMENT", 0)
