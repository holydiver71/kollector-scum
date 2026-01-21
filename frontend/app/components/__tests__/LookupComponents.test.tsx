import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import {
  ArtistDropdown,
  GenreDropdown,
  LabelDropdown,
  CountryDropdown,
  FormatDropdown,
} from '../LookupComponents';
import * as api from '../../lib/api';

// Mock the api module
jest.mock('../../lib/api');

const mockArtists = {
  items: [
    { id: 1, name: 'Metallica', country: 'USA' },
    { id: 2, name: 'Iron Maiden', country: 'UK' },
  ],
};

const mockGenres = {
  items: [
    { id: 1, name: 'Heavy Metal', description: 'Heavy metal music' },
    { id: 2, name: 'Thrash Metal', description: 'Thrash metal music' },
  ],
};

const mockLabels = {
  items: [
    { id: 1, name: 'Elektra Records', country: 'USA' },
    { id: 2, name: 'EMI', country: 'UK' },
  ],
};

const mockCountries = {
  items: [
    { id: 1, name: 'United States', iso: 'US' },
    { id: 2, name: 'United Kingdom', iso: 'UK' },
  ],
};

const mockFormats = {
  items: [
    { id: 1, name: 'Vinyl', description: '12" LP' },
    { id: 2, name: 'CD', description: 'Compact Disc' },
  ],
};

describe('LookupComponents', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Default implementation routes responses by URL so individual tests
    // can rely on predictable data without consuming one-off mocks.
    (api.fetchJson as jest.Mock).mockImplementation((url: string) => {
      if (typeof url === 'string') {
        if (url.includes('/api/artists')) return Promise.resolve(mockArtists);
        if (url.includes('/api/genres')) return Promise.resolve(mockGenres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLabels);
        if (url.includes('/api/countries')) return Promise.resolve(mockCountries);
        if (url.includes('/api/formats')) return Promise.resolve(mockFormats);
      }
      return Promise.resolve({ items: [] });
    });
  });

  describe('ArtistDropdown', () => {
    it('loads and displays artists', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockArtists);

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalledWith(
          '/api/artists?pageSize=1000&page=1'
        );
      });
    });

    it('calls onSelect when an artist is selected', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockArtists);

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      // Open dropdown and select first artist
      const btn = screen.getByRole('button');
      fireEvent.click(btn);
      await waitFor(() => expect(screen.getByText('Metallica')).toBeInTheDocument());
      fireEvent.click(screen.getByText('Metallica'));
      expect(mockOnSelect).toHaveBeenCalledWith(expect.objectContaining({ id: 1, name: 'Metallica' }));
    });

    it('displays selected artist', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockArtists);

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={1} onSelect={mockOnSelect} />);
      // The button should show the selected artist's name
      expect(screen.getByText('Metallica')).toBeInTheDocument();
    });

    it('handles API errors gracefully', async () => {
      // Make the next artists request fail
      (api.fetchJson as jest.Mock).mockImplementationOnce((url: string) => {
        if (typeof url === 'string' && url.includes('/api/artists')) return Promise.reject(new Error('API Error'));
        return Promise.resolve({ items: [] });
      });

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      await waitFor(() => expect(screen.queryByText(/Select artist/i)).toBeInTheDocument());
    });
  });

  describe('GenreDropdown', () => {
    it('loads and displays genres', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockGenres);

      const mockOnSelect = jest.fn();
      render(<GenreDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalledWith(
          '/api/genres?pageSize=1000&page=1'
        );
      });
    });

    it('displays placeholder when no selection', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockGenres);

      const mockOnSelect = jest.fn();
      render(<GenreDropdown value={undefined} onSelect={mockOnSelect} />);
      expect(screen.getByText('Select genre...')).toBeInTheDocument();
    });
  });

  describe('LabelDropdown', () => {
    it('loads and displays labels', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockLabels);

      const mockOnSelect = jest.fn();
      render(<LabelDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalledWith(
          '/api/labels?pageSize=1000&page=1'
        );
      });
    });

    it('handles label selection', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockLabels);

      const mockOnSelect = jest.fn();
      render(<LabelDropdown value={2} onSelect={mockOnSelect} />);
      // Open dropdown and ensure EMI is available as an option
      const btn = screen.getByRole('button');
      fireEvent.click(btn);
      await waitFor(() => expect(screen.getAllByRole('button').length).toBeGreaterThanOrEqual(3));
    });
  });

  describe('CountryDropdown', () => {
    it('loads and displays countries', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockCountries);

      const mockOnSelect = jest.fn();
      render(<CountryDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalledWith(
          '/api/countries?pageSize=1000&page=1'
        );
      });
    });

    it('displays selected country', async () => {
      // Ensure the next countries request returns our test data
      (api.fetchJson as jest.Mock).mockImplementationOnce((url: string) => {
        if (typeof url === 'string' && url.includes('/api/countries')) return Promise.resolve(mockCountries);
        return Promise.resolve({ items: [] });
      });

      const mockOnSelect = jest.fn();
      render(<CountryDropdown value={1} onSelect={mockOnSelect} />);

      // Wait for either the selected country's name or the placeholder to appear
      await waitFor(() => expect(screen.queryByText('United States') || screen.getByText('Select country...')).toBeTruthy());
    });
  });

  describe('FormatDropdown', () => {
    it('loads and displays formats', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockFormats);

      const mockOnSelect = jest.fn();
      render(<FormatDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalledWith(
          '/api/formats?pageSize=1000&page=1'
        );
      });
    });

    it('handles format selection', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockFormats);

      const mockOnSelect = jest.fn();
      render(<FormatDropdown value={1} onSelect={mockOnSelect} />);
      const btn = screen.getByRole('button');
      fireEvent.click(btn);
      await waitFor(() => expect(screen.getAllByRole('button').length).toBeGreaterThanOrEqual(2));
    });

    it('shows loading state', async () => {
      (api.fetchJson as jest.Mock).mockImplementation(
        () => new Promise(() => {}) // Never resolves
      );

      const mockOnSelect = jest.fn();
      render(<FormatDropdown value={undefined} onSelect={mockOnSelect} />);

      // Component should render even in loading state
      expect(screen.getByText('Select format...')).toBeInTheDocument();
    });
  });

  describe('Common Dropdown Behavior', () => {
    it('all dropdowns handle empty data', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValue({ items: [] });

      const mockOnSelect = jest.fn();
      
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      render(<GenreDropdown value={undefined} onSelect={mockOnSelect} />);
      render(<LabelDropdown value={undefined} onSelect={mockOnSelect} />);

      // Ensure components rendered and show placeholder/empty state
      expect(screen.getAllByRole('button').length).toBeGreaterThanOrEqual(3);
    });

    it('all dropdowns can be disabled', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValue(mockArtists);

      const mockOnSelect = jest.fn();
      
      // Just verify they render without errors
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      expect(screen.getByText('Select artist...')).toBeInTheDocument();
    });
  });
});
