/**
 * Tests for the AddReleasePage source-selection screen.
 *
 * The page now renders a flow-selection card instead of tabs, delegating to
 * DiscogsAddReleaseWizard or AddReleaseWizard based on the user's choice.
 * Both child wizards are mocked here so these tests focus on the page shell.
 */
import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AddReleasePage from '../page';

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/add',
}));

// Mock the wizard components so they don't need API access
jest.mock('../../components/wizard/discogs/DiscogsAddReleaseWizard', () => ({
  __esModule: true,
  default: function MockDiscogsWizard({ onCancel }: { onCancel: () => void }) {
    return (
      <div data-testid="discogs-wizard">
        <button onClick={onCancel}>Change method</button>
        Discogs Wizard
      </div>
    );
  },
}));

jest.mock('../../components/wizard/AddReleaseWizard', () => ({
  __esModule: true,
  default: function MockManualWizard({ onCancel }: { onCancel: () => void }) {
    return (
      <div data-testid="manual-wizard">
        <button onClick={onCancel}>Change method</button>
        Manual Wizard
      </div>
    );
  },
}));

describe('AddReleasePage – source selection', () => {
  it('renders the page heading', () => {
    render(<AddReleasePage />);
    expect(screen.getByRole('heading', { name: /add release/i, level: 1 })).toBeInTheDocument();
  });

  it('renders the page description', () => {
    render(<AddReleasePage />);
    expect(screen.getByText(/add a new music release to your collection/i)).toBeInTheDocument();
  });

  it('shows the Search Discogs and Manual Entry flow cards', () => {
    render(<AddReleasePage />);
    expect(screen.getByRole('button', { name: /search discogs/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /manual entry/i })).toBeInTheDocument();
  });

  it('does not render either wizard on the selection screen', () => {
    render(<AddReleasePage />);
    expect(screen.queryByTestId('discogs-wizard')).not.toBeInTheDocument();
    expect(screen.queryByTestId('manual-wizard')).not.toBeInTheDocument();
  });

  it('shows the Discogs wizard after clicking Search Discogs', async () => {
    const user = userEvent.setup();
    render(<AddReleasePage />);
    await user.click(screen.getByRole('button', { name: /search discogs/i }));
    expect(screen.getByTestId('discogs-wizard')).toBeInTheDocument();
    expect(screen.queryByTestId('manual-wizard')).not.toBeInTheDocument();
  });

  it('shows the Manual wizard after clicking Manual Entry', async () => {
    const user = userEvent.setup();
    render(<AddReleasePage />);
    await user.click(screen.getByRole('button', { name: /manual entry/i }));
    expect(screen.getByTestId('manual-wizard')).toBeInTheDocument();
    expect(screen.queryByTestId('discogs-wizard')).not.toBeInTheDocument();
  });

  it('shows a "Change method" link when a wizard is active', async () => {
    const user = userEvent.setup();
    render(<AddReleasePage />);
    await user.click(screen.getByRole('button', { name: /search discogs/i }));
    expect(screen.getByRole('button', { name: /change method/i })).toBeInTheDocument();
  });

  it('returns to the selection screen when Change method is clicked', async () => {
    const user = userEvent.setup();
    render(<AddReleasePage />);
    await user.click(screen.getByRole('button', { name: /search discogs/i }));
    await user.click(screen.getByRole('button', { name: /change method/i }));
    // Should be back on the selection screen
    expect(screen.getByRole('button', { name: /search discogs/i })).toBeInTheDocument();
    expect(screen.queryByTestId('discogs-wizard')).not.toBeInTheDocument();
  });

  it('has correct outer container classes', () => {
    const { container } = render(<AddReleasePage />);
    expect(container.querySelector('.max-w-4xl')).toBeInTheDocument();
    expect(container.querySelector('.space-y-6')).toBeInTheDocument();
  });
});
