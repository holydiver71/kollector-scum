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

  const isMultiDisc = media.length > 1;

  return (
    <div className="space-y-8">
      {/* Multi-disc: 2 column grid layout with custom proportions */}
      {isMultiDisc ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {media.map((disc, discIndex) => (
            <div key={discIndex}>
              {/* Disc Header */}
              <div className="flex items-center justify-between mb-6 pb-2 border-b border-gray-200">
                <h4 className="text-xs uppercase tracking-widest text-gray-900 font-bold">
                  {disc.name || `Disc ${discIndex + 1}`}
                </h4>
                {disc.tracks && disc.tracks.length > 0 && (
                  <div className="text-xs text-gray-500 font-semibold">
                    {disc.tracks.length} tracks
                    {getTotalDuration(disc.tracks) > 0 && (
                      <span> • {formatDuration(getTotalDuration(disc.tracks))}</span>
                    )}
                  </div>
                )}
              </div>

              {/* Track List */}
              {disc.tracks && disc.tracks.length > 0 ? (
                <div className="space-y-3">
                  {disc.tracks
                    .sort((a, b) => a.index - b.index)
                    .map((track, trackIndex) => (
                      <div
                        key={trackIndex}
                        className="flex items-start gap-4 group"
                      >
                        {/* Track Number */}
                        <div className="w-6 text-xs text-gray-500 text-right flex-shrink-0 pt-0.5 font-semibold">
                          {String(track.index).padStart(2, '0')}
                        </div>

                        {/* Track Info */}
                        <div className="flex-grow min-w-0">
                          <div className="flex items-start justify-between gap-4">
                            <div className="flex-grow min-w-0">
                              <h5 className="text-sm text-gray-900 font-medium leading-relaxed">
                                {track.title}
                                {track.live && (
                                  <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-gray-200 text-gray-600 font-medium">
                                    LIVE
                                  </span>
                                )}
                              </h5>
                              
                              {/* Track Artists (if different from main artist) */}
                              {track.artists && track.artists.length > 0 && areArtistsDifferent(track.artists, albumArtists) && (
                                <p className="text-xs text-gray-500 mt-1 font-medium">
                                  {track.artists.join(", ")}
                                </p>
                              )}
                            </div>

                            {/* Duration */}
                            {(track.lengthSecs && track.lengthSecs > 0) ? (
                              <div className="text-xs text-gray-500 flex-shrink-0 pt-0.5 font-semibold tabular-nums">
                                {formatDuration(track.lengthSecs)}
                              </div>
                            ) : null}
                          </div>
                        </div>
                      </div>
                    ))}
                </div>
              ) : (
                <div className="text-center py-12 text-gray-400">
                  <span className="text-2xl block mb-2">♪</span>
                  <p className="text-xs uppercase tracking-widest font-semibold">No track listing available</p>
                </div>
              )}
            </div>
          ))}
        </div>
      ) : (
        /* Single disc: standard layout */
        media.map((disc, discIndex) => (
          <div key={discIndex}>
            {/* Track List */}
            {disc.tracks && disc.tracks.length > 0 ? (
              <div className="space-y-3">
                {disc.tracks
                  .sort((a, b) => a.index - b.index)
                  .map((track, trackIndex) => (
                    <div
                      key={trackIndex}
                      className="flex items-start gap-4 group"
                    >
                      {/* Track Number */}
                      <div className="w-6 text-xs text-gray-500 text-right flex-shrink-0 pt-0.5 font-semibold">
                        {String(track.index).padStart(2, '0')}
                      </div>

                      {/* Track Info */}
                      <div className="flex-grow min-w-0">
                        <div className="flex items-start justify-between gap-4">
                          <div className="flex-grow min-w-0">
                            <h5 className="text-sm text-gray-900 font-medium leading-relaxed">
                              {track.title}
                              {track.live && (
                                <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-gray-200 text-gray-600 font-medium">
                                  LIVE
                                </span>
                              )}
                            </h5>
                            
                            {/* Track Artists (if different from main artist) */}
                            {track.artists && track.artists.length > 0 && areArtistsDifferent(track.artists, albumArtists) && (
                              <p className="text-xs text-gray-500 mt-1 font-medium">
                                {track.artists.join(", ")}
                              </p>
                            )}
                          </div>

                          {/* Duration */}
                          {(track.lengthSecs && track.lengthSecs > 0) ? (
                            <div className="text-xs text-gray-500 flex-shrink-0 pt-0.5 font-semibold tabular-nums">
                              {formatDuration(track.lengthSecs)}
                            </div>
                          ) : null}
                        </div>
                      </div>
                    </div>
                  ))}
              </div>
            ) : (
              <div className="text-center py-12 text-gray-400">
                <span className="text-2xl block mb-2">♪</span>
                <p className="text-xs uppercase tracking-widest font-semibold">No track listing available</p>
              </div>
            )}
          </div>
        ))
      )}

      {/* Overall Total for Multi-Disc */}
      {isMultiDisc && (
        (() => {
          const totalTracks = media.reduce((total, disc) => total + (disc.tracks?.length || 0), 0);
          const totalDuration = media.reduce((total, disc) => total + getTotalDuration(disc.tracks), 0);
          
          return (
            <div className="mt-8 pt-4 border-t border-gray-200">
              <div className="flex justify-between items-center text-xs text-gray-600 uppercase tracking-widest font-bold">
                <span>{totalTracks} total tracks</span>
                {totalDuration > 0 && (
                  <span className="tabular-nums">{formatDuration(totalDuration)}</span>
                )}
              </div>
            </div>
          );
        })()
      )}
    </div>
  );
}
