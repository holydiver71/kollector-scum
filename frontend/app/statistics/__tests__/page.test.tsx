import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import StatisticsPage from '../page';
import * as api from '../../lib/api';

// Mock Next.js Link
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => {
    return <a href={href}>{children}</a>;
  };
});

// Mock API
jest.mock('../../lib/api', () => ({
  getCollectionStatistics: jest.fn(),
}));

// Mock components
jest.mock('../../components/LoadingComponents', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner">Loading...</div>,
}));

jest.mock('../../components/StatisticsCharts', () => ({
  StatCard: ({ title, value }: any) => (
    <div data-testid="stat-card">
      <div>{title}</div>
      <div>{value}</div>
    </div>
  ),
  BarChart: ({ data, title }: any) => (
    <div data-testid="bar-chart">{title}</div>
  ),
  LineChart: ({ data, title }: any) => (
    <div data-testid="line-chart">{title}</div>
  ),
  DonutChart: ({ data, title }: any) => (
    <div data-testid="donut-chart">{title}</div>
  ),
}));

const mockStatistics = {
  totalReleases: 100,
  totalArtists: 50,
  totalGenres: 20,
  totalLabels: 30,
  releasesByYear: [
    { year: 2020, count: 20 },
    { year: 2021, count: 30 },
  ],
  releasesByGenre: [
    { genreName: 'Metal', count: 50, percentage: 50 },
    { genreName: 'Rock', count: 30, percentage: 30 },
  ],
  releasesByFormat: [
    { formatName: 'Vinyl', count: 60, percentage: 60 },
    { formatName: 'CD', count: 40, percentage: 40 },
  ],
  releasesByCountry: [
    { countryName: 'USA', count: 50 },
    { countryName: 'UK', count: 30 },
  ],
  totalValue: 1500.75,
  averagePrice: 10.00,
  mostExpensiveRelease: {
    id: 1,
    title: 'Expensive Album',
    artistName: 'Artist',
    price: 100.00,
  },
  recentlyAdded: [
    {
      id: 1,
      title: 'Recent Album',
      artistName: 'Artist',
      releaseYear: 2023,
    },
  ],
};

describe('StatisticsPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (api.getCollectionStatistics as jest.Mock).mockImplementation(() => new Promise(() => {}));
    
    render(<StatisticsPage />);
    
    expect(screen.getByText('Collection Statistics')).toBeInTheDocument();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('displays statistics after loading', async () => {
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(mockStatistics);
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(screen.getAllByTestId('stat-card').length).toBeGreaterThan(0);
    });
  });

  it('displays error state when API fails', async () => {
    (api.getCollectionStatistics as jest.Mock).mockRejectedValue(new Error('API Error'));
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(screen.getByText(/Failed to load collection statistics/)).toBeInTheDocument();
    });
  });

  it('renders page header', async () => {
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(mockStatistics);
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Collection Statistics')).toBeInTheDocument();
    });
  });

  it('calls getCollectionStatistics on mount', async () => {
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(mockStatistics);
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(api.getCollectionStatistics).toHaveBeenCalledTimes(1);
    });
  });

  it('has proper page structure', async () => {
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(mockStatistics);
    
    const { container } = render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
      expect(container.querySelector('.bg-gray-50')).toBeInTheDocument();
    });
  });

  it('shows description in header', () => {
    (api.getCollectionStatistics as jest.Mock).mockImplementation(() => new Promise(() => {}));
    
    render(<StatisticsPage />);
    
    expect(screen.getByText('Analyze your music collection')).toBeInTheDocument();
  });

  it('displays error in red box', async () => {
    (api.getCollectionStatistics as jest.Mock).mockRejectedValue(new Error('Test error'));
    
    const { container } = render(<StatisticsPage />);
    
    await waitFor(() => {
      const errorBox = container.querySelector('.bg-red-50');
      expect(errorBox).toBeInTheDocument();
    });
  });

  it('renders statistics without optional fields', async () => {
    const minimalStats = {
      totalReleases: 10,
      totalArtists: 5,
      totalGenres: 3,
      totalLabels: 2,
      releasesByYear: [],
      releasesByGenre: [],
      releasesByFormat: [],
      releasesByCountry: [],
      recentlyAdded: [],
    };
    
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(minimalStats);
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(screen.getAllByTestId('stat-card').length).toBeGreaterThan(0);
    });
  });

  it('handles statistics with only totalValue', async () => {
    const statsWithValue = {
      ...mockStatistics,
      averagePrice: undefined,
    };
    
    (api.getCollectionStatistics as jest.Mock).mockResolvedValue(statsWithValue);
    
    render(<StatisticsPage />);
    
    await waitFor(() => {
      expect(screen.getAllByTestId('stat-card').length).toBeGreaterThan(0);
    });
  });
});
