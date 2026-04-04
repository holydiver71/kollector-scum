"use client";

import { useEffect, useRef, useState } from "react";
import { useImageSearch, type CoverArtSearchResult } from "./useImageSearch";

// ─── Types ────────────────────────────────────────────────────────────────────

interface ImageSearchModalProps {
  /** Pre-populated search query (typically "{artist} {title} {year}"). */
  defaultQuery: string;
  /** Optional catalogue number to refine search via Discogs. */
  defaultCatalogueNumber?: string;
  /** Optional UPC/EAN barcode for highest-confidence barcode lookup (Tier 1 of waterfall). */
  barcode?: string;
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

// ─── Featured result card (barcode exact match) ───────────────────────────────

/**
 * Horizontal card displayed at the top of results when the search found a
 * barcode-matched release. Uses an emerald accent to visually distinguish it
 * from the "Other Editions" grid below.
 */
function FeaturedResultCard({
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
      className="w-full flex gap-4 rounded-xl border border-emerald-600/40 bg-[#0A0A12] hover:border-emerald-500 hover:bg-[#0D0D17] transition-all text-left overflow-hidden focus:outline-none focus:ring-2 focus:ring-emerald-500 p-3"
      aria-label={`Select ${result.artist} – ${result.title} (exact barcode match)`}
    >
      {/* Thumbnail */}
      <div className="shrink-0 w-20 h-20 rounded-lg bg-[#0F0F1A] flex items-center justify-center overflow-hidden">
        {result.thumbnailUrl && !imgError ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={result.thumbnailUrl}
            alt={`${result.artist} – ${result.title}`}
            className="w-full h-full object-cover"
            referrerPolicy="no-referrer"
            onError={() => setImgError(true)}
          />
        ) : (
          <svg className="w-8 h-8 text-gray-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
          </svg>
        )}
      </div>

      {/* Metadata */}
      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-center gap-2">
          <span className="inline-block text-[10px] font-semibold px-1.5 py-0.5 rounded border bg-emerald-600/20 text-emerald-400 border-emerald-600/40">
            Barcode Match
          </span>
          <ConfidenceBadge label={result.confidenceLabel} confidence={result.confidence} />
        </div>
        <p className="text-white text-sm font-semibold truncate leading-snug">{result.title}</p>
        <p className="text-gray-400 text-xs truncate">{result.artist}</p>
        <p className="text-gray-500 text-[11px]">
          {[result.year, result.format, result.country].filter(Boolean).join(" · ")}
        </p>
        {result.label && <p className="text-gray-600 text-[10px] truncate">{result.label}</p>}
        {result.catalogueNumber && (
          <p className="text-gray-600 text-[10px] truncate font-mono">{result.catalogueNumber}</p>
        )}
      </div>

      {/* CTA arrow */}
      <div className="shrink-0 self-center text-emerald-500 text-xs font-semibold whitespace-nowrap pr-1">
        Select →
      </div>
    </button>
  );
}

// ─── Regular result card ──────────────────────────────────────────────────────

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
 * On open, automatically fires a waterfall search:
 *   Tier 1 — barcode (UPC/EAN) via MusicBrainz barcode: field
 *   Tier 2 — catalogue number via Discogs
 *   Tier 3 — broad free-text via MusicBrainz
 *
 * Results are displayed with any barcode-matched release as a featured
 * "Best Match" card (emerald border) above an "Other Editions" grid.
 * The search box is labelled "Refine Search" so it's clear the initial
 * search has already run.
 */
export default function ImageSearchModal({
  defaultQuery,
  defaultCatalogueNumber,
  barcode,
  onSelect,
  onClose,
}: ImageSearchModalProps) {
  const [query, setQuery] = useState("");
  const [hasAutoSearched, setHasAutoSearched] = useState(false);
  const { results, isLoading, error, search, clear } = useImageSearch();
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Run initial waterfall search when the modal opens
  useEffect(() => {
    search(defaultQuery.trim(), defaultCatalogueNumber, barcode);
    setHasAutoSearched(true);
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
    debounceRef.current = setTimeout(() => {
      // Always pass barcode so the featured card persists while refining
      search(value, defaultCatalogueNumber, barcode);
    }, SEARCH_DEBOUNCE_MS);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (debounceRef.current) clearTimeout(debounceRef.current);
    search(query, defaultCatalogueNumber, barcode);
  };

  const handleSelect = (result: CoverArtSearchResult) => {
    onSelect(result.imageUrl ?? result.thumbnailUrl ?? "", result.thumbnailUrl ?? result.imageUrl ?? "");
  };

  // Split results: barcode match → featured; everything else → grid
  const bestMatch = results.find((r) => r.matchType === "barcode") ?? null;
  const otherResults = bestMatch ? results.filter((r) => r !== bestMatch) : results;

  // Describe which source(s) powered the auto-search
  const autoSearchSource = barcode
    ? "barcode, catalogue number & title"
    : defaultCatalogueNumber
    ? "catalogue number & title"
    : "title & artist";

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

        {/* Auto-search banner */}
        {hasAutoSearched && (
          <div className="px-5 py-2 bg-[#0A0A12] border-b border-[#1C1C28] text-[11px] text-gray-500">
            Auto-searched using <span className="text-gray-400">{autoSearchSource}</span>
          </div>
        )}

        {/* Refine search bar */}
        <form onSubmit={handleSearchSubmit} className="px-5 py-3 border-b border-[#1C1C28]">
          <label className="block text-[11px] font-semibold text-gray-500 mb-1.5 uppercase tracking-wider">
            Refine Search
          </label>
          <div className="flex gap-2">
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => handleQueryChange(e.target.value)}
              placeholder="Artist, album, year…"
              className="flex-1 bg-[#0F0F1A] border border-[#2A2A3C] rounded-xl px-4 py-2.5 text-white placeholder-gray-600 text-sm focus:outline-none focus:ring-1 focus:border-[#8B5CF6] focus:ring-[#8B5CF6] transition-colors"
            />
            <button
              type="submit"
              disabled={isLoading}
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
              <svg className="w-8 h-8 text-[#8B5CF6] animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
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
              {hasAutoSearched ? "No cover art found. Try refining your search." : "Enter a search query above."}
            </div>
          )}

          {!isLoading && !error && results.length > 0 && (
            <div className="space-y-4">
              {/* Tier 1 — Featured barcode match */}
              {bestMatch && (
                <div>
                  <p className="text-[11px] font-semibold text-emerald-500/80 uppercase tracking-wider mb-2">
                    Best Match
                  </p>
                  <FeaturedResultCard result={bestMatch} onSelect={() => handleSelect(bestMatch)} />
                </div>
              )}

              {/* Tier 2 & 3 — Other editions grid */}
              {otherResults.length > 0 && (
                <div>
                  {bestMatch && (
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
                      Other Editions
                    </p>
                  )}
                  <div
                    className="grid grid-cols-2 sm:grid-cols-4 gap-3 items-stretch"
                    role="list"
                    aria-label="Cover art search results"
                  >
                    {otherResults.map((result) => (
                      <div key={result.mbId} role="listitem" className="flex">
                        <ResultCard result={result} onSelect={() => handleSelect(result)} />
                      </div>
                    ))}
                  </div>
                </div>
              )}
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

