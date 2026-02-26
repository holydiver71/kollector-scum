import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ThemeSwitcher from '../ThemeSwitcher';
import { ThemeProvider } from '../../contexts/ThemeContext';

// Mock the auth module's updateUserProfile
jest.mock('../../lib/auth', () => ({
  updateUserProfile: jest.fn(),
}));

import { updateUserProfile } from '../../lib/auth';
const mockUpdateUserProfile = updateUserProfile as jest.MockedFunction<typeof updateUserProfile>;

function renderWithTheme(ui: React.ReactElement) {
  return render(<ThemeProvider>{ui}</ThemeProvider>);
}

describe('ThemeSwitcher', () => {
  beforeEach(() => {
    mockUpdateUserProfile.mockReset();
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('renders theme options', () => {
    renderWithTheme(<ThemeSwitcher />);
    expect(screen.getByText('Metal Default')).toBeInTheDocument();
    expect(screen.getByText('Clean Light')).toBeInTheDocument();
  });

  it('renders Save Theme button', () => {
    renderWithTheme(<ThemeSwitcher />);
    expect(screen.getByRole('button', { name: /save theme/i })).toBeInTheDocument();
  });

  it('marks the default theme as active', () => {
    renderWithTheme(<ThemeSwitcher />);
    const midnightBtn = screen.getByRole('button', { name: /midnight/i });
    expect(midnightBtn).toHaveAttribute('aria-pressed', 'true');
  });

  it('switches active state when a theme is clicked', () => {
    renderWithTheme(<ThemeSwitcher />);
    const cleanLightBtn = screen.getByRole('button', { name: /clean light/i });
    fireEvent.click(cleanLightBtn);
    expect(cleanLightBtn).toHaveAttribute('aria-pressed', 'true');
  });

  it('calls onSaveSuccess after saving the theme', async () => {
    mockUpdateUserProfile.mockResolvedValueOnce({
      userId: 'u1',
      email: 'test@test.com',
      selectedTheme: 'clean-light',
      isAdmin: false,
    } as any);

    const onSaveSuccess = jest.fn();
    renderWithTheme(<ThemeSwitcher onSaveSuccess={onSaveSuccess} />);

    fireEvent.click(screen.getByRole('button', { name: /clean light/i }));
    fireEvent.click(screen.getByRole('button', { name: /save theme/i }));

    await waitFor(() => expect(onSaveSuccess).toHaveBeenCalledWith('clean-light'));
  });

  it('calls onSaveError when the API call fails', async () => {
    mockUpdateUserProfile.mockRejectedValueOnce(new Error('Network error'));

    const onSaveError = jest.fn();
    renderWithTheme(<ThemeSwitcher onSaveError={onSaveError} />);

    fireEvent.click(screen.getByRole('button', { name: /clean light/i }));
    fireEvent.click(screen.getByRole('button', { name: /save theme/i }));

    await waitFor(() => expect(onSaveError).toHaveBeenCalledWith('Network error'));
  });

  it('has data-testid="theme-switcher"', () => {
    renderWithTheme(<ThemeSwitcher />);
    expect(screen.getByTestId('theme-switcher')).toBeInTheDocument();
  });
});
