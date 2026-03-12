import { impersonateUser } from '../lib/admin';
import * as api from '../lib/api';

jest.mock('../lib/api', () => ({
  fetchJson: jest.fn(),
}));

const mockFetchJson = api.fetchJson as jest.Mock;

describe('admin.ts — impersonateUser', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('impersonateUser_makesPostToCorrectEndpoint', async () => {
    mockFetchJson.mockResolvedValue({ userId: 'some-id', email: 'user@example.com' });

    await impersonateUser('some-id');

    expect(mockFetchJson).toHaveBeenCalledWith(
      '/api/admin/impersonate/some-id',
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('impersonateUser_returnsUserDataOnSuccess', async () => {
    const userData = { userId: 'user-1', email: 'user@example.com', displayName: 'John Doe' };
    mockFetchJson.mockResolvedValue(userData);

    const result = await impersonateUser('user-1');

    expect(result).toEqual(userData);
  });

  it('impersonateUser_throwsOnForbiddenResponse', async () => {
    const error = Object.assign(new Error('Forbidden'), { status: 403 });
    mockFetchJson.mockRejectedValue(error);

    await expect(impersonateUser('user-1')).rejects.toThrow('Forbidden');
  });

  it('impersonateUser_throwsOnNotFoundResponse', async () => {
    const error = Object.assign(new Error('Not Found'), { status: 404 });
    mockFetchJson.mockRejectedValue(error);

    await expect(impersonateUser('user-1')).rejects.toThrow('Not Found');
  });
});
