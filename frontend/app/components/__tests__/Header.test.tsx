import React from 'react';
import { render, screen } from '@testing-library/react';
import Header from '../Header';

// Mock Next.js Link
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  );
});

describe('Header Component', () => {
  it('renders the site title', () => {
    render(<Header />);
    expect(screen.getByText('Kollector Sküm')).toBeInTheDocument();
  });

  it('renders navigation links', () => {
    render(<Header />);
    
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Collection')).toBeInTheDocument();
    expect(screen.getByText('Search')).toBeInTheDocument();
    expect(screen.getByText('Statistics')).toBeInTheDocument();
  });

  it('contains link to home page', () => {
    render(<Header />);
    const homeLink = screen.getByRole('link', { name: /dashboard/i });
    expect(homeLink).toHaveAttribute('href', '/');
  });

  it('contains link to collection page', () => {
    render(<Header />);
    const collectionLink = screen.getByRole('link', { name: /collection/i });
    expect(collectionLink).toHaveAttribute('href', '/collection');
  });

  it('contains link to search page', () => {
    render(<Header />);
    const searchLink = screen.getByRole('link', { name: /search/i });
    expect(searchLink).toHaveAttribute('href', '/search');
  });

  it('contains link to statistics page', () => {
    render(<Header />);
    const statsLink = screen.getByRole('link', { name: /statistics/i });
    expect(statsLink).toHaveAttribute('href', '/statistics');
  });

  it('has proper header styling', () => {
    const { container } = render(<Header />);
    const header = container.querySelector('header');
    expect(header).toBeInTheDocument();
  });

  it('renders navigation in a nav element', () => {
    const { container } = render(<Header />);
    const nav = container.querySelector('nav');
    expect(nav).toBeInTheDocument();
  });
});
