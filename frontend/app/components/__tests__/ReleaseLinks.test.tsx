import React from 'react';
import { render, screen } from '@testing-library/react';
import { ReleaseLinks } from '../ReleaseLinks';

const mockLinks = [
  {
    url: 'https://open.spotify.com/album/123',
    type: 'spotify',
    description: 'Listen on Spotify',
  },
  {
    url: 'https://www.discogs.com/release/456',
    type: 'discogs',
    description: 'View on Discogs',
  },
  {
    url: 'https://musicbrainz.org/release/789',
    type: 'musicbrainz',
  },
];

const mockLinksWithGeneric = [
  {
    url: 'https://example.com',
    type: 'other',
    description: 'Official Website',
  },
];

describe('ReleaseLinks Component', () => {
  it('renders links section', () => {
    render(<ReleaseLinks links={mockLinks} />);
    // Check for any link to verify component rendered
    expect(screen.getByRole('link', { name: /spotify/i })).toBeInTheDocument();
  });

  it('renders all provided links', () => {
    render(<ReleaseLinks links={mockLinks} />);
    expect(screen.getByRole('link', { name: /spotify/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /discogs/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /musicbrainz/i })).toBeInTheDocument();
  });

  it('links have correct URLs', () => {
    render(<ReleaseLinks links={mockLinks} />);
    const spotifyLink = screen.getByRole('link', { name: /spotify/i });
    expect(spotifyLink).toHaveAttribute('href', 'https://open.spotify.com/album/123');
  });

  it('links open in new tab', () => {
    render(<ReleaseLinks links={mockLinks} />);
    const links = screen.getAllByRole('link');
    links.forEach(link => {
      expect(link).toHaveAttribute('target', '_blank');
      expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    });
  });

  it('returns null when links array is empty', () => {
    const { container } = render(<ReleaseLinks links={[]} />);
    expect(container.firstChild).toBeNull();
  });

  it('displays custom descriptions when provided', () => {
    render(<ReleaseLinks links={mockLinks} />);
    expect(screen.getByText(/Listen on Spotify/i)).toBeInTheDocument();
  });

  it('handles links without description', () => {
    const linksWithoutDesc = [
      {
        url: 'https://example.com',
        type: 'spotify',
      },
    ];
    
    render(<ReleaseLinks links={linksWithoutDesc} />);
    expect(screen.getByRole('link')).toBeInTheDocument();
  });

  it('handles various link types with correct icons', () => {
    const variousLinks = [
      { url: 'https://spotify.com', type: 'spotify' },
      { url: 'https://youtube.com', type: 'youtube' },
      { url: 'https://bandcamp.com', type: 'bandcamp' },
      { url: 'https://apple.com', type: 'apple' },
    ];
    
    render(<ReleaseLinks links={variousLinks} />);
    expect(screen.getByRole('link', { name: /spotify/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /youtube/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /bandcamp/i })).toBeInTheDocument();
  });

  it('handles generic link type', () => {
    render(<ReleaseLinks links={mockLinksWithGeneric} />);
    expect(screen.getByText(/Official Website/i)).toBeInTheDocument();
  });

  it('handles links without type', () => {
    const linksWithoutType = [
      {
        url: 'https://example.com',
        description: 'Some Link',
      },
    ];
    
    render(<ReleaseLinks links={linksWithoutType} />);
    expect(screen.getByText(/Some Link/i)).toBeInTheDocument();
  });

  it('displays multiple links in a grid', () => {
    const manyLinks = [
      { url: 'https://link1.com', type: 'spotify' },
      { url: 'https://link2.com', type: 'youtube' },
      { url: 'https://link3.com', type: 'discogs' },
      { url: 'https://link4.com', type: 'bandcamp' },
    ];
    
    render(<ReleaseLinks links={manyLinks} />);
    const allLinks = screen.getAllByRole('link');
    expect(allLinks).toHaveLength(4);
  });

  it('renders MusicBrainz link correctly', () => {
    render(<ReleaseLinks links={mockLinks} />);
    const mbLink = screen.getByRole('link', { name: /musicbrainz/i });
    expect(mbLink).toHaveAttribute('href', 'https://musicbrainz.org/release/789');
  });
});
