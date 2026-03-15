"use client";

/**
 * DiscogsDetailsStep
 *
 * Step 3 of the Discogs add-release wizard.  Fetches the full release payload
 * for the selected search result and presents a detailed preview.
 *
 * From this step the user can:
 *  - Go back to the results list (preserves selection)
 *  - Add directly to collection (maps data, POSTs, triggers image download)
 *  - Edit release (maps data into the manual AddReleaseWizard)
 */

import { useEffect, useState } from "react";
import Image from "next/image";
import { getDiscogsRelease } from "../../../lib/api";
import type { DiscogsRelease, DiscogsSearchResult } from "../../../lib/discogs-types";

export interface DiscogsDetailsStepProps {
  /** The result selected in step 2. */
  searchResult: DiscogsSearchResult;
  /** Called when the user presses "Back to Results". */
  onBack: () => void;
  /**
   * Called with the full release payload when the user chooses
   * "Add to Collection".
   */
  onAddToCollection: (release: DiscogsRelease) => void;
  /**
   * Called with the full release payload when the user chooses
   * "Edit Release".  The parent maps the data and opens the manual wizard.
   */
  onEditRelease: (release: DiscogsRelease) => void;
  /** Whether the add-to-collection action is in progress. */
  isAdding?: boolean;
}

/**
 * Step 3 of the Discogs import wizard – full release preview with actions.
 */
export default function DiscogsDetailsStep({
  searchResult,
  onBack,
  onAddToCollection,
  onEditRelease,
  isAdding = false,
}: DiscogsDetailsStepProps) {
  const [release, setRelease] = useState<DiscogsRelease | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const fetchRelease = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await getDiscogsRelease(searchResult.id);
        if (!cancelled) setRelease(data);
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : "Failed to load release details"
          );
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    fetchRelease();
    return () => {
      cancelled = true;
    };
  }, [searchResult.id]);

  // ── Loading ────────────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16 gap-4">
        <svg
          className="animate-spin h-10 w-10 text-[#8B5CF6]"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-label="Loading release details"
          role="img"
        >
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
        <p className="text-sm text-gray-400">Loading release details…</p>
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────

  if (error) {
    return (
      <div className="space-y-4">
        <div
          role="alert"
          className="flex gap-3 bg-red-500/10 border border-red-500/30 rounded-xl p-4"
        >
          <svg
            className="h-5 w-5 text-red-400 flex-shrink-0 mt-0.5"
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fillRule="evenodd"
              d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
              clipRule="evenodd"
            />
          </svg>
          <div>
            <p className="text-sm font-medium text-red-300">
              Error Loading Details
            </p>
            <p className="mt-1 text-sm text-red-400">{error}</p>
          </div>
        </div>
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 transition-colors"
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
              d="M15.75 19.5L8.25 12l7.5-7.5"
            />
          </svg>
          Back to Results
        </button>
      </div>
    );
  }

  if (!release) return null;

  const primaryImage =
    release.images.find((img) => img.type === "primary") ?? release.images[0];
  const allGenres = [...release.genres, ...release.styles];

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-xl font-black text-white mb-0.5">{release.title}</h2>
        <p className="text-sm text-gray-400">
          {release.artists.map((a) => a.name).join(", ")}
        </p>
      </div>

      {/* Main content grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Cover art */}
        <div className="md:col-span-1">
          {primaryImage ? (
            <div className="relative w-full aspect-square bg-[#1C1C28] rounded-xl overflow-hidden">
              <Image
                src={primaryImage.uri}
                alt={`${release.title} cover`}
                fill
                sizes="(max-width: 768px) 100vw, 33vw"
                className="object-cover"
              />
            </div>
          ) : (
            <div className="w-full aspect-square bg-[#1C1C28] rounded-xl flex items-center justify-center">
              <svg
                className="w-20 h-20 text-gray-700"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3"
                />
              </svg>
            </div>
          )}
        </div>

        {/* Metadata */}
        <div className="md:col-span-2 space-y-4">
          <div className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
            {release.year && (
              <div>
                <span className="text-gray-500">Year:</span>{" "}
                <span className="text-white">{release.year}</span>
              </div>
            )}
            {release.country && (
              <div>
                <span className="text-gray-500">Country:</span>{" "}
                <span className="text-white">{release.country}</span>
              </div>
            )}
            {release.labels.length > 0 && (
              <div className="col-span-2">
                <span className="text-gray-500">Labels:</span>{" "}
                {release.labels.map((l, i) => (
                  <span key={i} className="text-white mr-2">
                    {l.name}
                    {l.catalogNumber && (
                      <span className="text-gray-500 font-mono text-xs ml-1">
                        ({l.catalogNumber})
                      </span>
                    )}
                  </span>
                ))}
              </div>
            )}
            {release.formats.length > 0 && (
              <div className="col-span-2">
                <span className="text-gray-500">Format:</span>{" "}
                {release.formats.map((f, i) => (
                  <span key={i} className="text-white mr-2">
                    {f.name}
                    {f.descriptions?.length ? (
                      <span className="text-gray-500 text-xs ml-1">
                        ({f.descriptions.join(", ")})
                      </span>
                    ) : null}
                  </span>
                ))}
              </div>
            )}
            {allGenres.length > 0 && (
              <div className="col-span-2">
                <span className="text-gray-500">Genres:</span>{" "}
                <span className="text-white">{allGenres.join(", ")}</span>
              </div>
            )}
          </div>

          {/* Tracklist */}
          {release.tracklist?.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold text-gray-400 mb-2 uppercase tracking-wider">
                Tracklist
              </h3>
              <div className="bg-[#0F0F1A] border border-[#1C1C28] rounded-xl p-3 max-h-56 overflow-y-auto">
                <ol className="space-y-1 text-sm">
                  {release.tracklist.map((track, idx) => (
                    <li
                      key={idx}
                      className="flex justify-between items-baseline gap-2"
                    >
                      <span>
                        <span className="text-gray-600 font-mono mr-2">
                          {track.position}
                        </span>
                        <span className="text-gray-300">{track.title}</span>
                      </span>
                      {track.duration && (
                        <span className="text-gray-600 flex-shrink-0">
                          {track.duration}
                        </span>
                      )}
                    </li>
                  ))}
                </ol>
              </div>
            </div>
          )}

          {/* Notes */}
          {release.notes && (
            <div>
              <h3 className="text-sm font-semibold text-gray-400 mb-2 uppercase tracking-wider">
                Notes
              </h3>
              <p className="text-sm text-gray-400 bg-[#0F0F1A] border border-[#1C1C28] rounded-xl p-3 line-clamp-4">
                {release.notes}
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Action bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-[#1C1C28]">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 transition-colors"
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
              d="M15.75 19.5L8.25 12l7.5-7.5"
            />
          </svg>
          Back to Results
        </button>

        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => onEditRelease(release)}
            disabled={isAdding}
            className="px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Edit Release
          </button>
          <button
            type="button"
            onClick={() => onAddToCollection(release)}
            disabled={isAdding}
            className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-[#8B5CF6] hover:bg-[#7C3AED] text-white shadow-lg shadow-[#8B5CF6]/20 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
          >
            {isAdding ? (
              <>
                <svg
                  className="animate-spin h-4 w-4"
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Adding…
              </>
            ) : (
              "Add to Collection"
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
