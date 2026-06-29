import os, glob, anthropic

def collect_source_signatures():
    signatures = []
    for f in glob.glob("backend/KollectorScum.Api/**/*.cs", recursive=True):
        with open(f) as fh:
            lines = fh.readlines()
        sigs = [l.strip() for l in lines
                if "public" in l and ("void" in l or "Task" in l
                    or "IEnumerable" in l or "class " in l
                    or "interface " in l)]
        if sigs:
            signatures.append(f"// {f}\n" + "\n".join(sigs[:20]))
    return "\n\n".join(signatures)[:4000]

def collect_existing_tests():
    tests = []
    for f in glob.glob("backend/KollectorScum.Tests/**/*.cs", recursive=True):
        with open(f) as fh:
            lines = fh.readlines()
        test_names = [l.strip() for l in lines
                      if "[Fact]" in l or "[Theory]" in l
                      or ("public" in l and "Task" in l)]
        tests.extend(test_names[:30])
    return "\n".join(tests)[:2000]

client = anthropic.Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
source = collect_source_signatures()
tests  = collect_existing_tests()

msg = client.messages.create(
    model="claude-haiku-4-5-20251001",
    max_tokens=800,
    messages=[{"role": "user", "content": f"""
You are a test coverage analyst for Kollector Scum — a multi-tenant vinyl/CD
catalog SaaS. Target coverage: 80%+.

Priority areas to find gaps:
- UserId scoping in all service methods (multi-tenancy correctness)
- GenericCrudService<T> — pagination, search, 5-min cache TTL
- DiscogsImportBackgroundService — job state transitions
- CloudflareR2StorageService — upload paths, error handling
- IUserContext — JWT claim extraction

Source signatures:
{source}

Existing test methods:
{tests}

List the 5 most important untested methods. For each:
- Method name and file path
- Why it matters (especially if multi-tenancy-related)
- A concrete xUnit test:
  public async Task Should_ExpectedResult_When_Condition()
"""}]
)
print(msg.content[0].text)