import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { RecentlyPlayed } from '../RecentlyPlayed';
import * as api from '../../lib/api';

// Mock Next.js Link component
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => {
    return <a href={href}>{children}</a>;
  };
});

// Mock API
jest.mock('../../lib/api', () => ({
  getRecentlyPlayed: jest.fn(),
  API_BASE_URL: 'http://localhost:5072',
}));

describe('RecentlyPlayed Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (api.getRecentlyPlayed as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<RecentlyPlayed />);

    expect(screen.getByText('Recently Played')).toBeInTheDocument();
    // Should show skeleton loading states
    const skeletons = document.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('renders empty state when no recently played items', async () => {
    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue([]);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('No recently played albums')).toBeInTheDocument();
    });

    expect(screen.getByText(/Mark albums as/)).toBeInTheDocument();
  });

  it('renders recently played items with covers', async () => {
    const mockItems = [
      { id: 1, coverFront: 'cover1.jpg', playedAt: new Date().toISOString(), playCount: 1 },
      { id: 2, coverFront: 'cover2.jpg', playedAt: new Date().toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      const links = screen.getAllByRole('link');
      expect(links.length).toBe(2);
    });

    // Check that links point to release detail pages
    const links = screen.getAllByRole('link');
    expect(links[0]).toHaveAttribute('href', '/releases/1');
    expect(links[1]).toHaveAttribute('href', '/releases/2');
  });

  it('renders error state when API fails', async () => {
    (api.getRecentlyPlayed as jest.Mock).mockRejectedValue(new Error('API Error'));

    // silence expected console.error during this negative flow
    const spy = jest.spyOn(console, 'error').mockImplementation(() => {});

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('Unable to load recently played')).toBeInTheDocument();
    });

    expect(screen.getByText('API Error')).toBeInTheDocument();

    spy.mockRestore();
  });

  it('shows date heading for first item of each day', async () => {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    const mockItems = [
      { id: 1, coverFront: 'cover1.jpg', playedAt: today.toISOString(), playCount: 1 },
      { id: 2, coverFront: 'cover2.jpg', playedAt: today.toISOString(), playCount: 1 },
      { id: 3, coverFront: 'cover3.jpg', playedAt: yesterday.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('Today')).toBeInTheDocument();
    });

    expect(screen.getByText('Yesterday')).toBeInTheDocument();
    // Only one "Today" should be shown (for the first item of that day)
    expect(screen.getAllByText('Today').length).toBe(1);
  });

  it('uses placeholder for missing cover images', async () => {
    const mockItems = [
      { id: 1, coverFront: undefined, playedAt: new Date().toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      const images = screen.getAllByRole('img');
      expect(images[0]).toHaveAttribute('src', '/placeholder-album.svg');
    });
  });

  it('respects maxItems prop', async () => {
    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue([]);

    render(<RecentlyPlayed maxItems={12} />);

    await waitFor(() => {
      expect(api.getRecentlyPlayed).toHaveBeenCalledWith(12);
    });
  });

  it('uses default maxItems of 24', async () => {
    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue([]);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(api.getRecentlyPlayed).toHaveBeenCalledWith(24);
    });
  });

  it('shows play count badge for albums played multiple times', async () => {
    const mockItems = [
      { id: 1, coverFront: 'cover1.jpg', playedAt: new Date().toISOString(), playCount: 3 },
      { id: 2, coverFront: 'cover2.jpg', playedAt: new Date().toISOString(), playCount: 1 },
      { id: 3, coverFront: 'cover3.jpg', playedAt: new Date().toISOString(), playCount: 5 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('x3')).toBeInTheDocument();
    });

    expect(screen.getByText('x5')).toBeInTheDocument();
    // Album with playCount 1 should not have a badge
    expect(screen.queryByText('x1')).not.toBeInTheDocument();
  });

  it('does not show badge for albums played only once', async () => {
    const mockItems = [
      { id: 1, coverFront: 'cover1.jpg', playedAt: new Date().toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      const links = screen.getAllByRole('link');
      expect(links.length).toBe(1);
    });

    // No badge should be shown
    expect(screen.queryByText(/x\d+/)).not.toBeInTheDocument();
  });
});

describe('formatRelativeDate function', () => {
  // Note: We test this indirectly through the component since it's not exported
  it('shows "Today" for today\'s date', async () => {
    const today = new Date();
    const mockItems = [
      { id: 1, coverFront: 'cover.jpg', playedAt: today.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('Today')).toBeInTheDocument();
    });
  });

  it('shows "Yesterday" for yesterday\'s date', async () => {
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const mockItems = [
      { id: 1, coverFront: 'cover.jpg', playedAt: yesterday.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('Yesterday')).toBeInTheDocument();
    });
  });

  it('shows "X days ago" for dates within a week', async () => {
    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);
    const mockItems = [
      { id: 1, coverFront: 'cover.jpg', playedAt: threeDaysAgo.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('3 days ago')).toBeInTheDocument();
    });
  });

  it('shows "1 week ago" for dates 7-13 days ago', async () => {
    const oneWeekAgo = new Date();
    oneWeekAgo.setDate(oneWeekAgo.getDate() - 10);
    const mockItems = [
      { id: 1, coverFront: 'cover.jpg', playedAt: oneWeekAgo.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('1 week ago')).toBeInTheDocument();
    });
  });

  it('shows "X weeks ago" for dates 14-29 days ago', async () => {
    const twoWeeksAgo = new Date();
    twoWeeksAgo.setDate(twoWeeksAgo.getDate() - 20);
    const mockItems = [
      { id: 1, coverFront: 'cover.jpg', playedAt: twoWeeksAgo.toISOString(), playCount: 1 },
    ];

    (api.getRecentlyPlayed as jest.Mock).mockResolvedValue(mockItems);

    render(<RecentlyPlayed />);

    await waitFor(() => {
      expect(screen.getByText('2 weeks ago')).toBeInTheDocument();
    });
  });
});
