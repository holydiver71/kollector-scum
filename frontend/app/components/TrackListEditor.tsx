"use client";

import { useState } from "react";

export interface Track {
  title: string;
  index: number;
  lengthSecs?: number;
  artists?: string[];
  genres?: string[];
  live?: boolean;
}

export interface Media {
  name?: string;
  tracks: Track[];
}

interface TrackListEditorProps {
  media: Media[];
  onChange: (media: Media[]) => void;
}

export default function TrackListEditor({ media, onChange }: TrackListEditorProps) {
  const [expandedMedia, setExpandedMedia] = useState<number>(0);

  const addMedia = () => {
    const newMedia: Media = {
      name: `Disc ${media.length + 1}`,
      tracks: [],
    };
    onChange([...media, newMedia]);
    setExpandedMedia(media.length);
  };

  const removeMedia = (mediaIndex: number) => {
    const newMedia = media.filter((_, i) => i !== mediaIndex);
    onChange(newMedia);
    if (expandedMedia >= newMedia.length) {
      setExpandedMedia(Math.max(0, newMedia.length - 1));
    }
  };

  const updateMedia = (mediaIndex: number, updatedMedia: Media) => {
    const newMedia = [...media];
    newMedia[mediaIndex] = updatedMedia;
    onChange(newMedia);
  };

  const addTrack = (mediaIndex: number) => {
    const currentMedia = media[mediaIndex];
    const newTrack: Track = {
      title: "",
      index: currentMedia.tracks.length + 1,
    };
    updateMedia(mediaIndex, {
      ...currentMedia,
      tracks: [...currentMedia.tracks, newTrack],
    });
  };

  const removeTrack = (mediaIndex: number, trackIndex: number) => {
    const currentMedia = media[mediaIndex];
    const newTracks = currentMedia.tracks.filter((_, i) => i !== trackIndex);
    // Reindex tracks
    const reindexedTracks = newTracks.map((track, i) => ({
      ...track,
      index: i + 1,
    }));
    updateMedia(mediaIndex, {
      ...currentMedia,
      tracks: reindexedTracks,
    });
  };

  const updateTrack = (mediaIndex: number, trackIndex: number, updatedTrack: Track) => {
    const currentMedia = media[mediaIndex];
    const newTracks = [...currentMedia.tracks];
    newTracks[trackIndex] = updatedTrack;
    updateMedia(mediaIndex, {
      ...currentMedia,
      tracks: newTracks,
    });
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const parseDuration = (durationString: string): number | undefined => {
    const parts = durationString.split(':');
    if (parts.length !== 2) return undefined;
    const mins = parseInt(parts[0], 10);
    const secs = parseInt(parts[1], 10);
    if (isNaN(mins) || isNaN(secs)) return undefined;
    return mins * 60 + secs;
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-[var(--theme-card-text)]">Track Listing</h3>
        {media.length === 0 && (
          <button
            type="button"
            onClick={addMedia}
            className="px-3 py-1 text-sm bg-[var(--theme-accent)] text-white rounded hover:bg-[var(--theme-accent-hover)]"
          >
            Add Disc/Media
          </button>
        )}
      </div>

      {media.map((disc, mediaIndex) => (
        <div key={mediaIndex} className="border border-[var(--theme-card-border)] rounded-md bg-[var(--theme-card-bg)]">
          <div
            className="flex items-center justify-between p-3 bg-[var(--theme-sidebar-hover)] cursor-pointer hover:opacity-90"
            onClick={() => setExpandedMedia(expandedMedia === mediaIndex ? -1 : mediaIndex)}
          >
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-[var(--theme-card-text)]">
                {disc.name || `Disc ${mediaIndex + 1}`}
              </span>
              <span className="text-xs text-[var(--theme-muted-text)]">
                ({disc.tracks.length} track{disc.tracks.length !== 1 ? 's' : ''})
              </span>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  removeMedia(mediaIndex);
                }}
                className="px-2 py-1 text-xs text-[var(--theme-error-text)] hover:opacity-80 hover:bg-[var(--theme-error-bg)] rounded"
              >
                Remove Disc
              </button>
              <span className="text-[var(--theme-muted-text)]">
                {expandedMedia === mediaIndex ? '▼' : '▶'}
              </span>
            </div>
          </div>

          {expandedMedia === mediaIndex && (
            <div className="p-3 space-y-3">
              <div>
                <label className="block text-xs font-medium text-[var(--theme-card-text)] mb-1">
                  Disc Name (Optional)
                </label>
                <input
                  type="text"
                  value={disc.name || ""}
                  onChange={(e) => updateMedia(mediaIndex, { ...disc, name: e.target.value })}
                  className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                  placeholder={`Disc ${mediaIndex + 1}`}
                />
              </div>

              <div className="space-y-2">
                {disc.tracks.map((track, trackIndex) => (
                  <div key={trackIndex} className="p-2 bg-[var(--theme-sidebar-hover)] rounded border border-[var(--theme-card-border)]">
                    <div className="grid grid-cols-12 gap-2 items-start">
                      <div className="col-span-1">
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">#</label>
                        <input
                          type="number"
                          value={track.index}
                          onChange={(e) => updateTrack(mediaIndex, trackIndex, {
                            ...track,
                            index: parseInt(e.target.value) || 1,
                          })}
                          className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                          min="1"
                        />
                      </div>

                      <div className="col-span-6">
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">Title *</label>
                        <input
                          type="text"
                          value={track.title}
                          onChange={(e) => updateTrack(mediaIndex, trackIndex, {
                            ...track,
                            title: e.target.value,
                          })}
                          className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                          placeholder="Track title"
                        />
                      </div>

                      <div className="col-span-2">
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">Duration</label>
                        <input
                          type="text"
                          value={track.lengthSecs ? formatDuration(track.lengthSecs) : ""}
                          onChange={(e) => {
                            const duration = parseDuration(e.target.value);
                            updateTrack(mediaIndex, trackIndex, {
                              ...track,
                              lengthSecs: duration,
                            });
                          }}
                          className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                          placeholder="M:SS"
                        />
                      </div>

                      <div className="col-span-2">
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">Live</label>
                        <input
                          type="checkbox"
                          checked={track.live || false}
                          onChange={(e) => updateTrack(mediaIndex, trackIndex, {
                            ...track,
                            live: e.target.checked,
                          })}
                          className="mt-2 h-4 w-4 text-[var(--theme-accent)] focus:ring-[var(--theme-accent)] border-[var(--theme-card-border)] rounded"
                        />
                      </div>

                      <div className="col-span-1 flex items-end">
                        <button
                          type="button"
                          onClick={() => removeTrack(mediaIndex, trackIndex)}
                          className="px-2 py-1 text-xs text-[var(--theme-error-text)] hover:opacity-80 hover:bg-[var(--theme-error-bg)] rounded"
                        >
                          ✕
                        </button>
                      </div>
                    </div>

                    <div className="mt-2 grid grid-cols-2 gap-2">
                      <div>
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">
                          Track Artists (comma-separated)
                        </label>
                        <input
                          type="text"
                          value={track.artists?.join(', ') || ""}
                          onChange={(e) => {
                            const artists = e.target.value
                              .split(',')
                              .map(a => a.trim())
                              .filter(a => a.length > 0);
                            updateTrack(mediaIndex, trackIndex, {
                              ...track,
                              artists: artists.length > 0 ? artists : undefined,
                            });
                          }}
                          className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                          placeholder="Leave empty to use album artists"
                        />
                      </div>

                      <div>
                        <label className="block text-xs text-[var(--theme-muted-text)] mb-1">
                          Track Genres (comma-separated)
                        </label>
                        <input
                          type="text"
                          value={track.genres?.join(', ') || ""}
                          onChange={(e) => {
                            const genres = e.target.value
                              .split(',')
                              .map(g => g.trim())
                              .filter(g => g.length > 0);
                            updateTrack(mediaIndex, trackIndex, {
                              ...track,
                              genres: genres.length > 0 ? genres : undefined,
                            });
                          }}
                          className="w-full px-2 py-1 text-sm border border-[var(--theme-card-border)] rounded bg-[var(--theme-input-bg)] text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)] focus:ring-1 focus:ring-[var(--theme-accent)]"
                          placeholder="Leave empty to use album genres"
                        />
                      </div>
                    </div>
                  </div>
                ))}

                <button
                  type="button"
                  onClick={() => addTrack(mediaIndex)}
                  className="w-full px-3 py-2 border-2 border-dashed border-[var(--theme-card-border)] rounded text-sm text-[var(--theme-muted-text)] hover:border-[var(--theme-accent)] hover:text-[var(--theme-card-text)]"
                >
                  + Add Track
                </button>
              </div>
            </div>
          )}
        </div>
      ))}

      {media.length > 0 && (
        <button
          type="button"
          onClick={addMedia}
          className="w-full px-4 py-2 border-2 border-dashed border-[var(--theme-card-border)] rounded text-[var(--theme-muted-text)] hover:border-[var(--theme-accent)] hover:text-[var(--theme-card-text)]"
        >
          + Add Another Disc/Media
        </button>
      )}
    </div>
  );
}
