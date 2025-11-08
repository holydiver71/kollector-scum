import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MusicReleaseList, MusicReleaseCard } from '../MusicReleaseList';
import * as api from '../../lib/api';

// Mock the api module
jest.mock('../../lib/api');

// Mock Next.js Link component
jest.mock('next/link', () => {
  // eslint-disable-next-line react/display-name
  return ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  );
});

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: { src: string; alt: string }) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} {...props} />
  ),
}));

const mockRelease = {
  id: 1,
  title: 'Master of Puppets',
  releaseYear: '1986-03-03T00:00:00',
  artistNames: ['Metallica'],
  genreNames: ['Thrash Metal'],
  labelName: 'Elektra',
  countryName: 'USA',
  formatName: 'Vinyl',
  coverImageUrl: 'test.jpg',
  dateAdded: '2024-01-01T00:00:00',
};

const mockPagedResult = {
  items: [mockRelease],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
};

describe('MusicReleaseCard Component', () => {
  it('renders release information correctly', () => {
    render(<MusicReleaseCard release={mockRelease} />);

    expect(screen.getByText('Master of Puppets')).toBeInTheDocument();
    expect(screen.getByText('Metallica')).toBeInTheDocument();
    expect(screen.getByText(/1986/)).toBeInTheDocument();
  });

  it('displays multiple artists joined with commas', () => {
    const multiArtistRelease = {
      ...mockRelease,
      artistNames: ['Artist 1', 'Artist 2', 'Artist 3'],
    };

    render(<MusicReleaseCard release={multiArtistRelease} />);
    expect(screen.getByText('Artist 1, Artist 2, Artist 3')).toBeInTheDocument();
  });

  it('displays genre information', () => {
    render(<MusicReleaseCard release={mockRelease} />);
    expect(screen.getByText('Thrash Metal')).toBeInTheDocument();
  });

  it('displays format and country information', () => {
    render(<MusicReleaseCard release={mockRelease} />);
    expect(screen.getByText(/Vinyl/)).toBeInTheDocument();
    expect(screen.getByText(/USA/)).toBeInTheDocument();
  });

  it('renders links to release details page', () => {
    render(<MusicReleaseCard release={mockRelease} />);
    const links = screen.getAllByRole('link');
    expect(links.length).toBeGreaterThan(0);
    expect(links[0]).toHaveAttribute('href', '/releases/1');
  });

  it('renders cover image', () => {
    render(<MusicReleaseCard release={mockRelease} />);
    const img = screen.getByAltText('Master of Puppets cover');
    expect(img).toBeInTheDocument();
  });
});

describe('MusicReleaseList Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (api.fetchJson as jest.Mock).mockReset();
  });

  it('displays skeleton loading state initially', () => {
    (api.fetchJson as jest.Mock).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    const { container } = render(<MusicReleaseList />);
    // Check for skeleton loaders (animated pulse elements)
    const skeletons = container.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('fetches and displays music releases', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockPagedResult);

    render(<MusicReleaseList />);

    await waitFor(() => {
      expect(screen.getByText('Master of Puppets')).toBeInTheDocument();
    });

    expect(api.fetchJson).toHaveBeenCalled();
  });

  it('applies filters to API request', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockPagedResult);

    const filters = {
      search: 'metallica',
      genreId: 1,
      yearFrom: 1980,
      yearTo: 1990,
    };

    render(<MusicReleaseList filters={filters} />);

    await waitFor(() => {
      expect(api.fetchJson).toHaveBeenCalled();
    });

    const callUrl = (api.fetchJson as jest.Mock).mock.calls[0][0];
    expect(callUrl).toContain('search=metallica');
    expect(callUrl).toContain('genreId=1');
  });

  it('renders music release cards', async () => {
    (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockPagedResult);

    render(<MusicReleaseList />);

    await waitFor(() => {
      expect(screen.getByText('Master of Puppets')).toBeInTheDocument();
      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.getByText('Thrash Metal')).toBeInTheDocument();
    });
  });

  it('displays multiple artists correctly', async () => {
    const multiArtistResult = {
      ...mockPagedResult,
      items: [{
        ...mockRelease,
        artistNames: ['Artist 1', 'Artist 2'],
      }],
    };

    (api.fetchJson as jest.Mock).mockResolvedValueOnce(multiArtistResult);

    render(<MusicReleaseList />);

    await waitFor(() => {
      expect(screen.getByText('Artist 1, Artist 2')).toBeInTheDocument();
    });
  });
});
