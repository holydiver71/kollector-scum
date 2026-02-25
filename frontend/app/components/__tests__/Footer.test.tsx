import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import Footer from '../Footer';

// Mock the API module so no real network calls are made
jest.mock('../../lib/api', () => ({
  getHealth: jest.fn().mockResolvedValue({
    status: 'Healthy',
    timestamp: '',
    service: 'test',
    version: '1.0.0',
  }),
}));

// Mock child component that makes its own API call
jest.mock('../DbConnectionStatus', () => () => <div data-testid="db-connection-status" />);

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

  it('renders About link', () => {
    render(<Footer />);
    const aboutLink = screen.getByRole('link', { name: /about/i });
    expect(aboutLink).toBeInTheDocument();
    expect(aboutLink).toHaveAttribute('href', '/about');
  });

  it('renders the API health status indicator', () => {
    render(<Footer />);
    expect(screen.getByTestId('api-health-status')).toBeInTheDocument();
    expect(screen.getByText('API')).toBeInTheDocument();
  });

  it('shows a green dot when API is healthy', async () => {
    const { getHealth } = require('../../lib/api');
    getHealth.mockResolvedValueOnce({ status: 'Healthy', timestamp: '', service: 'test', version: '1.0.0' });
    render(<Footer />);
    await waitFor(() => {
      const dot = screen.getByTestId('api-health-status').querySelector('span');
      expect(dot).toHaveClass('bg-green-500');
    });
  });

  it('shows a red dot when the API is unhealthy', async () => {
    const { getHealth } = require('../../lib/api');
    getHealth.mockRejectedValueOnce(new Error('offline'));
    render(<Footer />);
    await waitFor(() => {
      const dot = screen.getByTestId('api-health-status').querySelector('span');
      expect(dot).toHaveClass('bg-red-500');
    });
  });

  it('does not render "Built with Next.js" text', () => {
    render(<Footer />);
    expect(screen.queryByText(/built with next\.js/i)).not.toBeInTheDocument();
  });

  it('does not render a Phase 5 banner', () => {
    render(<Footer />);
    expect(screen.queryByText(/phase 5/i)).not.toBeInTheDocument();
  });

  it('does not render an API Docs link', () => {
    render(<Footer />);
    expect(screen.queryByRole('link', { name: /api docs/i })).not.toBeInTheDocument();
  });

  it('has proper footer styling', () => {
    const { container } = render(<Footer />);
    const footer = container.querySelector('footer');
    expect(footer).toHaveClass('bg-gray-50', 'border-t');
  });

  it('renders the DB connection status indicator', () => {
    render(<Footer />);
    expect(screen.getByTestId('db-connection-status')).toBeInTheDocument();
  });

  it('displays Last Deploy when health returns a timestamp', async () => {
    const { getHealth } = require('../../lib/api');
    getHealth.mockResolvedValueOnce({
      status: 'Healthy',
      timestamp: '2026-02-25T21:39:53Z',
      service: 'test',
      version: '1.0.0',
    });
    render(<Footer />);
    await waitFor(() => {
      expect(screen.getByTestId('last-deploy')).toHaveTextContent(/Last Deploy:/);
    });
  });
});

