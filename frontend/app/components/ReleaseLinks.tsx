"use client";

interface ReleaseLink {
  url?: string;
  type?: string;
  description?: string;
}

interface ReleaseLinksProps {
  links: ReleaseLink[];
}

export function ReleaseLinks({ links }: ReleaseLinksProps) {
  if (!links || links.length === 0) {
    return null;
  }

  const getIconForLinkType = (type?: string, url?: string) => {
    // Try to use provided type, or infer from URL
    const effectiveType = type || inferTypeFromUrl(url);
    
    if (!effectiveType) return '🔗';
    
    const lowerType = effectiveType.toLowerCase();
    
    switch (lowerType) {
      case 'spotify':
        return '🎵';
      case 'discogs':
        return '💽';
      case 'musicbrainz':
        return '🎼';
      case 'youtube':
        return '📺';
      case 'bandcamp':
        return '🎧';
      case 'soundcloud':
        return '☁️';
      case 'apple':
      case 'itunes':
        return '🍎';
      case 'amazon':
        return '📦';
      case 'lastfm':
      case 'last.fm':
        return '📻';
      case 'allmusic':
        return '🎵';
      case 'rateyourmusic':
      case 'rym':
        return '⭐';
      default:
        return '🔗';
    }
  };

  const inferTypeFromUrl = (url?: string): string | undefined => {
    if (!url) return undefined;
    
    const lowerUrl = url.toLowerCase();
    
    if (lowerUrl.includes('spotify.com')) return 'spotify';
    if (lowerUrl.includes('discogs.com')) return 'discogs';
    if (lowerUrl.includes('musicbrainz.org')) return 'musicbrainz';
    if (lowerUrl.includes('youtube.com') || lowerUrl.includes('youtu.be')) return 'youtube';
    if (lowerUrl.includes('bandcamp.com')) return 'bandcamp';
    if (lowerUrl.includes('soundcloud.com')) return 'soundcloud';
    if (lowerUrl.includes('apple.com') || lowerUrl.includes('itunes.apple.com')) return 'apple';
    if (lowerUrl.includes('amazon.com') || lowerUrl.includes('amazon.')) return 'amazon';
    if (lowerUrl.includes('last.fm') || lowerUrl.includes('lastfm.')) return 'lastfm';
    if (lowerUrl.includes('allmusic.com')) return 'allmusic';
    if (lowerUrl.includes('rateyourmusic.com')) return 'rateyourmusic';
    
    return undefined;
  };

  const extractDomain = (url?: string): string | undefined => {
    if (!url) return undefined;
    
    try {
      const urlObject = new URL(url);
      // Remove 'www.' prefix if present
      return urlObject.hostname.replace(/^www\./, '');
    } catch {
      return undefined;
    }
  };

  const formatLinkType = (type?: string, url?: string) => {
    // Try to use provided type, or infer from URL
    const effectiveType = type || inferTypeFromUrl(url);
    
    // If no type found, use the domain name
    if (!effectiveType) {
      const domain = extractDomain(url);
      return domain || 'Link';
    }
    
    const lowerType = effectiveType.toLowerCase();
    
    switch (lowerType) {
      case 'musicbrainz':
        return 'MusicBrainz';
      case 'lastfm':
        return 'Last.fm';
      case 'rateyourmusic':
        return 'Rate Your Music';
      case 'allmusic':
        return 'AllMusic';
      case 'soundcloud':
        return 'SoundCloud';
      default:
        return effectiveType.charAt(0).toUpperCase() + effectiveType.slice(1);
    }
  };

  const isValidUrl = (url?: string) => {
    if (!url) return false;
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const validLinks = links.filter(link => isValidUrl(link.url));

  if (validLinks.length === 0) {
    return null;
  }

  return (
    <div className="space-y-2">
      {validLinks.map((link, index) => (
        <a
          key={index}
          href={link.url}
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-3 py-2 text-gray-600 hover:text-red-500 transition-colors group"
        >
          {/* Link Type */}
          <span className="text-sm uppercase tracking-widest font-bold min-w-[120px]">
            {formatLinkType(link.type, link.url)}
          </span>
          
          {/* Description / label */}
          <div className="flex-1 min-w-0">
            {link.description ? (
              <div className="text-sm text-gray-700 truncate">{link.description}</div>
            ) : (
              <div className="text-sm text-gray-500 truncate">{extractDomain(link.url)}</div>
            )}
          </div>

          {/* Arrow */}
          <svg className="w-3 h-3 text-gray-300 group-hover:text-red-500 transition-colors flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M14 5l7 7m0 0l-7 7m7-7H3" />
          </svg>
        </a>
      ))}
    </div>
  );
}
