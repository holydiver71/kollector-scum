import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import ReleaseDetailPage from '../page';
import * as api from '../../../lib/api';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useParams: jest.fn(),
  useRouter: jest.fn(),
}));

const mockUseParams = require('next/navigation').useParams;
const mockUseRouter = require('next/navigation').useRouter;

// Mock Next.js Link
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => {
    return <a href={href}>{children}</a>;
  };
});

// Mock API
jest.mock('../../../lib/api', () => ({
  fetchJson: jest.fn(),
}));

// Mock components
jest.mock('../../../components/LoadingComponents', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner">Loading...</div>,
}));

jest.mock('../../../components/ImageGallery', () => ({
  ImageGallery: ({ images }: any) => <div data-testid="image-gallery">ImageGallery</div>,
}));

jest.mock('../../../components/TrackList', () => ({
  TrackList: ({ tracks }: any) => <div data-testid="track-list">TrackList</div>,
}));

jest.mock('../../../components/ReleaseLinks', () => ({
  ReleaseLinks: ({ links }: any) => <div data-testid="release-links">ReleaseLinks</div>,
}));

const mockRelease = {
  id: 1,
  title: 'Test Album',
  artistName: 'Test Artist',
  releaseYear: 2023,
  genreName: 'Rock',
  formatName: 'Vinyl',
  labelName: 'Test Label',
  countryName: 'USA',
  packagingName: 'Jewel Case',
  price: 20.00,
  currency: 'USD',
  purchaseDate: '2023-01-01',
  tracks: [],
  images: {},
  links: [],
};

describe('ReleaseDetailPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUseParams.mockReturnValue({ id: '1' });
    mockUseRouter.mockReturnValue({
      push: jest.fn(),
      back: jest.fn(),
    });
  });

  it('renders loading state initially', () => {
    (api.fetchJson as jest.Mock).mockImplementation(() => new Promise(() => {}));
    
    render(<ReleaseDetailPage />);
    
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('loads and displays release data', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Album')).toBeInTheDocument();
    });
  });

  it('displays release title', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Album')).toBeInTheDocument();
    });
  });

  it('displays error state when API fails', async () => {
    (api.fetchJson as jest.Mock).mockRejectedValue(new Error('Failed to fetch'));
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByText(/Error loading release/)).toBeInTheDocument();
    });
  });

  it('renders ImageGallery component', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('image-gallery')).toBeInTheDocument();
    });
  });

  it('calls fetchJson with correct URL', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(api.fetchJson).toHaveBeenCalledWith('/api/musicreleases/1');
    });
  });

  it('has proper page structure', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    const { container } = render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
    });
  });
});
