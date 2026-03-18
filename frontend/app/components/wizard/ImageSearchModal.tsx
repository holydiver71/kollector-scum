"use client";

import { useEffect, useRef, useState } from "react";
import { useImageSearch, type CoverArtSearchResult } from "./useImageSearch";

// ─── Types ────────────────────────────────────────────────────────────────────

interface ImageSearchModalProps {
  /** Pre-populated search query (typically "{artist} {title} {year}"). */
  defaultQuery: string;
  /** Optional catalogue number to refine search via Discogs. */
  defaultCatalogueNumber?: string;
  /** Called with the selected full-resolution image URL. */
  onSelect: (imageUrl: string, thumbnailUrl: string) => void;
  /** Called when the user closes the modal without selecting. */
  onClose: () => void;
}

// ─── Confidence badge ─────────────────────────────────────────────────────────

/**
 * Renders a small colour-coded badge indicating how closely the result
 * matches the search query (Exact, Good, or Possible).
 */
function ConfidenceBadge({ label, confidence }: { label: string; confidence: number }) {
  const colour =
    confidence >= 0.95 ? "bg-emerald-600/20 text-emerald-400 border-emerald-600/40" :
    confidence >= 0.75 ? "bg-blue-600/20 text-blue-400 border-blue-600/40" :
                         "bg-amber-600/20 text-amber-400 border-amber-600/40";

  return (
    <span
      className={`inline-block text-[10px] font-semibold px-1.5 py-0.5 rounded border ${colour}`}
    >
      {label}
    </span>
  );
}

// ─── Result card ─────────────────────────────────────────────────────────────

function ResultCard({
  result,
  onSelect,
}: {
  result: CoverArtSearchResult;
  onSelect: () => void;
}) {
  const [imgError, setImgError] = useState(false);

  return (
    <button
      type="button"
      onClick={onSelect}
      className="group flex flex-col h-full rounded-xl border border-[#1C1C28] bg-[#0A0A12] hover:border-[#8B5CF6]/60 hover:bg-[#0F0F1A] transition-all text-left overflow-hidden focus:outline-none focus:ring-2 focus:ring-[#8B5CF6]"
      aria-label={`Select ${result.artist} – ${result.title}`}
    >
      {/* Thumbnail */}
      <div className="relative w-full aspect-square bg-[#0F0F1A] flex items-center justify-center overflow-hidden">
        {result.thumbnailUrl && !imgError ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={result.thumbnailUrl}
            alt={`${result.artist} – ${result.title}`}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            referrerPolicy="no-referrer"
            onError={() => setImgError(true)}
          />
        ) : (
          <svg
            className="w-10 h-10 text-gray-700"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={1}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z"
            />
          </svg>
        )}
      </div>

      {/* Metadata */}
      <div className="p-2.5 space-y-1">
        <ConfidenceBadge label={result.confidenceLabel} confidence={result.confidence} />
        <p className="text-white text-xs font-semibold truncate leading-snug mt-1">
          {result.title}
        </p>
        <p className="text-gray-400 text-[11px] truncate">
          {result.artist}
        </p>
        <p className="text-gray-600 text-[10px]">
          {[result.year, result.format, result.country].filter(Boolean).join(" · ")}
        </p>
        {result.label && (
          <p className="text-gray-600 text-[10px] truncate">{result.label}</p>
        )}
        {result.catalogueNumber && (
          <p className="text-gray-600 text-[10px] truncate font-mono">{result.catalogueNumber}</p>
        )}
      </div>
    </button>
  );
}

/** Debounce delay (ms) for auto-search while typing. */
const SEARCH_DEBOUNCE_MS = 400;

/**
 * Full-screen overlay modal for searching and selecting album cover art.
 *
 * Features:
 * - Search bar pre-populated with the default query; pressing Enter triggers search.
 * - Optional catalogue number parameter to refine search via Discogs.
 * - Auto-search on input change (debounced 400 ms — "auto-search while typing").
 * - Up to 4 results displayed in a responsive grid with confidence indicators.
 * - Loading spinner, empty state and error state.
 */
