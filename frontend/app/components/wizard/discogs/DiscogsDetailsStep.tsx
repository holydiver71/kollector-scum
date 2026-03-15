"use client";

/**
 * DiscogsDetailsStep – Step 3 of the Discogs add-release wizard.
 *
 * Fetches the full Discogs release payload for the selected result and
 * presents it for review.  The user can then either:
 *
 *   - **Add to Collection** – maps the release to a DTO and submits it
 *     directly, triggering image downloads afterwards.
 *   - **Edit Release** – hands the mapped data off to `AddReleaseWizard`
 *     so the user can make manual adjustments before saving.
 *
 * New-entity detection surfaces badges for artists, labels, genres,
 * countries, and formats that do not yet exist in the local database.
 */

import { useEffect, useState } from "react";
import Image from "next/image";
import { getDiscogsRelease } from "../../../lib/api";
import type { DiscogsRelease, DiscogsSearchResult } from "../../../lib/discogs-types";
import { mapDiscogsRelease } from "./mapDiscogsRelease";
import type { MappedDiscogsRelease } from "./mapDiscogsRelease";

interface DiscogsDetailsStepProps {
  /** The result selected in the previous step */
  searchResult: DiscogsSearchResult;
  /** Callback when the user confirms Add to Collection */
  onAddToCollection: (mapped: MappedDiscogsRelease) => void;
  /** Callback when the user chooses to edit the release manually */
  onEditRelease: (mapped: MappedDiscogsRelease) => void;
  /** Navigate back to the results step */
  onBack: () => void;
  /** Lists of entity names already in the database, used for new-entity detection */
  existingArtists: string[];
  existingLabels: string[];
  existingGenres: string[];
  existingCountries: string[];
  existingFormats: string[];
}

interface NewEntities {
  artists: string[];
  labels: string[];
  genres: string[];
  countries: string[];
  formats: string[];
}

/** Detect which entity names are not yet in the local database */
function detectNewEntities(
  release: DiscogsRelease,
  existingArtists: string[],
  existingLabels: string[],
  existingGenres: string[],
  existingCountries: string[],
  existingFormats: string[]
): NewEntities {
  const lower = (s: string) => s.toLowerCase();

  const artists = (release.artists ?? [])
    .map((a) => a.name)
    .filter((n) => !existingArtists.some((ea) => lower(ea) === lower(n)));

  const labels = (release.labels ?? [])
    .map((l) => l.name)
    .filter((n) => !existingLabels.some((el) => lower(el) === lower(n)));

  const allGenres = [...(release.genres ?? []), ...(release.styles ?? [])];
  const genres = allGenres.filter(
    (n) => !existingGenres.some((eg) => lower(eg) === lower(n))
  );

  const countries =
    release.country &&
    !existingCountries.some((ec) => lower(ec) === lower(release.country))
      ? [release.country]
      : [];

  const formats = (release.formats ?? [])
    .map((f) => f.name)
    .filter((n) => !existingFormats.some((ef) => lower(ef) === lower(n)));

  return { artists, labels, genres, countries, formats };
}

/** Small inline "New" badge */
function NewBadge() {
  return (
    <span className="ml-1 inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-500/15 text-emerald-400 border border-emerald-500/20">
      ✨ New
    </span>
  );
}

