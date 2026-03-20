import { fetchJson, toDiscogsProxyUrl } from '../lib/api';

describe('fetchJson — impersonation header injection', () => {
  let mockFetch: jest.Mock;

  beforeEach(() => {
    // Clear impersonation key (auth_token is set by jest.setup.ts beforeEach)
    localStorage.removeItem('impersonation_userId');

    mockFetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({}),
      headers: { get: jest.fn().mockReturnValue('application/json') },
    });

    // Replace the global fetch used by api.ts
    (global as unknown as { fetch: jest.Mock }).fetch = mockFetch;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('fetchJson_withImpersonationInLocalStorage_includesXAdminActAsHeader', async () => {
    localStorage.setItem('impersonation_userId', 'impersonated-user-id');

    await fetchJson('/api/test');

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, options] = mockFetch.mock.calls[0] as [string, RequestInit & { headers: Record<string, string> }];
    expect((options.headers as Record<string, string>)['X-Admin-Act-As']).toBe('impersonated-user-id');
  });

  it('fetchJson_withoutImpersonationInLocalStorage_doesNotIncludeXAdminActAsHeader', async () => {
    // impersonation_userId is not set — ensure it's absent
    localStorage.removeItem('impersonation_userId');

    await fetchJson('/api/test');

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, options] = mockFetch.mock.calls[0] as [string, RequestInit & { headers: Record<string, string> }];
    expect((options.headers as Record<string, string>)['X-Admin-Act-As']).toBeUndefined();
  });

  it('fetchJson_withImpersonation_stillIncludesAuthorizationHeader', async () => {
    // auth_token is 'test-token' from jest.setup.ts beforeEach
    localStorage.setItem('impersonation_userId', 'impersonated-user-id');

    await fetchJson('/api/test');

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, options] = mockFetch.mock.calls[0] as [string, RequestInit & { headers: Record<string, string> }];
    const headers = options.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer test-token');
    expect(headers['X-Admin-Act-As']).toBe('impersonated-user-id');
  });
});

describe('toDiscogsProxyUrl', () => {
  it('returns a proxy path for i.discogs.com URLs', () => {
    const input = 'https://i.discogs.com/abc/image.jpg';
    const result = toDiscogsProxyUrl(input);
    // In the test environment NEXT_PUBLIC_API_BASE_URL is not set so API_BASE_URL is ''
    expect(result).toBe('/api/images/proxy?url=https%3A%2F%2Fi.discogs.com%2Fabc%2Fimage.jpg');
  });

  it('returns a proxy path preserving query parameters', () => {
    const input = 'https://i.discogs.com/thumb/abc.jpg?token=xyz&size=thumb';
    const result = toDiscogsProxyUrl(input);
    expect(result).toContain('/api/images/proxy?url=');
    expect(result).toContain(encodeURIComponent(input));
  });

  it('returns non-Discogs URLs unchanged', () => {
    const input = 'https://example.com/cover.jpg';
    expect(toDiscogsProxyUrl(input)).toBe(input);
  });

  it('returns undefined for null input', () => {
    expect(toDiscogsProxyUrl(null)).toBeUndefined();
  });

  it('returns undefined for undefined input', () => {
    expect(toDiscogsProxyUrl(undefined)).toBeUndefined();
  });

  it('returns undefined for empty string', () => {
    expect(toDiscogsProxyUrl('')).toBeUndefined();
  });

  it('proxy path routes through the backend images/proxy endpoint', () => {
    const input = 'https://i.discogs.com/test.jpg';
    const result = toDiscogsProxyUrl(input);
    // Must contain the backend proxy path so static-SPA deployments (e.g. Cloudflare
    // Pages) use the Render backend rather than a non-existent Next.js API route.
    expect(result).toContain('/api/images/proxy?url=');
    expect(result).toContain(encodeURIComponent(input));
  });
});
