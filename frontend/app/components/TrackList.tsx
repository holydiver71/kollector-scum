"use client";

interface Track {
  title: string;
  releaseYear?: string;
  artists: string[];
  genres: string[];
  live: boolean;
  lengthSecs?: number;
  index: number;
}

interface Media {
  name?: string;
  tracks?: Track[];
}

interface TrackListProps {
  media: Media[];
  albumArtists?: string[];
}

export function TrackList({ media, albumArtists = [] }: TrackListProps) {
  const formatDuration = (seconds?: number) => {
    if (!seconds || seconds === 0) return null;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  const areArtistsDifferent = (trackArtists: string[], albumArtists: string[]) => {
    if (trackArtists.length === 0) return false;
    if (albumArtists.length === 0) return true;
    
    // Debug logging to see what we're getting
    console.log('Track artists:', trackArtists);
    console.log('Album artists:', albumArtists);
    
    // Check if track artists are numeric IDs (indicating they need resolution)
    const trackArtistsAreIds = trackArtists.some(artist => /^\d+$/.test(artist));
    if (trackArtistsAreIds) {
      // If track artists are IDs, we can't compare them to names
      // For now, show them if they exist (this indicates different artists)
      return true;
    }
    
    // Sort both arrays for comparison
    const sortedTrackArtists = [...trackArtists].sort();
    const sortedAlbumArtists = [...albumArtists].sort();
    
    // Check if arrays are different
    if (sortedTrackArtists.length !== sortedAlbumArtists.length) return true;
    
    return sortedTrackArtists.some((artist, index) => 
      artist.toLowerCase() !== sortedAlbumArtists[index].toLowerCase()
    );
  };

  const getTotalDuration = (tracks?: Track[]) => {
    if (!tracks) return 0;
    return tracks.reduce((total, track) => {
      const trackLength = track.lengthSecs || 0;
      return total + (trackLength > 0 ? trackLength : 0);
    }, 0);
  };

  if (!media || media.length === 0) {
    return null;
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-6">Tracklist</h3>
      
      {media.map((disc, discIndex) => (
        <div key={discIndex} className={discIndex > 0 ? 'mt-8' : ''}>
          {/* Disc Header */}
          {media.length > 1 && (
            <div className="flex items-center justify-between mb-4">
              <h4 className="text-md font-medium text-gray-800">
                {disc.name || `Disc ${discIndex + 1}`}
              </h4>
              {disc.tracks && disc.tracks.length > 0 && (
                <div className="text-sm text-gray-500">
                  {disc.tracks.length} tracks
                  {getTotalDuration(disc.tracks) > 0 && (
                    <span> • {formatDuration(getTotalDuration(disc.tracks))}</span>
                  )}
                </div>
              )}
            </div>
          )}

          {/* Track List */}
          {disc.tracks && disc.tracks.length > 0 ? (
            <div className="space-y-1">
              {disc.tracks
                .sort((a, b) => a.index - b.index)
                .map((track, trackIndex) => (
                  <div
                    key={trackIndex}
                    className="flex items-center gap-4 py-2 px-3 rounded hover:bg-gray-50 transition-colors"
                  >
                    {/* Track Number */}
                    <div className="w-8 text-sm text-gray-500 text-right flex-shrink-0">
                      {track.index}
                    </div>

                    {/* Track Info */}
                    <div className="flex-grow min-w-0">
                      <div className="flex items-start justify-between">
                        <div className="flex-grow min-w-0">
                          <h5 className="text-sm font-medium text-gray-900 truncate">
                            {track.title}
                            {track.live && (
                              <span className="ml-2 inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">
                                LIVE
                              </span>
                            )}
                          </h5>
                          
                          {/* Track Artists (if different from main artist) */}
                          {track.artists && track.artists.length > 0 && areArtistsDifferent(track.artists, albumArtists) && (
                            <p className="text-xs text-gray-600 mt-1 truncate">
                              {track.artists.join(", ")}
                            </p>
                          )}

                          {/* Track Genres */}
                          {track.genres && track.genres.length > 0 && (
                            <div className="flex flex-wrap gap-1 mt-1">
                              {track.genres.slice(0, 2).map((genre, genreIndex) => (
                                <span
                                  key={genreIndex}
                                  className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700"
                                >
                                  {genre}
                                </span>
                              ))}
                              {track.genres.length > 2 && (
                                <span className="text-xs text-gray-500">
                                  +{track.genres.length - 2}
                                </span>
                              )}
                            </div>
                          )}
                        </div>

                        {/* Duration */}
                        {track.lengthSecs && track.lengthSecs > 0 && (
                          <div className="text-sm text-gray-500 ml-4 flex-shrink-0">
                            {formatDuration(track.lengthSecs)}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">
              <span className="text-2xl block mb-2">🎵</span>
              <p className="text-sm">No track listing available</p>
            </div>
          )}

          {/* Disc Total */}
          {disc.tracks && disc.tracks.length > 0 && getTotalDuration(disc.tracks) > 0 && (
            <div className="flex justify-between items-center mt-4 pt-4 border-t border-gray-200 text-sm text-gray-600">
              <span>{disc.tracks.length} tracks</span>
              <span>Total: {formatDuration(getTotalDuration(disc.tracks))}</span>
            </div>
          )}
        </div>
      ))}

      {/* Overall Total for Multi-Disc */}
      {media.length > 1 && (
        <div className="mt-6 pt-4 border-t border-gray-300">
          <div className="flex justify-between items-center text-sm font-medium text-gray-800">
            <span>
              {media.reduce((total, disc) => total + (disc.tracks?.length || 0), 0)} total tracks
            </span>
            <span>
              Total: {formatDuration(
                media.reduce((total, disc) => total + getTotalDuration(disc.tracks), 0)
              )}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
