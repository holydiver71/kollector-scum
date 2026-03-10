import { requestMagicLink, verifyMagicLink, getAuthToken, clearAuthToken } from '../auth';

// Mock the api module
jest.mock('../api', () => ({
  fetchJson: jest.fn(),
}));

import { fetchJson } from '../api';

const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

describe('Magic Link Auth', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Clear localStorage between tests
    localStorage.clear();
  });

  describe('requestMagicLink', () => {
    it('calls the correct API endpoint with the email', async () => {
      mockFetchJson.mockResolvedValueOnce({ message: 'If your email is registered...' });

      await requestMagicLink('user@example.com');

      expect(mockFetchJson).toHaveBeenCalledWith(
        '/api/auth/magic-link/request',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ email: 'user@example.com' }),
        })
      );
    });

    it('does not throw on successful response', async () => {
      mockFetchJson.mockResolvedValueOnce({ message: 'Email sent' });

      await expect(requestMagicLink('user@example.com')).resolves.not.toThrow();
    });

    it('propagates errors from the API', async () => {
      mockFetchJson.mockRejectedValueOnce(new Error('Network error'));

      await expect(requestMagicLink('user@example.com')).rejects.toThrow('Network error');
    });
  });

  describe('verifyMagicLink', () => {
    it('calls the correct API endpoint with the token', async () => {
      const mockResponse = {
        token: 'jwt-token-xyz',
        profile: {
          userId: 'user-id-123',
          email: 'user@example.com',
          displayName: 'Test User',
          selectedTheme: 'dark',
          isAdmin: false,
        },
      };

      mockFetchJson.mockResolvedValueOnce(mockResponse);

      await verifyMagicLink('my-magic-token');

      expect(mockFetchJson).toHaveBeenCalledWith(
        '/api/auth/magic-link/verify',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ token: 'my-magic-token' }),
        })
      );
    });

    it('stores the JWT token in localStorage after successful verification', async () => {
      const jwtToken = 'jwt-token-stored';
      const mockResponse = {
        token: jwtToken,
        profile: {
          userId: 'user-id-123',
          email: 'user@example.com',
          selectedTheme: 'dark',
          isAdmin: false,
        },
      };

      mockFetchJson.mockResolvedValueOnce(mockResponse);

      await verifyMagicLink('my-magic-token');

      expect(getAuthToken()).toBe(jwtToken);
    });

    it('returns the full auth response', async () => {
      const mockResponse = {
        token: 'jwt-token',
        profile: {
          userId: 'abc',
          email: 'user@example.com',
          selectedTheme: 'dark',
          isAdmin: true,
        },
      };

      mockFetchJson.mockResolvedValueOnce(mockResponse);

      const result = await verifyMagicLink('token-123');

      expect(result.token).toBe('jwt-token');
      expect(result.profile.email).toBe('user@example.com');
      expect(result.profile.isAdmin).toBe(true);
    });

    it('does not store a token on failure', async () => {
      clearAuthToken();
      mockFetchJson.mockRejectedValueOnce(new Error('Invalid token'));

      await expect(verifyMagicLink('bad-token')).rejects.toThrow('Invalid token');

      expect(getAuthToken()).toBeNull();
    });
  });
});
