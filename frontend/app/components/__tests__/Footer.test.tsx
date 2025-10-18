import React from 'react';
import { render, screen } from '@testing-library/react';
import Footer from '../Footer';

describe('Footer Component', () => {
  it('renders the footer element', () => {
    const { container } = render(<Footer />);
    const footer = container.querySelector('footer');
    expect(footer).toBeInTheDocument();
  });

  it('displays copyright with current year', () => {
    const currentYear = new Date().getFullYear();
    render(<Footer />);
    expect(screen.getByText(new RegExp(`© ${currentYear}`))).toBeInTheDocument();
  });

  it('displays app name in copyright', () => {
    render(<Footer />);
    expect(screen.getByText(/Kollector Sküm/)).toBeInTheDocument();
  });

  it('displays technology stack information', () => {
    render(<Footer />);
    expect(screen.getByText(/Built with Next.js & .NET Core API/i)).toBeInTheDocument();
  });

  it('renders About link', () => {
    render(<Footer />);
    const aboutLink = screen.getByRole('link', { name: /about/i });
    expect(aboutLink).toBeInTheDocument();
    expect(aboutLink).toHaveAttribute('href', '/about');
  });

  it('renders API Status link', () => {
    render(<Footer />);
    const statusLink = screen.getByRole('link', { name: /api status/i });
    expect(statusLink).toBeInTheDocument();
    expect(statusLink).toHaveAttribute('href', '/api/health');
    expect(statusLink).toHaveAttribute('target', '_blank');
  });

  it('renders API Docs link', () => {
    render(<Footer />);
    const docsLink = screen.getByRole('link', { name: /api docs/i });
    expect(docsLink).toBeInTheDocument();
    expect(docsLink).toHaveAttribute('href', '/swagger');
    expect(docsLink).toHaveAttribute('target', '_blank');
  });

  it('displays phase information', () => {
    render(<Footer />);
    expect(screen.getByText(/Phase 5/i)).toBeInTheDocument();
  });

  it('has proper footer styling', () => {
    const { container } = render(<Footer />);
    const footer = container.querySelector('footer');
    expect(footer).toHaveClass('bg-gray-50', 'border-t');
  });
});
