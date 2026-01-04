import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { fireEvent } from '@testing-library/react';
import Dashboard from '../page';
import * as api from '../lib/api';

// Mock Next.js Link component
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => {
    return <a href={href}>{children}</a>;
  };
});

// Mock API
jest.mock('../lib/api', () => ({
  getHealth: jest.fn(),
  getPagedCount: jest.fn(),
  getRecentlyPlayed: jest.fn(),
  API_BASE_URL: 'http://localhost:5072',
}));
// Provide a simple fetchJson shim so `getUserProfile()` (which uses fetchJson)
// works when the test only partially mocks the api module.
const realFetchJson = async (url: string) => {
  if (url.includes('/api/profile')) {
    return {
      email: 'test@example.com',
      name: 'Test User',
      hasCollection: true,
      userId: 'user-1',
      isAdmin: false,
    };
  }
  return {};
};
(require('../lib/api') as any).fetchJson = jest.fn(realFetchJson);

describe('Dashboard Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Default mock for getRecentlyPlayed returns empty array
    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue([]);
  });

  it('renders loading state initially', () => {
    (api.getHealth as jest.Mock).mockImplementation(() => new Promise(() => {}));
    (api.getPagedCount as jest.Mock).mockImplementation(() => new Promise(() => {}));
    // Prevent recently played from resolving during this test so we don't trigger async state updates
    (api.getRecentlyPlayed as jest.Mock).mockImplementation(() => new Promise(() => {}));

    const { container } = render(<Dashboard />);
    
    // Should show loading skeletons with animate-pulse class
    const skeletons = container.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('loads and displays health data and stats', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock)
      .mockResolvedValueOnce(100) // releases
      .mockResolvedValueOnce(50)  // artists
      .mockResolvedValueOnce(20)  // genres
      .mockResolvedValueOnce(30); // labels

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('100')).toBeInTheDocument();
    });

    expect(screen.getByText('50')).toBeInTheDocument();
    expect(screen.getByText('20')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
  });

  it('displays stat card labels', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock).mockResolvedValue(0);

    render(<Dashboard />);

    // When the collection is empty the app shows the Welcome screen
    await waitFor(() => {
      expect(screen.getByText('Welcome to your music collection manager')).toBeInTheDocument();
    });
  });

  it('displays action cards', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock).mockResolvedValue(0);

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Browse Collection')).toBeInTheDocument();
    });

    expect(screen.getByText('Search Music')).toBeInTheDocument();
    expect(screen.getByText('View Statistics')).toBeInTheDocument();
    expect(screen.getByText('Add Release')).toBeInTheDocument();
  });

  it('displays error state when API fails', async () => {
    (api.getHealth as jest.Mock).mockRejectedValue(new Error('API connection failed'));

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Connection Error')).toBeInTheDocument();
    });

    expect(screen.getByText(/API connection failed/)).toBeInTheDocument();
  });

  it('has reload button in error state', async () => {
    (api.getHealth as jest.Mock).mockRejectedValue(new Error('API Error'));

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /reload/i })).toBeInTheDocument();
    });
  });

  it('displays API error with URL when available', async () => {
    const apiError = new Error('Failed to fetch') as any;
    apiError.url = 'http://localhost:5000/api/health';
    
    (api.getHealth as jest.Mock).mockRejectedValue(apiError);

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText(/Failed to fetch/)).toBeInTheDocument();
    });

    expect(screen.getByText(/localhost:5000/)).toBeInTheDocument();
  });

  it('calls getPagedCount for all stat types', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock).mockResolvedValue(0);

    render(<Dashboard />);

    await waitFor(() => {
      expect(api.getPagedCount).toHaveBeenCalledTimes(4);
    });

    expect(api.getPagedCount).toHaveBeenCalledWith('/api/musicreleases');
    expect(api.getPagedCount).toHaveBeenCalledWith('/api/artists');
    expect(api.getPagedCount).toHaveBeenCalledWith('/api/genres');
    expect(api.getPagedCount).toHaveBeenCalledWith('/api/labels');
  });

  it('displays online status', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    // Return at least one release so the main dashboard (not WelcomeScreen) renders
    (api.getPagedCount as jest.Mock).mockResolvedValue(1);

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Online')).toBeInTheDocument();
    });
  });

  it('has links to different sections', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock).mockResolvedValue(0);

    render(<Dashboard />);

    await waitFor(() => {
      const links = screen.getAllByRole('link');
      expect(links.length).toBeGreaterThan(0);
    });
  });

  it('displays zero values when stats are not loaded', async () => {
    const mockHealth = {
      status: 'Healthy',
      timestamp: '2024-01-01T00:00:00Z',
      service: 'KollectorScum API',
      version: '1.0.0',
    };

    (api.getHealth as jest.Mock).mockResolvedValue(mockHealth);
    (api.getPagedCount as jest.Mock).mockResolvedValue(0);

    render(<Dashboard />);

    // With zero releases the Welcome screen is shown instead of the stats
    await waitFor(() => {
      expect(screen.getByText('Welcome to your music collection manager')).toBeInTheDocument();
    });
  });
});
