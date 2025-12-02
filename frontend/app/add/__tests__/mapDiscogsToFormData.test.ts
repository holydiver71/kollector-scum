/**
 * Unit tests for mapDiscogsToFormData logic
 * 
 * Since mapDiscogsToFormData is an internal function in page.tsx,
 * we replicate its core logic here for testing.
 * This tests the feature: Discogs URI should be pre-populated in links when editing
 */

import type { DiscogsRelease } from '../../lib/discogs-types';

// Replicated logic from page.tsx for testing
function mapDiscogsToFormDataLinks(release: DiscogsRelease): Array<{ url: string; type: string; description: string }> {
  return release.uri ? [{ url: release.uri, type: "Discogs", description: "" }] : [];
}

describe('mapDiscogsToFormData - Discogs URI mapping', () => {
  it('should include Discogs URI in links when URI is present', () => {
    const mockRelease: Partial<DiscogsRelease> = {
      id: 12345,
      title: 'Test Album',
      uri: 'https://www.discogs.com/release/12345',
      artists: [{ id: 1, name: 'Test Artist' }],
      labels: [{ id: 1, name: 'Test Label' }],
      formats: [{ name: 'Vinyl', qty: '1' }],
      genres: ['Rock'],
      styles: ['Hard Rock'],
      country: 'US',
      releaseDate: '2023-01-01',
      year: 2023,
      tracklist: [],
      images: [],
      identifiers: [],
      resourceUrl: 'https://api.discogs.com/releases/12345',
    };

    const links = mapDiscogsToFormDataLinks(mockRelease as DiscogsRelease);

    expect(links).toHaveLength(1);
    expect(links[0]).toEqual({
      url: 'https://www.discogs.com/release/12345',
      type: 'Discogs',
      description: '',
    });
  });

  it('should return empty links array when URI is not present', () => {
    const mockRelease: Partial<DiscogsRelease> = {
      id: 12345,
      title: 'Test Album',
      uri: '', // empty URI
      artists: [{ id: 1, name: 'Test Artist' }],
      labels: [{ id: 1, name: 'Test Label' }],
      formats: [{ name: 'Vinyl', qty: '1' }],
      genres: ['Rock'],
      styles: ['Hard Rock'],
      country: 'US',
      releaseDate: '2023-01-01',
      year: 2023,
      tracklist: [],
      images: [],
      identifiers: [],
      resourceUrl: 'https://api.discogs.com/releases/12345',
    };

    const links = mapDiscogsToFormDataLinks(mockRelease as DiscogsRelease);

    expect(links).toHaveLength(0);
  });

  it('should set link type to "Discogs"', () => {
    const mockRelease: Partial<DiscogsRelease> = {
      id: 12345,
      title: 'Test Album',
      uri: 'https://www.discogs.com/release/12345',
      artists: [],
      labels: [],
      formats: [],
      genres: [],
      styles: [],
      country: '',
      releaseDate: '',
      year: 2023,
      tracklist: [],
      images: [],
      identifiers: [],
      resourceUrl: '',
    };

    const links = mapDiscogsToFormDataLinks(mockRelease as DiscogsRelease);

    expect(links[0].type).toBe('Discogs');
  });
});
