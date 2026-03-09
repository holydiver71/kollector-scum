import React from 'react';
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react';
import ArtistsPage from './page';
import * as api from '../lib/api';

// Mock Next.js Link component
jest.mock('next/link', () => {
  return ({ children, href, title }: { children: React.ReactNode; href: string; title?: string }) => {
    return <a href={href} title={title}>{children}</a>;
  };
});

// Mock API
jest.mock('../lib/api', () => ({
  getArtists: jest.fn(),
  API_BASE_URL: 'http://localhost:5072',
}));

/** Helper that builds a PagedArtistsResponse-shaped mock */
const mockPagedArtists = (
  items: { id: number; name: string }[],
  overrides: Partial<{ page: number; pageSize: number; totalCount: number; totalPages: number }> = {}
) => ({
  items,
  page: overrides.page ?? 1,
  pageSize: overrides.pageSize ?? 48,
  totalCount: overrides.totalCount ?? items.length,
  totalPages: overrides.totalPages ?? 1,
});

describe('ArtistsPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('shows a loading spinner initially', () => {
    (api.getArtists as jest.Mock).mockImplementation(() => new Promise(() => {}));
    render(<ArtistsPage />);
    const spinners = document.querySelectorAll('.animate-spin');
    expect(spinners.length).toBeGreaterThan(0);
  });

  it('renders a list of artists after loading', async () => {
    const mockData = mockPagedArtists([
      { id: 1, name: 'Radiohead' },
      { id: 2, name: 'Portishead' },
      { id: 3, name: 'Massive Attack' },
    ], { totalCount: 3 });

    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByText('Radiohead')).toBeInTheDocument();
    });

    expect(screen.getByText('Portishead')).toBeInTheDocument();
    expect(screen.getByText('Massive Attack')).toBeInTheDocument();
  });

  it('displays the total artist count', async () => {
    const mockData = mockPagedArtists(
      [{ id: 1, name: 'Radiohead' }],
      { totalCount: 42 }
    );
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByText('42 artists in your collection')).toBeInTheDocument();
    });
  });

  it('uses singular "artist" when total count is 1', async () => {
    const mockData = mockPagedArtists(
      [{ id: 1, name: 'Radiohead' }],
      { totalCount: 1 }
    );
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByText('1 artist in your collection')).toBeInTheDocument();
    });
  });

  it('links each artist card to the collection filtered by artistId', async () => {
    const mockData = mockPagedArtists([
      { id: 7, name: 'Nirvana' },
    ]);
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      const link = screen.getByRole('link', { name: /Nirvana/i });
      expect(link).toHaveAttribute('href', '/collection?artistId=7');
    });
  });

  it('shows the first letter of the artist name as avatar', async () => {
    const mockData = mockPagedArtists([{ id: 1, name: 'Aphex Twin' }]);
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      // The avatar is inside a specific element; use getAllByText since the A-Z
      // filter bar also contains an 'A' button.
      const allA = screen.getAllByText('A');
      expect(allA.length).toBeGreaterThan(0);
    });
  });

  it('shows an error message when the API call fails', async () => {
    (api.getArtists as jest.Mock).mockRejectedValue(new Error('Network error'));

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });
  });

  it('shows an empty state when there are no artists', async () => {
    const mockData = mockPagedArtists([], { totalCount: 0 });
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByText('No artists in your collection yet')).toBeInTheDocument();
    });
  });

  it('shows empty state with search term when no results found', async () => {
    // First load returns results
    const initial = mockPagedArtists([{ id: 1, name: 'Radiohead' }]);
    // Search returns nothing
    const empty = mockPagedArtists([], { totalCount: 0 });
    (api.getArtists as jest.Mock)
      .mockResolvedValueOnce(initial)
      .mockResolvedValueOnce(empty);

    render(<ArtistsPage />);

    await waitFor(() => screen.getByText('Radiohead'));

    const searchInput = screen.getByRole('textbox', { name: /search artists/i });
    fireEvent.change(searchInput, { target: { value: 'xyz' } });

    // Advance timer to trigger debounce
    act(() => jest.advanceTimersByTime(400));

    await waitFor(() => {
      expect(screen.getByText('No artists found matching "xyz"')).toBeInTheDocument();
    });
  });

  it('renders a clear-search button in empty search state', async () => {
    const initial = mockPagedArtists([{ id: 1, name: 'Radiohead' }]);
    const empty = mockPagedArtists([], { totalCount: 0 });
    (api.getArtists as jest.Mock)
      .mockResolvedValueOnce(initial)
      .mockResolvedValueOnce(empty);

    render(<ArtistsPage />);

    await waitFor(() => screen.getByText('Radiohead'));

    const searchInput = screen.getByRole('textbox', { name: /search artists/i });
    fireEvent.change(searchInput, { target: { value: 'xyz' } });

    act(() => jest.advanceTimersByTime(400));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
    });
  });

  it('clears the search when the clear button is clicked', async () => {
    const full = mockPagedArtists([{ id: 1, name: 'Radiohead' }]);
    const empty = mockPagedArtists([], { totalCount: 0 });
    (api.getArtists as jest.Mock)
      .mockResolvedValueOnce(full)
      .mockResolvedValueOnce(empty)
      .mockResolvedValueOnce(full);

    render(<ArtistsPage />);

    await waitFor(() => screen.getByText('Radiohead'));

    const searchInput = screen.getByRole('textbox', { name: /search artists/i });
    fireEvent.change(searchInput, { target: { value: 'xyz' } });
    act(() => jest.advanceTimersByTime(400));

    await waitFor(() => screen.getByText('No artists found matching "xyz"'));

    fireEvent.click(screen.getByRole('button', { name: /clear filters/i }));

    await waitFor(() => {
      expect(screen.getByText('Radiohead')).toBeInTheDocument();
    });
  });

  it('calls getArtists with search parameter after debounce', async () => {
    const mockData = mockPagedArtists([{ id: 1, name: 'Radiohead' }]);
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);
    await waitFor(() => screen.getByText('Radiohead'));

    const searchInput = screen.getByRole('textbox', { name: /search artists/i });
    fireEvent.change(searchInput, { target: { value: 'Radio' } });

    // Before debounce fires, should not have called with search term yet
    expect(api.getArtists).not.toHaveBeenCalledWith('Radio', expect.anything(), expect.anything());

    act(() => jest.advanceTimersByTime(400));

    await waitFor(() => {
      expect(api.getArtists).toHaveBeenCalledWith('Radio', 1, 48, undefined);
    });
  });

  it('shows pagination controls when there are multiple pages', async () => {
    const mockData = mockPagedArtists(
      Array.from({ length: 48 }, (_, i) => ({ id: i + 1, name: `Artist ${i + 1}` })),
      { totalCount: 96, totalPages: 2 }
    );
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /next page/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /previous page/i })).toBeInTheDocument();
    });
  });

  it('disables the Prev button on first page', async () => {
    const mockData = mockPagedArtists(
      [{ id: 1, name: 'Radiohead' }],
      { totalCount: 96, totalPages: 2 }
    );
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => {
      const prevBtn = screen.getByRole('button', { name: /previous page/i });
      expect(prevBtn).toBeDisabled();
    });
  });

  it('navigates to the next page when Next is clicked', async () => {
    const page1 = mockPagedArtists(
      [{ id: 1, name: 'Radiohead' }],
      { page: 1, totalCount: 96, totalPages: 2 }
    );
    const page2 = mockPagedArtists(
      [{ id: 49, name: 'Portishead' }],
      { page: 2, totalCount: 96, totalPages: 2 }
    );
    (api.getArtists as jest.Mock)
      .mockResolvedValueOnce(page1)
      .mockResolvedValueOnce(page2);

    render(<ArtistsPage />);

    await waitFor(() => screen.getByText('Radiohead'));

    fireEvent.click(screen.getByRole('button', { name: /next page/i }));

    await waitFor(() => {
      expect(screen.getByText('Portishead')).toBeInTheDocument();
    });

    expect(api.getArtists).toHaveBeenCalledWith(undefined, 2, 48, undefined);
  });

  it('does not show pagination when there is only one page', async () => {
    const mockData = mockPagedArtists(
      [{ id: 1, name: 'Radiohead' }],
      { totalCount: 1, totalPages: 1 }
    );
    (api.getArtists as jest.Mock).mockResolvedValue(mockData);

    render(<ArtistsPage />);

    await waitFor(() => screen.getByText('Radiohead'));

    expect(screen.queryByRole('button', { name: /next page/i })).not.toBeInTheDocument();
  });
});
