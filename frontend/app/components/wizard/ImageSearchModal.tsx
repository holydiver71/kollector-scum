"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { useImageSearch } from "../../hooks/useImageSearch";
import type { ImageSearchResult } from "../../hooks/useImageSearch";

interface ImageSearchModalProps {
  /** Pre-populated search query (e.g. "{artist} {title} {year} album cover"). */
  defaultQuery: string;
  /** Called with the chosen image URL when the user selects a result. */
  onSelect: (imageUrl: string) => void;
  /** Called when the user dismisses the modal without making a selection. */
  onClose: () => void;
}

/**
 * Modal overlay for searching web images via the backend Google image search
 * proxy.  Opens pre-filled with a suggested query and lets the user refine
 * the search and pick a result.
 *
 * Security: All images are rendered with `referrerPolicy="no-referrer"` so
 * no referer header is sent to third-party image hosts.
 */
export default function ImageSearchModal({
  defaultQuery,
  onSelect,
  onClose,
}: ImageSearchModalProps) {
  const { query, results, isLoading, error, setQuery, search, reset } =
    useImageSearch();

  const inputRef = useRef<HTMLInputElement>(null);
  const [hasSearched, setHasSearched] = useState(false);

  // Initialise with the default query and run the initial search
  useEffect(() => {
    setQuery(defaultQuery);
    if (defaultQuery.trim()) {
      search(defaultQuery).then(() => setHasSearched(true));
    }
    // Focus the input after mount
    inputRef.current?.focus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSearch = useCallback(async () => {
    await search();
    setHasSearched(true);
  }, [search]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter") handleSearch();
      if (e.key === "Escape") onClose();
    },
    [handleSearch, onClose]
  );

  const handleSelect = useCallback(
    (result: ImageSearchResult) => {
      onSelect(result.imageUrl);
      onClose();
    },
    [onSelect, onClose]
  );

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      role="dialog"
      aria-modal="true"
      aria-labelledby="image-search-modal-title"
    >
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/70 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
        data-testid="modal-backdrop"
      />

      {/* Panel */}
      <div className="relative bg-[#0D0D1A] border border-[#1C1C28] rounded-2xl shadow-2xl w-full max-w-3xl mx-4 flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[#1C1C28]">
          <h2
            id="image-search-modal-title"
            className="text-base font-bold text-white"
          >
            Search Web for Cover Art
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close image search"
            className="text-gray-500 hover:text-white transition-colors"
            data-testid="modal-close-button"
          >
            <svg className="w-5 h-5" viewBox="0 0 20 20" fill="currentColor">
              <path
                fillRule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
          </button>
        </div>

        {/* Search bar */}
        <div className="px-6 py-4 border-b border-[#1C1C28] flex gap-3">
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Search for album cover art…"
            className="flex-1 bg-[#0F0F1A] border border-[#2A2A3C] rounded-xl px-4 py-2.5 text-white placeholder-gray-600 text-sm focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
            data-testid="search-input"
          />
          <button
            type="button"
            onClick={handleSearch}
            disabled={isLoading || !query.trim()}
            className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-[#8B5CF6] hover:bg-[#7C3AED] disabled:opacity-50 disabled:cursor-not-allowed text-white transition-colors"
            data-testid="search-button"
          >
            {isLoading ? "Searching…" : "Search"}
          </button>
        </div>

        {/* Results */}
        <div className="flex-1 overflow-y-auto p-6">
          {/* Loading spinner */}
          {isLoading && (
            <div
              className="flex items-center justify-center py-16"
              data-testid="loading-spinner"
            >
              <div className="w-8 h-8 border-2 border-[#8B5CF6] border-t-transparent rounded-full animate-spin" />
            </div>
          )}

          {/* Error state */}
          {!isLoading && error && (
            <div
              className="text-center py-12"
              role="alert"
              data-testid="search-error"
            >
              <p className="text-red-400 text-sm">{error}</p>
              <button
                type="button"
                onClick={() => {
                  reset();
                  handleSearch();
                }}
                className="mt-3 text-xs text-[#8B5CF6] hover:underline"
              >
                Retry
              </button>
            </div>
          )}

          {/* Empty state (after a search) */}
          {!isLoading && !error && hasSearched && results.length === 0 && (
            <div
              className="text-center py-12 text-gray-500 text-sm"
              data-testid="empty-state"
            >
              No results found. Try refining your search.
            </div>
          )}

          {/* Result grid */}
          {!isLoading && !error && results.length > 0 && (
            <div
              className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3"
              data-testid="results-grid"
            >
              {results.map((result, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => handleSelect(result)}
                  title={result.title}
                  className="group relative aspect-square rounded-xl overflow-hidden bg-[#0F0F1A] border border-[#1C1C28] hover:border-[#8B5CF6] focus:outline-none focus:ring-2 focus:ring-[#8B5CF6] transition-all"
                  data-testid={`result-item-${idx}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={result.thumbnailUrl}
                    alt={result.title}
                    loading="lazy"
                    referrerPolicy="no-referrer"
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                    onError={(e) => {
                      (e.target as HTMLImageElement).style.display = "none";
                    }}
                  />
                  {/* Hover overlay */}
                  <div className="absolute inset-0 bg-[#8B5CF6]/0 group-hover:bg-[#8B5CF6]/20 transition-colors duration-200 flex items-end p-2 opacity-0 group-hover:opacity-100">
                    <span className="text-xs text-white truncate drop-shadow">
                      {result.title}
                    </span>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
