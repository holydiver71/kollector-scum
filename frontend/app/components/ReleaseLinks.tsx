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

  const getIconForLinkType = (type?: string) => {
    if (!type) return '🔗';
    
    const lowerType = type.toLowerCase();
    
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

  const formatLinkType = (type?: string) => {
    if (!type) return 'Link';
    
    const lowerType = type.toLowerCase();
    
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
        return type.charAt(0).toUpperCase() + type.slice(1);
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
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">External Links</h3>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {validLinks.map((link, index) => (
          <a
            key={index}
            href={link.url}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg hover:border-gray-300 hover:shadow-sm transition-all group"
          >
            {/* Icon */}
            <div className="text-xl flex-shrink-0">
              {getIconForLinkType(link.type)}
            </div>
            
            {/* Link Info */}
            <div className="flex-grow min-w-0">
              <div className="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">
                {formatLinkType(link.type)}
              </div>
              
              {link.description && (
                <div className="text-sm text-gray-600 truncate">
                  {link.description}
                </div>
              )}
              
              <div className="text-xs text-gray-500 truncate mt-1">
                {link.url}
              </div>
            </div>
            
            {/* External Link Icon */}
            <div className="text-gray-400 group-hover:text-gray-600 transition-colors flex-shrink-0">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
              </svg>
            </div>
          </a>
        ))}
      </div>
      
      {/* Link Count Info */}
      <div className="mt-4 text-sm text-gray-500 text-center">
        {validLinks.length} external link{validLinks.length !== 1 ? 's' : ''} available
      </div>
    </div>
  );
}
