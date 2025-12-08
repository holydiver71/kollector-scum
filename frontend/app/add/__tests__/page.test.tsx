import React from 'react';
import { render, screen, within } from '@testing-library/react';
import AddReleasePage from '../page';

// Mock next/navigation hooks used by this page
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/add',
}));

describe('AddReleasePage', () => {
  beforeEach(() => {
    // Mock global.fetch so the page's lookup effect doesn't throw in Node (jsdom env)
    // Return empty arrays by default for any lookup endpoints.
    // Tests that require different fetch behavior can override this.
    // Keep fetch unresolved for these tests to prevent async state updates from running
    // (we only assert the static page UI here). Individual tests can override the mock.
    (global as any).fetch = jest.fn().mockImplementation(() => new Promise(() => {}));
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });
  it('renders the page header', () => {
    render(<AddReleasePage />);
    
    expect(screen.getByRole('heading', { name: 'Add Release', level: 1 })).toBeInTheDocument();
    expect(screen.getByText('Add a new music release to your collection')).toBeInTheDocument();
  });

  it('renders the tab navigation', () => {
    render(<AddReleasePage />);

    // Page should include the two primary tab options in the nav
    const nav = screen.getByRole('navigation');
    expect(within(nav).getByRole('button', { name: /Search Discogs/i })).toBeInTheDocument();
    expect(within(nav).getByRole('button', { name: /Manual Entry/i })).toBeInTheDocument();
  });

  it('displays the page description', () => {
    render(<AddReleasePage />);

    expect(screen.getByText(/Add a new music release to your collection/)).toBeInTheDocument();
  });

  it('renders the plus icon SVG', () => {
    const { container } = render(<AddReleasePage />);
    
    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
    // Ensure there's an SVG icon and it has expected sizing classes (nav icon is present)
    expect(svg).toHaveClass('w-5');
  });

  it('has proper page structure', () => {
    const { container } = render(<AddReleasePage />);
    
    expect(container.querySelector('.max-w-7xl')).toBeInTheDocument();
    expect(container.querySelector('.bg-white')).toBeInTheDocument();
    expect(container.querySelector('.rounded-lg')).toBeInTheDocument();
  });
});
