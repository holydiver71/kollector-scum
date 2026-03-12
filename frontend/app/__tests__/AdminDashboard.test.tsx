import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import AdminDashboard from '../components/AdminDashboard';
import {
  getInvitations,
  getUsers,
  impersonateUser,
} from '../lib/admin';
import { useImpersonation } from '../contexts/ImpersonationContext';

jest.mock('../lib/admin', () => ({
  getInvitations: jest.fn(),
  createInvitation: jest.fn(),
  deleteInvitation: jest.fn(),
  activateInvitation: jest.fn(),
  getUsers: jest.fn(),
  revokeUserAccess: jest.fn(),
  impersonateUser: jest.fn(),
}));

jest.mock('../contexts/ImpersonationContext', () => ({
  useImpersonation: jest.fn(),
}));

const mockUseImpersonation = useImpersonation as jest.Mock;
const mockGetInvitations = getInvitations as jest.Mock;
const mockGetUsers = getUsers as jest.Mock;
const mockImpersonateUser = impersonateUser as jest.Mock;

const defaultNonAdminUser = {
  userId: 'user-1',
  email: 'user@example.com',
  displayName: 'Regular User',
  createdAt: '2024-01-01T00:00:00Z',
  isAdmin: false,
};

const defaultAdminUser = {
  userId: 'admin-1',
  email: 'admin@example.com',
  displayName: 'Admin User',
  createdAt: '2024-01-01T00:00:00Z',
  isAdmin: true,
};

describe('AdminDashboard — impersonation', () => {
  let mockStartImpersonation: jest.Mock;

  beforeEach(() => {
    jest.clearAllMocks();
    mockStartImpersonation = jest.fn();
    mockUseImpersonation.mockReturnValue({ startImpersonation: mockStartImpersonation });
    mockGetInvitations.mockResolvedValue([]);
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);
    window.confirm = jest.fn(() => true);
  });

  it('impersonateButton_visibleForNonAdminUsers', async () => {
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Impersonate')).toBeInTheDocument();
    });
  });

  it('impersonateButton_notVisibleForAdminUsers', async () => {
    mockGetUsers.mockResolvedValue([defaultAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      // Wait for loading to finish by checking that the user email is rendered
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    });

    expect(screen.queryByText('Impersonate')).not.toBeInTheDocument();
  });

  it('impersonateButton_notVisibleWhileLoading', () => {
    // Never-resolving promises keep the component in the loading state
    mockGetInvitations.mockImplementation(() => new Promise(() => {}));
    mockGetUsers.mockImplementation(() => new Promise(() => {}));

    render(<AdminDashboard />);

    expect(screen.queryByText('Impersonate')).not.toBeInTheDocument();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('clickingImpersonate_callsImpersonateUserApiWithCorrectUserId', async () => {
    mockImpersonateUser.mockResolvedValue({ userId: 'user-1', email: 'user@example.com' });
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Impersonate')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Impersonate'));

    await waitFor(() => {
      expect(mockImpersonateUser).toHaveBeenCalledWith('user-1');
    });
  });

  it('clickingImpersonate_onSuccess_callsStartImpersonation', async () => {
    const returnedUser = { userId: 'user-1', email: 'user@example.com', displayName: 'Regular User' };
    mockImpersonateUser.mockResolvedValue(returnedUser);
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Impersonate')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Impersonate'));

    await waitFor(() => {
      expect(mockStartImpersonation).toHaveBeenCalledWith(returnedUser);
    });
  });

  it('clickingImpersonate_onApiError_displaysErrorMessage', async () => {
    mockImpersonateUser.mockRejectedValue(new Error('You are not authorized to impersonate this user'));
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Impersonate')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Impersonate'));

    await waitFor(() => {
      expect(screen.getByText('You are not authorized to impersonate this user')).toBeInTheDocument();
    });
  });

  it('clickingImpersonate_onApiError_doesNotCallStartImpersonation', async () => {
    mockImpersonateUser.mockRejectedValue(new Error('Forbidden'));
    mockGetUsers.mockResolvedValue([defaultNonAdminUser]);

    render(<AdminDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Impersonate')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Impersonate'));

    await waitFor(() => {
      expect(screen.getByText('Forbidden')).toBeInTheDocument();
    });

    expect(mockStartImpersonation).not.toHaveBeenCalled();
  });
});
