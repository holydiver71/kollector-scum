import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import ImpersonationBanner from '../components/ImpersonationBanner';
import { useImpersonation } from '../contexts/ImpersonationContext';

jest.mock('../contexts/ImpersonationContext', () => ({
  useImpersonation: jest.fn(),
}));

const mockUseImpersonation = useImpersonation as jest.Mock;

describe('ImpersonationBanner', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('doesNotRender_whenNotImpersonating', () => {
    mockUseImpersonation.mockReturnValue({
      isImpersonating: false,
      impersonatedUserDisplayName: null,
      impersonatedUserEmail: null,
      endImpersonation: jest.fn(),
    });

    const { container } = render(<ImpersonationBanner />);

    expect(container.firstChild).toBeNull();
  });

  it('renders_whenImpersonating_withDisplayName', () => {
    mockUseImpersonation.mockReturnValue({
      isImpersonating: true,
      impersonatedUserDisplayName: 'John Doe',
      impersonatedUserEmail: 'john@example.com',
      endImpersonation: jest.fn(),
    });

    render(<ImpersonationBanner />);

    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });

  it('renders_whenImpersonating_fallsBackToEmailWhenDisplayNameAbsent', () => {
    mockUseImpersonation.mockReturnValue({
      isImpersonating: true,
      impersonatedUserDisplayName: null,
      impersonatedUserEmail: 'user@example.com',
      endImpersonation: jest.fn(),
    });

    render(<ImpersonationBanner />);

    expect(screen.getByText('user@example.com')).toBeInTheDocument();
  });

  it('exitButton_callsEndImpersonation_onClick', () => {
    const mockEndImpersonation = jest.fn();
    mockUseImpersonation.mockReturnValue({
      isImpersonating: true,
      impersonatedUserDisplayName: 'John Doe',
      impersonatedUserEmail: 'john@example.com',
      endImpersonation: mockEndImpersonation,
    });

    render(<ImpersonationBanner />);

    fireEvent.click(screen.getByText('Exit Impersonation'));

    expect(mockEndImpersonation).toHaveBeenCalledTimes(1);
  });

  it('bannerText_includesImpersonatedUserIdentity', () => {
    mockUseImpersonation.mockReturnValue({
      isImpersonating: true,
      impersonatedUserDisplayName: 'Jane Smith',
      impersonatedUserEmail: 'jane@example.com',
      endImpersonation: jest.fn(),
    });

    render(<ImpersonationBanner />);

    expect(screen.getByText(/Impersonating:/)).toBeInTheDocument();
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
  });
});