export default function ImageSearchModal({
  defaultQuery,
  defaultCatalogueNumber,
  onSelect,
  onClose,
}: ImageSearchModalProps) {
  const [query, setQuery] = useState(defaultQuery);
  const { results, isLoading, error, search, clear } = useImageSearch();
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Run initial search when the modal opens
  useEffect(() => {
    if (defaultQuery.trim()) {
      search(defaultQuery.trim(), defaultCatalogueNumber);
    }
    inputRef.current?.focus();
    return () => clear();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Close on Escape key
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onClose]);

  const handleQueryChange = (value: string) => {
    setQuery(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    // Auto-search while typing (consideration #1 from Image Search Research)
    debounceRef.current = setTimeout(() => {
      search(value, defaultCatalogueNumber);
    }, SEARCH_DEBOUNCE_MS);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (debounceRef.current) clearTimeout(debounceRef.current);
    search(query, defaultCatalogueNumber);
  };

  const handleSelect = (result: CoverArtSearchResult) => {
    onSelect(result.imageUrl ?? result.thumbnailUrl ?? "", result.thumbnailUrl ?? result.imageUrl ?? "");
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/70 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal */}
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Search for album cover art"
        className="relative bg-[#13131F] border border-[#1C1C28] rounded-2xl shadow-2xl w-full max-w-3xl flex flex-col max-h-[90vh]"
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-[#1C1C28]">
          <h2 className="text-base font-bold text-white">Search for Cover Art</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="text-gray-500 hover:text-white transition-colors p-1 rounded-lg hover:bg-[#1C1C28]"
          >
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Search bar */}
        <form onSubmit={handleSearchSubmit} className="px-5 py-3 border-b border-[#1C1C28]">
          <div className="flex gap-2">
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => handleQueryChange(e.target.value)}
              placeholder="Search artist, album, year…"
              className="flex-1 bg-[#0F0F1A] border border-[#2A2A3C] rounded-xl px-4 py-2.5 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 focus:border-[#8B5CF6] focus:ring-[#8B5CF6] transition-colors"
            />
            <button
              type="submit"
              disabled={isLoading || !query.trim()}
              className="px-4 py-2.5 rounded-xl bg-[#8B5CF6] hover:bg-[#7C3AED] disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-semibold transition-colors"
            >
              {isLoading ? "Searching…" : "Search"}
            </button>
          </div>
        </form>

        {/* Results area */}
        <div className="overflow-y-auto flex-1 px-5 py-4 min-h-[200px]">
          {isLoading && (
            <div className="flex items-center justify-center h-40" role="status" aria-label="Searching…">
              <svg
                className="w-8 h-8 text-[#8B5CF6] animate-spin"
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
                  d="M4 12a8 8 0 018-8v8H4z"
                />
              </svg>
            </div>
          )}

          {!isLoading && error && (
            <div className="flex items-center justify-center h-40" role="alert">
              <p className="text-red-400 text-sm text-center">{error}</p>
            </div>
          )}

          {!isLoading && !error && results.length === 0 && (
            <div className="flex items-center justify-center h-40 text-gray-500 text-sm">
              {query.trim() ? "No cover art found for this search." : "Enter a search query above."}
            </div>
          )}

          {!isLoading && !error && results.length > 0 && (
            <div
              className="grid grid-cols-2 sm:grid-cols-4 gap-3 items-stretch"
              role="list"
              aria-label="Cover art search results"
            >
              {results.map((result) => (
                <div key={result.mbId} role="listitem" className="flex">
                  <ResultCard result={result} onSelect={() => handleSelect(result)} />
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer hint */}
        <div className="px-5 py-3 border-t border-[#1C1C28] text-xs text-gray-600 text-center">
          Results from{" "}
          <a
            href="https://musicbrainz.org"
            target="_blank"
            rel="noopener noreferrer"
            className="text-[#8B5CF6]/70 hover:text-[#8B5CF6] transition-colors"
          >
            MusicBrainz
          </a>{" "}
          &amp;{" "}
          <a
            href="https://coverartarchive.org"
            target="_blank"
            rel="noopener noreferrer"
            className="text-[#8B5CF6]/70 hover:text-[#8B5CF6] transition-colors"
          >
            Cover Art Archive
          </a>
          . Free &amp; open data.
        </div>
      </div>
    </div>
  );
}
