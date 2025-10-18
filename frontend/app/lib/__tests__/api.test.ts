import { fetchJson, getHealth, getSearchSuggestions, getCollectionStatistics } from '../api';

// Mock the global fetch
global.fetch = jest.fn();

describe('API Utilities', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('fetchJson', () => {
    it('fetches data successfully', async () => {
      const mockData = { test: 'data' };
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      });

      const result = await fetchJson('/test');
      expect(result).toEqual(mockData);
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/test'),
        expect.any(Object)
      );
    });

    it('throws error on failed response', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: async () => ({ error: 'Not found' }),
      });

      await expect(fetchJson('/test')).rejects.toThrow('Request failed (404) Not Found');
    });

    it('handles JSON parse errors', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => {
          throw new Error('Invalid JSON');
        },
      });

      await expect(fetchJson('/test')).rejects.toThrow(/failed to parse json/i);
    });
  });

  describe('getHealth', () => {
    it('fetches health data', async () => {
      const mockHealth = {
        status: 'Healthy',
        timestamp: '2025-10-18T12:00:00Z',
        service: 'KollectorScum.Api',
        version: '1.0.0',
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockHealth,
      });

      const result = await getHealth();
      expect(result).toEqual(mockHealth);
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/health'),
        expect.any(Object)
      );
    });
  });

  describe('getSearchSuggestions', () => {
    it('returns empty array for queries less than 2 characters', async () => {
      const result = await getSearchSuggestions('a');
      expect(result).toEqual([]);
      expect(global.fetch).not.toHaveBeenCalled();
    });

    it('fetches suggestions for valid queries', async () => {
      const mockSuggestions = [
        { type: 'release', id: 1, name: 'Test Album', subtitle: '1990' },
        { type: 'artist', id: 2, name: 'Test Artist' },
      ];

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockSuggestions,
      });

      const result = await getSearchSuggestions('metal');
      expect(result).toEqual(mockSuggestions);
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/musicreleases/suggestions?query=metal'),
        expect.any(Object)
      );
    });

    it('encodes special characters in query', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      });

      await getSearchSuggestions('AC/DC');
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('AC%2FDC'),
        expect.any(Object)
      );
    });
  });

  describe('getCollectionStatistics', () => {
    it('fetches statistics data', async () => {
      const mockStats = {
        totalReleases: 2393,
        totalArtists: 1856,
        totalGenres: 127,
        totalLabels: 892,
        releasesByYear: [],
        releasesByGenre: [],
        releasesByFormat: [],
        releasesByCountry: [],
        recentlyAdded: [],
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockStats,
      });

      const result = await getCollectionStatistics();
      expect(result).toEqual(mockStats);
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/musicreleases/statistics'),
        expect.any(Object)
      );
    });
  });
});
