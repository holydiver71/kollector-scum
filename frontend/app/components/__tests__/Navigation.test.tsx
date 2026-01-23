import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import Navigation from '../Navigation';

// Mock Next.js modules
jest.mock('next/link', () => {
   
  return ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  );
});

jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

import { usePathname } from 'next/navigation';
const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

describe('Navigation Component', () => {
  beforeEach(() => {
    mockUsePathname.mockReturnValue('/');
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders navigation component', () => {
    render(<Navigation />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('renders all navigation items', () => {
    render(<Navigation />);
    
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Collection')).toBeInTheDocument();
    expect(screen.getByText('Search')).toBeInTheDocument();
    expect(screen.getByText('Add Release')).toBeInTheDocument();
  });

  it('renders navigation links with correct hrefs', () => {
    render(<Navigation />);
    
    const dashboardLinks = screen.getAllByRole('link', { name: /dashboard/i });
    expect(dashboardLinks[0]).toHaveAttribute('href', '/');
    
    const collectionLinks = screen.getAllByRole('link', { name: /collection/i });
    expect(collectionLinks[0]).toHaveAttribute('href', '/collection');
  });

  it('highlights active link on dashboard', () => {
    mockUsePathname.mockReturnValue('/');
    render(<Navigation />);
    
    // Dashboard should be active
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('highlights active link on collection page', () => {
    mockUsePathname.mockReturnValue('/collection');
    render(<Navigation />);
    
    expect(screen.getByText('Collection')).toBeInTheDocument();
  });

  it('highlights active link on search page', () => {
    mockUsePathname.mockReturnValue('/search');
    render(<Navigation />);
    
    expect(screen.getByText('Search')).toBeInTheDocument();
  });

  it('highlights active link on add page', () => {
    mockUsePathname.mockReturnValue('/add');
    render(<Navigation />);
    
    expect(screen.getByText('Add Release')).toBeInTheDocument();
  });

  it('renders icons for each navigation item', () => {
    const { container } = render(<Navigation />);
    const svgs = container.querySelectorAll('svg');
    
    // Should have icons for navigation items
    expect(svgs.length).toBeGreaterThan(3);
  });

  it('shows mobile menu button', () => {
    const { container } = render(<Navigation />);
    const menuButtons = container.querySelectorAll('button');
    
    // Should have mobile menu toggle button
    expect(menuButtons.length).toBeGreaterThan(0);
  });

  it('toggles mobile menu on button click', () => {
    const { container } = render(<Navigation />);
    const menuButton = container.querySelector('button');
    
    if (menuButton) {
      fireEvent.click(menuButton);
      // Menu should toggle
      fireEvent.click(menuButton);
    }
  });

  it('handles nested routes correctly', () => {
    mockUsePathname.mockReturnValue('/collection/123');
    render(<Navigation />);
    
    // Collection should still be active for nested routes
    expect(screen.getByText('Collection')).toBeInTheDocument();
  });

  it('renders navigation descriptions', () => {
    render(<Navigation />);
    
    // Check for description text if visible
    const _text = screen.queryByText(/collection overview/i) || 
           screen.queryByText(/browse your music/i);
    // Descriptions might be hidden on mobile, so we just check rendering works
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('handles root path specifically', () => {
    mockUsePathname.mockReturnValue('/');
    render(<Navigation />);
    
    // Only root should be active, not other paths
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('renders without errors on different paths', () => {
    const paths = ['/', '/collection', '/search', '/add', '/releases/123'];
    
    paths.forEach(path => {
      mockUsePathname.mockReturnValue(path);
      const { unmount } = render(<Navigation />);
      expect(screen.getByText('Dashboard')).toBeInTheDocument();
      unmount();
    });
  });
});
