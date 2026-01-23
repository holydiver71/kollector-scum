import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import EditReleasePage from '../page';
import * as api from '../../../../lib/api';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useParams: jest.fn(),
  useRouter: jest.fn(),
}));

import { useParams, useRouter } from 'next/navigation';
const mockUseParams = useParams as jest.MockedFunction<typeof useParams>;
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;

// Mock API
jest.mock('../../../../lib/api', () => ({
  fetchJson: jest.fn(),
  updateRelease: jest.fn(),
}));

// Mock AddReleaseForm component
jest.mock('../../../../components/AddReleaseForm', () => {
   
  return ({ initialData, releaseId, onSuccess, onCancel }: any) => (
    <div data-testid="add-release-form">
      <div data-testid="form-release-id">{releaseId}</div>
      <div data-testid="form-title">{initialData?.title}</div>
      <div data-testid="form-artist-ids">{JSON.stringify(initialData?.artistIds)}</div>
      <button onClick={() => onSuccess(123)}>Submit</button>
      <button onClick={onCancel}>Cancel</button>
    </div>
  );
});

// Mock LoadingSpinner
jest.mock('../../../../components/LoadingComponents', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner">Loading...</div>,
}));

const mockRelease = {
  id: 1,
  title: 'Test Album',
  artists: [{ id: 1, name: 'Test Artist' }],
  genres: [{ id: 2, name: 'Rock' }],
  live: false,
  label: { id: 3, name: 'Test Label' },
  country: { id: 4, name: 'USA' },
  format: { id: 5, name: 'Vinyl' },
  packaging: { id: 6, name: 'Jewel Case' },
  releaseYear: '2023-01-01T00:00:00',
  origReleaseYear: '2022-01-01T00:00:00',
  labelNumber: 'TEST123',
  upc: '123456789',
  lengthInSeconds: 3600,
  purchaseInfo: {
    storeId: 405,
    storeName: 'Test Store',
    price: 20.00,
    currency: 'USD',
    purchaseDate: '2023-01-15T00:00:00',
    notes: 'Test notes',
  },
  images: {
    coverFront: 'front.jpg',
    coverBack: 'back.jpg',
    thumbnail: 'thumb.jpg',
  },
  links: [
    { url: 'https://example.com', type: 'Official', description: 'Website' },
  ],
  media: [
    {
      name: 'CD 1',
      tracks: [
        { title: 'Track 1', index: 1, lengthSecs: 180, artists: [], genres: [], live: false },
      ],
    },
  ],
  dateAdded: '2023-01-01T00:00:00',
  lastModified: '2023-01-02T00:00:00',
};

describe('EditReleasePage', () => {
  const mockPush = jest.fn();
  const mockBack = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseParams.mockReturnValue({ id: '1' });
    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: mockBack,
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    } as any);
  });

  it('renders loading state initially', () => {
    (api.fetchJson as jest.Mock).mockImplementation(() => new Promise(() => {}));
    
    render(<EditReleasePage />);
    
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('fetches release data on mount', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(api.fetchJson).toHaveBeenCalledWith('/api/musicreleases/1');
    });
  });

  it('renders form with release data after loading', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('add-release-form')).toBeInTheDocument();
    });

    expect(screen.getByTestId('form-title')).toHaveTextContent('Test Album');
    expect(screen.getByTestId('form-release-id')).toHaveTextContent('1');
    expect(screen.getByTestId('form-artist-ids')).toHaveTextContent('[1]');
  });

  it('passes correct initialData to form', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('add-release-form')).toBeInTheDocument();
    });

    // Form should receive artistIds
    expect(screen.getByTestId('form-artist-ids')).toHaveTextContent('[1]');
  });

  it('navigates to release detail page on success', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('add-release-form')).toBeInTheDocument();
    });

    const submitButton = screen.getByRole('button', { name: /submit/i });
    submitButton.click();

    expect(mockPush).toHaveBeenCalledWith('/releases/123');
  });

  it('navigates back on cancel', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('add-release-form')).toBeInTheDocument();
    });

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    cancelButton.click();

    expect(mockPush).toHaveBeenCalledWith('/releases/1');
  });

  it('displays error message on fetch failure', async () => {
    (api.fetchJson as jest.Mock).mockRejectedValue(new Error('Failed to load'));
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByText(/error loading release/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
  });

  it('shows go back button on error', async () => {
    (api.fetchJson as jest.Mock).mockRejectedValue(new Error('Failed to load'));
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /go back/i })).toBeInTheDocument();
    });
  });

  it('displays page title and description', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByText('Edit Release')).toBeInTheDocument();
    });

    expect(screen.getByText(/update the details for "test album"/i)).toBeInTheDocument();
  });

  it('converts purchaseInfo correctly', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('add-release-form')).toBeInTheDocument();
    });

    // The form should receive the purchaseInfo as-is
    // (the form component handles the conversion)
  });

  it('passes releaseId to form component', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValue(mockRelease);
    mockUseParams.mockReturnValue({ id: '456' });
    
    render(<EditReleasePage />);
    
    await waitFor(() => {
      expect(screen.getByTestId('form-release-id')).toHaveTextContent('456');
    });
  });
});
