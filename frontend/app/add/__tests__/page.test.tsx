import React from 'react';
import { render, screen } from '@testing-library/react';
import AddReleasePage from '../page';

// Mock next/navigation hooks used by this page
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/add',
}));

// Mock the Discogs wizard (it fetches lookups on mount)
jest.mock('../../components/wizard/discogs/DiscogsAddReleaseWizard', () => ({
  __esModule: true,
  default: () => <div data-testid="discogs-wizard" />,
}));

// Mock the manual wizard
jest.mock('../../components/wizard/AddReleaseWizard', () => ({
  __esModule: true,
  default: () => <div data-testid="manual-wizard" />,
}));

describe('AddReleasePage', () => {
  it('renders the page header', () => {
    render(<AddReleasePage />);

    expect(screen.getByRole('heading', { name: 'Add Release', level: 1 })).toBeInTheDocument();
    expect(screen.getByText(/how would you like to add a release/i)).toBeInTheDocument();
  });

  it('renders the source-selection cards', () => {
    render(<AddReleasePage />);

    expect(screen.getByText('Search Discogs')).toBeInTheDocument();
    expect(screen.getByText('Manual Entry')).toBeInTheDocument();
  });

  it('renders the plus icon SVG', () => {
    const { container } = render(<AddReleasePage />);

    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
    expect(svg).toBeTruthy();
  });

  it('has proper page structure', () => {
    const { container } = render(<AddReleasePage />);

    expect(container.querySelector('.max-w-4xl')).toBeInTheDocument();
    expect(container.querySelector('.space-y-8')).toBeInTheDocument();
  });
});
