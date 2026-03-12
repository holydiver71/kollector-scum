import { fetchJson } from '../lib/api';

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
