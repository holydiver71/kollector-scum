import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import ReleaseDetailPage from '../page';
import * as api from '../../../lib/api';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useParams: jest.fn(),
  useRouter: jest.fn(),
}));

import { useParams, useRouter } from 'next/navigation';
const mockUseParams = useParams as jest.MockedFunction<typeof useParams>;
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;

// Mock Next.js Link
jest.mock('next/link', () => {
  // eslint-disable-next-line react/display-name
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
  ImageGallery: () => <div data-testid="image-gallery">ImageGallery</div>,
}));

jest.mock('../../../components/TrackList', () => ({
  TrackList: () => <div data-testid="track-list">TrackList</div>,
}));

jest.mock('../../../components/ReleaseLinks', () => ({
  ReleaseLinks: () => <div data-testid="release-links">ReleaseLinks</div>,
}));

jest.mock('../../../components/DeleteReleaseButton', () => ({
  DeleteReleaseButton: ({ 
    onDeleteSuccess 
  }: { 
    releaseId: number; 
    releaseTitle: string; 
    onDeleteSuccess?: () => void; 
    onDeleteError?: (error: unknown) => void;
  }) => (
    <button 
      data-testid="delete-release-button"
      onClick={() => {
        // Simulate successful deletion
        if (onDeleteSuccess) onDeleteSuccess();
      }}
    >
      Delete
    </button>
  ),
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
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
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

  it('renders delete button', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('delete-release-button')).toBeInTheDocument();
    });
  });

  it('navigates to collection page after successful deletion', async () => {
    const mockPush = jest.fn();
    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    });
    
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('delete-release-button')).toBeInTheDocument();
    });
    
    // Click the delete button (which triggers onDeleteSuccess in mock)
    const deleteButton = screen.getByTestId('delete-release-button');
    deleteButton.click();
    
    expect(mockPush).toHaveBeenCalledWith('/collection');
  });

  it('has back button that navigates back', async () => {
    const mockBack = jest.fn();
    mockUseRouter.mockReturnValue({
      push: jest.fn(),
      back: mockBack,
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    });
    
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByText(/Back to Collection/i)).toBeInTheDocument();
    });
    
    const backButton = screen.getByText(/Back to Collection/i);
    backButton.click();
    
    expect(mockBack).toHaveBeenCalled();
  });

  it('displays price of 0.00 correctly', async () => {
    const releaseWithZeroPrice = {
      ...mockRelease,
      purchaseInfo: {
        storeId: 1,
        storeName: 'Test Store',
        price: 0,
        currency: 'USD',
      },
      media: [{ name: 'Disc 1', tracks: [] }], // Single disc so purchase info shows in first layout
      dateAdded: '2023-01-01T00:00:00',
      lastModified: '2023-01-01T00:00:00',
    };
    
    (api.fetchJson as jest.Mock).mockResolvedValue(releaseWithZeroPrice);
    
    render(<ReleaseDetailPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Test Album')).toBeInTheDocument();
    });

    // The price should display as USD 0.00 (not be hidden)
    await waitFor(() => {
      expect(screen.getByText('USD 0.00')).toBeInTheDocument();
    });
  });
});
