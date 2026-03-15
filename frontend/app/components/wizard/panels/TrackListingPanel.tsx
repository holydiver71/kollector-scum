"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import type { WizardFormData, WizardMedia, WizardTrack, ValidationErrors } from "../types";

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors (keyed as "track{discIndex}_{trackIndex}") */
  errors: ValidationErrors;
  /**
   * Called whenever the panel's local validation state changes so the wizard
   * shell can disable the Next button while invalid durations exist.
   */
  onErrors?: (errors: ValidationErrors) => void;
}

/** Format seconds as M:SS (e.g. 3 → "0:03", 207 → "3:27") */
function formatDuration(seconds?: number): string {
  if (seconds == null) return "";
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

/**
 * Parse a 1–2 digit minute + exactly 2 digit second string into seconds.
 * Returns undefined when the format is wrong or seconds ≥ 60.
 */
function parseDuration(value: string): number | undefined {
  if (!value.trim()) return undefined;
  const match = value.match(/^(\d{1,2}):(\d{2})$/);
  if (!match) return undefined;
  const mins = parseInt(match[1], 10);
  const secs = parseInt(match[2], 10);
  if (secs >= 60) return undefined;
  return mins * 60 + secs;
}

/**
 * Apply the M:SS mask to a raw string and the previous display value.
 * Rules:
 * - Only digits and `:` are kept.
 * - Minutes: max 2 digits. Colon is auto-inserted after the 2nd minute digit
 *   when the user is adding characters (not deleting).
 * - Seconds: max 2 digits after the colon.
 * - User may also type `:` manually after 1 minute digit.
 */
function applyDurationMask(raw: string, prevDisplay: string): string {
  const clean = raw.replace(/[^\d:]/g, "");
  const colonIdx = clean.indexOf(":");

  if (colonIdx === -1) {
    const digits = clean.slice(0, 2);
    // Auto-insert colon only when the user is typing (not deleting)
    if (digits.length === 2 && raw.length > prevDisplay.length) {
      return `${digits}:`;
    }
    return digits;
  }

  const mins = clean.slice(0, colonIdx).slice(0, 2);
  const secs = clean.slice(colonIdx + 1).replace(/\D/g, "").slice(0, 2);
  return `${mins}:${secs}`;
}

/**
 * Masked M:SS duration input.
 *
 * Typing behaviour:
 * - Filters to digits and `:` only.
 * - Colon is auto-inserted after the 2nd minute digit.
 * - Backspacing over an auto-inserted colon removes the preceding minute digit
 *   so the cursor doesn't appear stuck.
 * - Max: 2 minute digits + `:` + 2 second digits.
 *
 * Blur behaviour:
 * - Empty → valid (duration is optional).
 * - Complete `M:SS` / `MM:SS` with seconds 0–59 → normalises and commits.
 * - Anything else → red border + tooltip, calls onValidityChange(true).
 */
function DurationInput({
  value,
  onChange,
  onValidityChange,
}: {
  value?: number;
  onChange: (seconds?: number) => void;
  onValidityChange: (isInvalid: boolean) => void;
}) {
  const [display, setDisplay] = useState(() =>
    value != null ? formatDuration(value) : ""
  );
  const [invalid, setInvalid] = useState(false);

  // Sync display when the stored value is updated externally (e.g. Discogs pre-fill)
  useEffect(() => {
    setDisplay(value != null ? formatDuration(value) : "");
    setInvalid(false);
  }, [value]);

  const clearError = () => {
    if (invalid) {
      setInvalid(false);
      onValidityChange(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;

    // Special case: user backspaced over an auto-inserted colon.
    // display = "34:"  →  raw = "34"  →  step back one more minute digit.
    if (display.endsWith(":") && !raw.includes(":") && raw === display.slice(0, -1)) {
      setDisplay(raw.slice(0, -1));
      clearError();
      return;
    }

    setDisplay(applyDurationMask(raw, display));
    clearError();
  };

  const validate = () => {
    if (!display.trim()) {
      onChange(undefined);
      setInvalid(false);
      onValidityChange(false);
      return;
    }
    const parsed = parseDuration(display);
    if (parsed !== undefined) {
      onChange(parsed);
      setDisplay(formatDuration(parsed));
      setInvalid(false);
      onValidityChange(false);
    } else {
      setInvalid(true);
      onValidityChange(true);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      validate();
      e.currentTarget.blur();
    }
  };

  return (
    <div className="relative">
      <input
        type="text"
        inputMode="numeric"
        value={display}
        onChange={handleChange}
        onBlur={validate}
        onKeyDown={handleKeyDown}
        placeholder="M:SS"
        title={
          invalid
            ? "Invalid duration – use M:SS format with seconds 0–59 (e.g. 3:47)"
            : undefined
        }
        className={`w-16 bg-[#0F0F1A] border rounded px-2 py-1 text-xs text-right font-mono focus:outline-none focus:text-white placeholder-gray-600 transition-colors ${
          invalid
            ? "border-red-500 text-red-400 focus:border-red-500"
            : "border-transparent text-gray-200 focus:border-[#8B5CF6]"
        }`}
      />
      {invalid && (
        <p className="absolute right-0 top-full mt-1 w-44 text-[10px] text-red-400 bg-[#1C0A0A] border border-red-900/50 rounded px-2 py-1 z-10 leading-tight">
          Use M:SS with seconds 0–59
        </p>
      )}
    </div>
  );
}

/**
 * Panel 6 – Track Listing (optional).
 * Supports multiple discs with any number of tracks per disc.
 * Durations are stored as integer seconds internally and displayed as M:SS.
 */
export default function TrackListingPanel({ data, onChange, errors, onErrors }: Props) {
  const media: WizardMedia[] = data.media ?? [];

  // Ref tracks current invalid duration keys synchronously so callers see
  // the latest state even when blur and a button click fire in the same frame.
  const durationErrorsRef = useRef<ValidationErrors>({});

  const reportDurationError = useCallback(
    (key: string, isInvalid: boolean) => {
      const next = { ...durationErrorsRef.current };
      if (isInvalid) {
        next[key] = "Invalid duration";
      } else {
        delete next[key];
      }
      durationErrorsRef.current = next;
      onErrors?.(next);
    },
    [onErrors]
  );

  /** Remove all duration error keys belonging to a given disc */
  const clearDiscDurationErrors = useCallback(
    (discIndex: number) => {
      const next = Object.fromEntries(
        Object.entries(durationErrorsRef.current).filter(
          ([k]) => !k.startsWith(`duration_${discIndex}_`)
        )
      );
      durationErrorsRef.current = next;
      onErrors?.(next);
    },
    [onErrors]
  );

  /** Remove the duration error key for a single track */
  const clearTrackDurationError = useCallback(
    (discIndex: number, trackIndex: number) => {
      const next = { ...durationErrorsRef.current };
      delete next[`duration_${discIndex}_${trackIndex}`];
      durationErrorsRef.current = next;
      onErrors?.(next);
    },
    [onErrors]
  );

  const updateMedia = (updated: WizardMedia[]) => {
    // Re-index track numbers within each disc
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
    clearDiscDurationErrors(discIndex);
    updateMedia(media.filter((_, i) => i !== discIndex));
  };

  const updateDiscName = (discIndex: number, name: string) => {
    updateMedia(
      media.map((d, i) => (i === discIndex ? { ...d, name } : d))
    );
  };

  const addTrack = (discIndex: number) => {
    const newTrack: WizardTrack = {
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
    clearTrackDurationError(discIndex, trackIndex);
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
    patch: Partial<WizardTrack>
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
                  <span className="flex-1 text-[10px] font-semibold uppercase tracking-wider text-gray-500">
                    Track
                  </span>
                  <span className="w-16 text-right text-[10px] font-semibold uppercase tracking-wider text-gray-500">
                    Duration
                  </span>
                  <span className="w-4" />
                </div>
              )}
              {disc.tracks.map((track, trackIndex) => {
                const trackError = errors[`track${discIndex}_${trackIndex}`];
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
                    <DurationInput
                      value={track.lengthSecs}
                      onChange={(secs) =>
                        updateTrack(discIndex, trackIndex, { lengthSecs: secs })
                      }
                      onValidityChange={(isInvalid) =>
                        reportDurationError(`duration_${discIndex}_${trackIndex}`, isInvalid)
                      }
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
