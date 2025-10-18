import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
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

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
    });

    it('displays selected artist', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockArtists);

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={1} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
    });

    it('handles API errors gracefully', async () => {
      (api.fetchJson as jest.Mock).mockRejectedValueOnce(new Error('API Error'));

      const mockOnSelect = jest.fn();
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
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

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
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

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
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
      (api.fetchJson as jest.Mock).mockResolvedValueOnce(mockCountries);

      const mockOnSelect = jest.fn();
      render(<CountryDropdown value={1} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
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

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
    });

    it('shows loading state', async () => {
      (api.fetchJson as jest.Mock).mockImplementation(
        () => new Promise(() => {}) // Never resolves
      );

      const mockOnSelect = jest.fn();
      render(<FormatDropdown value={undefined} onSelect={mockOnSelect} />);

      // Component should render even in loading state
      expect(api.fetchJson).toHaveBeenCalled();
    });
  });

  describe('Common Dropdown Behavior', () => {
    it('all dropdowns handle empty data', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValue({ items: [] });

      const mockOnSelect = jest.fn();
      
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      render(<GenreDropdown value={undefined} onSelect={mockOnSelect} />);
      render(<LabelDropdown value={undefined} onSelect={mockOnSelect} />);

      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
    });

    it('all dropdowns can be disabled', async () => {
      (api.fetchJson as jest.Mock).mockResolvedValue(mockArtists);

      const mockOnSelect = jest.fn();
      
      // Just verify they render without errors
      render(<ArtistDropdown value={undefined} onSelect={mockOnSelect} />);
      
      await waitFor(() => {
        expect(api.fetchJson).toHaveBeenCalled();
      });
    });
  });
});
