"use client";

import type { MockFormData, MockMedia, MockTrack, ValidationErrors } from "../types";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/** Format seconds as M:SS */
function formatDuration(seconds?: number): string {
  if (!seconds) return "";
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

/** Parse a M:SS string into seconds */
function parseDuration(value: string): number | undefined {
  if (!value.trim()) return undefined;
  const parts = value.split(":").map(Number);
  if (parts.length === 2 && !isNaN(parts[0]) && !isNaN(parts[1])) {
    return parts[0] * 60 + parts[1];
  }
  return undefined;
}

/**
 * Panel 8 – Track Listing (optional).
 * Supports multiple discs with any number of tracks per disc.
 * Mirrors the field structure of TrackListEditor without requiring that component.
 */
export default function TrackListingPanel({ data, onChange, errors }: Props) {
  const media: MockMedia[] = data.media ?? [];

  const updateMedia = (updated: MockMedia[]) => {
    // Re-index track indices within each disc
    const reindexed = updated.map((disc) => ({
      ...disc,
      tracks: disc.tracks.map((t, i) => ({ ...t, index: i + 1 })),
    }));
    onChange({ media: reindexed });
  };

  const addDisc = () => {
    updateMedia([
      ...media,
      { name: `Disc ${media.length + 1}`, tracks: [] },
    ]);
  };

  const removeDisc = (discIndex: number) => {
    updateMedia(media.filter((_, i) => i !== discIndex));
  };

  const updateDiscName = (discIndex: number, name: string) => {
    updateMedia(
      media.map((d, i) => (i === discIndex ? { ...d, name } : d))
    );
  };

  const addTrack = (discIndex: number) => {
    const newTrack: MockTrack = {
      title: "",
      index: (media[discIndex]?.tracks.length ?? 0) + 1,
    };
    updateMedia(
      media.map((d, i) =>
        i === discIndex ? { ...d, tracks: [...d.tracks, newTrack] } : d
      )
    );
  };

  const removeTrack = (discIndex: number, trackIndex: number) => {
    updateMedia(
      media.map((d, i) =>
        i === discIndex
          ? { ...d, tracks: d.tracks.filter((_, ti) => ti !== trackIndex) }
          : d
      )
    );
  };

  const updateTrack = (
    discIndex: number,
    trackIndex: number,
    patch: Partial<MockTrack>
  ) => {
    updateMedia(
      media.map((d, i) =>
        i === discIndex
          ? {
              ...d,
              tracks: d.tracks.map((t, ti) =>
                ti === trackIndex ? { ...t, ...patch } : t
              ),
            }
          : d
      )
    );
  };

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Add one disc for a standard release, or multiple discs for box sets,
        double albums etc. Durations are optional and should be entered as{" "}
        <span className="font-mono text-xs text-gray-400">M:SS</span> (e.g.{" "}
        <span className="font-mono text-xs text-gray-400">3:47</span>).
      </p>

      {/* Disc list */}
      <div className="space-y-5">
        {media.map((disc, discIndex) => (
          <div
            key={discIndex}
            className="bg-[#0F0F1A] border border-[#2A2A3C] rounded-xl overflow-hidden"
          >
            {/* Disc header */}
            <div className="flex items-center gap-3 px-4 py-3 border-b border-[#2A2A3C] bg-[#0A0A12]">
              <svg
                className="w-4 h-4 text-[#8B5CF6] flex-shrink-0"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M9 9l10.5-3m0 6.553v3.75a2.25 2.25 0 01-1.632 2.163l-1.32.377a1.803 1.803 0 11-.99-3.467l2.31-.66a2.25 2.25 0 001.632-2.163zm0 0V2.25L9 5.25v10.303m0 0v3.75a2.25 2.25 0 01-1.632 2.163l-1.32.377a1.803 1.803 0 01-.99-3.467l2.31-.66A2.25 2.25 0 009 15.553z"
                />
              </svg>
              <input
                type="text"
                value={disc.name}
                onChange={(e) => updateDiscName(discIndex, e.target.value)}
                className="flex-1 bg-transparent text-sm font-semibold text-white focus:outline-none placeholder-gray-600"
                placeholder={`Disc ${discIndex + 1} name`}
              />
              <button
                type="button"
                onClick={() => removeDisc(discIndex)}
                className="text-gray-600 hover:text-red-400 transition-colors text-xs px-2 py-1 rounded hover:bg-red-400/10"
              >
                Remove disc
              </button>
            </div>

            {/* Track list */}
            <div className="divide-y divide-[#1C1C28]">
              {/* Column headers */}
              {disc.tracks.length > 0 && (
                <div className="flex items-center gap-3 px-4 pt-2.5 pb-1">
                  <span className="w-5" />
                  <span className="flex-1 text-[10px] font-semibold uppercase tracking-wider text-[#A78BFA]/40">Track</span>
                  <span className="w-16 text-right text-[10px] font-semibold uppercase tracking-wider text-[#A78BFA]/40">Duration</span>
                  <span className="w-4" />
                </div>
              )}
              {disc.tracks.map((track, trackIndex) => {
                const trackError =
                  errors[`track${discIndex}_${trackIndex}`];
                return (
                  <div
                    key={trackIndex}
                    className="flex items-center gap-3 px-4 py-2.5 group"
                  >
                    {/* Index */}
                    <span className="text-xs text-[#A78BFA]/40 w-5 text-right flex-shrink-0 font-mono">
                      {track.index}
                    </span>

                    {/* Title */}
                    <input
                      type="text"
                      value={track.title}
                      onChange={(e) =>
                        updateTrack(discIndex, trackIndex, {
                          title: e.target.value,
                        })
                      }
                      placeholder="Track title"
                      className={`flex-1 bg-transparent text-sm text-white focus:outline-none placeholder-gray-700 border-b pb-0.5 transition-colors ${
                        trackError
                          ? "border-red-500"
                          : "border-transparent focus:border-[#8B5CF6]"
                      }`}
                    />

                    {/* Duration */}
                    <input
                      type="text"
                      value={
                        track.lengthSecs
                          ? formatDuration(track.lengthSecs)
                          : ""
                      }
                      onChange={(e) =>
                        updateTrack(discIndex, trackIndex, {
                          lengthSecs: parseDuration(e.target.value),
                        })
                      }
                      placeholder="0:00"
                      className="w-16 bg-transparent text-xs text-gray-500 text-right font-mono focus:outline-none focus:text-white placeholder-gray-700 border-b border-transparent focus:border-[#8B5CF6] pb-0.5"
                    />

                    {/* Remove */}
                    <button
                      type="button"
                      onClick={() => removeTrack(discIndex, trackIndex)}
                      className="text-gray-700 hover:text-red-400 transition-colors opacity-0 group-hover:opacity-100"
                      aria-label="Remove track"
                    >
                      <svg
                        className="w-4 h-4"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        strokeWidth={2}
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          d="M6 18L18 6M6 6l12 12"
                        />
                      </svg>
                    </button>
                  </div>
                );
              })}
            </div>

            {/* Add track */}
            <button
              type="button"
              onClick={() => addTrack(discIndex)}
              className="w-full flex items-center gap-2 px-4 py-3 text-sm text-gray-500 hover:text-[#A78BFA] hover:bg-[#8B5CF6]/5 transition-colors border-t border-[#2A2A3C]"
            >
              <svg
                className="w-4 h-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M12 4.5v15m7.5-7.5h-15"
                />
              </svg>
              Add track
            </button>
          </div>
        ))}
      </div>

      {/* Add disc */}
      <button
        type="button"
        onClick={addDisc}
        className="flex items-center gap-2 text-sm font-semibold text-[#A78BFA] hover:text-[#8B5CF6] border border-dashed border-[#8B5CF6]/40 hover:border-[#8B5CF6] rounded-lg px-4 py-3 w-full justify-center transition-colors"
      >
        <svg
          className="w-4 h-4"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2}
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M12 4.5v15m7.5-7.5h-15"
          />
        </svg>
        Add {media.length === 0 ? "a disc" : "another disc"}
      </button>

      {media.length === 0 && (
        <p className="text-center text-xs text-gray-700">
          No discs added yet. Click the button above to start.
        </p>
      )}
    </div>
  );
}