/** Details step component */
export default function DiscogsDetailsStep({
  searchResult,
  onAddToCollection,
  onEditRelease,
  onBack,
  existingArtists,
  existingLabels,
  existingGenres,
  existingCountries,
  existingFormats,
}: DiscogsDetailsStepProps) {
  const [release, setRelease] = useState<DiscogsRelease | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [newEntities, setNewEntities] = useState<NewEntities>({
    artists: [],
    labels: [],
    genres: [],
    countries: [],
    formats: [],
  });

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    getDiscogsRelease(searchResult.id)
      .then((data) => {
        if (cancelled) return;
        setRelease(data);
        setNewEntities(
          detectNewEntities(
            data,
            existingArtists,
            existingLabels,
            existingGenres,
            existingCountries,
            existingFormats
          )
        );
      })
      .catch((err) => {
        if (cancelled) return;
        setError(
          err instanceof Error ? err.message : "Failed to load release details"
        );
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchResult.id]);

  // ── Loading state ───────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <svg
          className="animate-spin h-8 w-8 text-[var(--theme-accent)]"
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
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
        <span className="ml-3 text-gray-400">Loading release details…</span>
      </div>
    );
  }

  // ── Error state ─────────────────────────────────────────────────────────────

  if (error) {
    return (
      <div className="space-y-4">
        <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4">
          <p className="text-sm text-red-400">{error}</p>
        </div>
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[var(--theme-card-border)] text-gray-300 hover:text-white transition-colors"
        >
          ← Back to Results
        </button>
      </div>
    );
  }

  if (!release) return null;

  // ── Derived values ───────────────────────────────────────────────────────────

  const primaryImage =
    release.images?.find((img) => img.type === "primary") ||
    release.images?.[0];

  const totalNewEntities =
    newEntities.artists.length +
    newEntities.labels.length +
    newEntities.genres.length +
    newEntities.countries.length +
    newEntities.formats.length;

  // ── Action handlers ──────────────────────────────────────────────────────────

  const handleAddToCollection = async () => {
    setIsAdding(true);
    try {
      const mapped = mapDiscogsRelease(release);
      await onAddToCollection(mapped);
    } finally {
      setIsAdding(false);
    }
  };

  const handleEditRelease = () => {
    const mapped = mapDiscogsRelease(release);
    onEditRelease(mapped);
  };

  // ── Render ───────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-black text-[var(--theme-foreground)]">
          {release.title}
        </h2>
        <p className="text-lg text-gray-400 mt-0.5">
          {(release.artists ?? []).map((a) => a.name).join(", ")}
        </p>
      </div>

      {/* New entities notice */}
      {totalNewEntities > 0 && (
        <div className="bg-blue-500/10 border border-blue-500/20 rounded-xl p-4">
          <p className="text-sm font-semibold text-blue-300 mb-2">
            {totalNewEntities} new{" "}
            {totalNewEntities === 1 ? "entry" : "entries"} will be created:
          </p>
          <ul className="text-sm text-blue-200 space-y-0.5 list-disc list-inside">
            {newEntities.artists.length > 0 && (
              <li>
                Artists: {newEntities.artists.join(", ")}
              </li>
            )}
            {newEntities.labels.length > 0 && (
              <li>Labels: {newEntities.labels.join(", ")}</li>
            )}
            {newEntities.genres.length > 0 && (
              <li>Genres: {newEntities.genres.join(", ")}</li>
            )}
            {newEntities.countries.length > 0 && (
              <li>Countries: {newEntities.countries.join(", ")}</li>
            )}
            {newEntities.formats.length > 0 && (
              <li>Formats: {newEntities.formats.join(", ")}</li>
            )}
          </ul>
        </div>
      )}

      {/* Main content grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Cover art */}
        <div className="md:col-span-1">
          {primaryImage ? (
            <div className="relative w-full aspect-square rounded-xl overflow-hidden bg-[#0F0F1A]">
              <Image
                src={primaryImage.uri}
                alt={`${release.title} cover`}
                fill
                sizes="(max-width: 768px) 100vw, 33vw"
                className="object-cover"
              />
            </div>
          ) : (
            <div className="w-full aspect-square rounded-xl bg-[#0F0F1A] flex items-center justify-center">
              <svg
                className="w-24 h-24 text-gray-700"
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
        <div className="md:col-span-2 space-y-4 text-sm">
          <div className="grid grid-cols-2 gap-x-6 gap-y-2">
            {release.year && (
              <div>
                <span className="text-gray-500">Year</span>
                <p className="text-[var(--theme-foreground)] font-medium">
                  {release.year}
                </p>
              </div>
            )}
            {release.country && (
              <div>
                <span className="text-gray-500">Country</span>
                <p className="text-[var(--theme-foreground)] font-medium">
                  {release.country}
                  {newEntities.countries.includes(release.country) && (
                    <NewBadge />
                  )}
                </p>
              </div>
            )}
          </div>

          {/* Labels */}
          {release.labels && release.labels.length > 0 && (
            <div>
              <span className="text-gray-500">
                {release.labels.length === 1 ? "Label" : "Labels"}
              </span>
              <div className="mt-0.5 space-y-0.5">
                {release.labels.map((label, idx) => (
                  <p key={idx} className="text-[var(--theme-foreground)]">
                    {label.name}
                    {label.catalogNumber && (
                      <span className="text-gray-500 font-mono text-xs ml-1">
                        ({label.catalogNumber})
                      </span>
                    )}
                    {newEntities.labels.includes(label.name) && <NewBadge />}
                  </p>
                ))}
              </div>
            </div>
          )}

          {/* Formats */}
          {release.formats && release.formats.length > 0 && (
            <div>
              <span className="text-gray-500">Format</span>
              <div className="mt-0.5 space-y-0.5">
                {release.formats.map((fmt, idx) => (
                  <p key={idx} className="text-[var(--theme-foreground)]">
                    {fmt.name}
                    {fmt.descriptions && fmt.descriptions.length > 0 && (
                      <span className="text-gray-500 text-xs ml-1">
                        ({fmt.descriptions.join(", ")})
                      </span>
                    )}
                    {newEntities.formats.includes(fmt.name) && <NewBadge />}
                  </p>
                ))}
              </div>
            </div>
          )}

          {/* Genres */}
          {(release.genres?.length > 0 || release.styles?.length > 0) && (
            <div>
              <span className="text-gray-500">Genres / Styles</span>
              <div className="mt-1 flex flex-wrap gap-1">
                {[...(release.genres ?? []), ...(release.styles ?? [])].map(
                  (genre, idx) => (
                    <span
                      key={idx}
                      className="inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-[#1C1C28] text-gray-300 border border-[var(--theme-card-border)]"
                    >
                      {genre}
                      {newEntities.genres.includes(genre) && <NewBadge />}
                    </span>
                  )
                )}
              </div>
            </div>
          )}

          {/* Tracklist */}
          {release.tracklist && release.tracklist.length > 0 && (
            <div>
              <span className="text-gray-500 block mb-1">
                Tracklist ({release.tracklist.length} tracks)
              </span>
              <div className="bg-[#0F0F1A] rounded-xl p-3 max-h-52 overflow-y-auto space-y-1">
                {release.tracklist.map((track, idx) => (
                  <div
                    key={idx}
                    className="flex justify-between items-start gap-2 text-xs"
                  >
                    <span className="text-[var(--theme-foreground)]">
                      <span className="text-gray-600 font-mono mr-1">
                        {track.position}
                      </span>
                      {track.title}
                    </span>
                    {track.duration && (
                      <span className="text-gray-500 flex-shrink-0 font-mono">
                        {track.duration}
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Footer actions */}
      <div className="flex flex-wrap items-center justify-between gap-4 pt-2 border-t border-[var(--theme-card-border)]">
        <button
          type="button"
          onClick={onBack}
          disabled={isAdding}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[var(--theme-card-border)] text-gray-300 hover:text-white hover:border-[var(--theme-accent)]/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
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

        <div className="flex gap-3 flex-wrap">
          <button
            type="button"
            onClick={handleEditRelease}
            disabled={isAdding}
            className="px-5 py-2.5 rounded-xl text-sm font-semibold border border-[var(--theme-card-border)] text-[var(--theme-foreground)] hover:bg-[var(--theme-sidebar-hover)] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Edit Release
          </button>

          <button
            type="button"
            onClick={handleAddToCollection}
            disabled={isAdding}
            className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-[var(--theme-accent)] text-white hover:opacity-90 disabled:opacity-60 disabled:cursor-not-allowed transition-all shadow-lg shadow-[var(--theme-accent)]/20"
          >
            {isAdding ? (
              <>
                <svg
                  className="animate-spin h-4 w-4"
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
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
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
