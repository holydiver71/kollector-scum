"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import Link from "next/link";
import { getArtists, type ArtistItem } from "../lib/api";
import { LoadingSpinner } from "../components/LoadingComponents";
import { Search, ChevronLeft, ChevronRight, Music } from "lucide-react";

/** Number of artists displayed per page */
const PAGE_SIZE = 48;

/** Debounce delay in milliseconds for search input */
const SEARCH_DEBOUNCE_MS = 300;

/** All available letter filter options: '0-9' for numeric names, then A-Z. */
const LETTER_FILTERS = [
  "0-9",
  "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
  "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
];

/**
 * Artists page – displays a searchable, filterable, paginated grid of all
 * artists in the collection. Each artist card links through to the collection
 * filtered by that artist.
 */
export default function ArtistsPage() {
  const [artists, setArtists] = useState<ArtistItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [letterFilter, setLetterFilter] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  /** Load artists from the API whenever the page, debounced search, or letter filter changes. */
  const loadArtists = useCallback(async (
    currentPage: number,
    searchTerm: string,
    letter: string | null
  ) => {
    try {
      setLoading(true);
      setError(null);
      const data = await getArtists(
        searchTerm || undefined,
        currentPage,
        PAGE_SIZE,
        letter || undefined
      );
      setArtists(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load artists");
    } finally {
      setLoading(false);
    }
  }, []);

  // Reload when page, search term, or letter filter changes
  useEffect(() => {
    loadArtists(page, debouncedSearch, letterFilter);
  }, [page, debouncedSearch, letterFilter, loadArtists]);

  /** Clear the debounce timer on unmount to prevent memory leaks. */
  useEffect(() => {
    return () => {
      if (debounceTimer.current) {
        clearTimeout(debounceTimer.current);
      }
    };
  }, []);

  /** Debounce search input to avoid excessive API calls while typing. */
  const handleSearchChange = (value: string) => {
    setSearch(value);
    if (debounceTimer.current) {
      clearTimeout(debounceTimer.current);
    }
    debounceTimer.current = setTimeout(() => {
      setDebouncedSearch(value);
      setPage(1); // Reset to first page on new search
    }, SEARCH_DEBOUNCE_MS);
  };

  /**
   * Toggle the given letter filter. Selecting the active letter resets to
   * show all artists; selecting a new letter replaces the current filter.
   */
  const handleLetterButtonClick = (letter: string) => {
    setLetterFilter(prev => (prev === letter ? null : letter));
    setPage(1);
  };

  /** Handle the dropdown selector used on small screens. */
  const handleLetterDropdownChange = (value: string) => {
    setLetterFilter(value || null);
    setPage(1);
  };

  /** Clear all active filters and return to the default all-artists view. */
  const handleClearFilters = () => {
    setSearch("");
    setDebouncedSearch("");
    setLetterFilter(null);
    setPage(1);
  };

  /** Navigate to previous page. */
  const handlePreviousPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  /** Navigate to next page. */
  const handleNextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  /** Build a human-readable description of the active filters for empty states. */
  const activeFilterDescription = (() => {
    const parts: string[] = [];
    if (debouncedSearch) parts.push(`matching "${debouncedSearch}"`);
    if (letterFilter) parts.push(`starting with "${letterFilter}"`);
    return parts.length > 0 ? `No artists found ${parts.join(" ")}` : "No artists in your collection yet";
  })();

  return (
    <div className="min-h-screen bg-transparent">
      <div className="max-w-7xl mx-auto px-4 py-6">

        {/* Page header */}
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-white mb-1">Artists</h1>
          <p className="text-gray-400 text-sm">
            {totalCount > 0
              ? `${totalCount.toLocaleString()} artist${totalCount === 1 ? "" : "s"} in your collection`
              : "Browse artists in your collection"}
          </p>
        </div>

        {/* Search bar */}
        <div className="relative mb-4">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-500 pointer-events-none" />
          <input
            type="text"
            value={search}
            onChange={e => handleSearchChange(e.target.value)}
            placeholder="Search artists…"
            aria-label="Search artists"
            className="w-full bg-[#13131F] border border-[#1C1C28] rounded-lg pl-10 pr-4 py-2.5 text-white placeholder-gray-500 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
          />
        </div>

        {/* A-Z letter filter bar */}
        <div className="mb-6">
          {/* Dropdown selector – shown only on extra-small screens */}
          <div className="sm:hidden">
            <select
              value={letterFilter ?? ""}
              onChange={e => handleLetterDropdownChange(e.target.value)}
              aria-label="Filter artists by letter"
              className="w-full bg-[#13131F] border border-[#1C1C28] rounded-lg px-3 py-2 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
            >
              <option value="">All artists</option>
              {LETTER_FILTERS.map(l => (
                <option key={l} value={l}>{l}</option>
              ))}
            </select>
          </div>

          {/* Button bar – shown on sm and above, wraps to rows as needed */}
          <div className="hidden sm:flex flex-wrap gap-1" role="group" aria-label="Filter artists by letter">
            {LETTER_FILTERS.map(l => (
              <button
                key={l}
                onClick={() => handleLetterButtonClick(l)}
                aria-pressed={letterFilter === l}
                className={`min-w-[2.25rem] px-2 py-1 text-xs font-medium rounded transition-colors focus:outline-none focus:ring-1 focus:ring-[#8B5CF6] ${
                  letterFilter === l
                    ? "bg-[#8B5CF6] text-white border border-[#8B5CF6]"
                    : "bg-[#13131F] border border-[#1C1C28] text-gray-400 hover:text-white hover:border-[#8B5CF6]/50"
                }`}
              >
                {l}
              </button>
            ))}
          </div>
        </div>

        {/* Error state */}
        {error && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4 text-red-400 font-medium mb-6">
            {error}
          </div>
        )}

        {/* Loading state */}
        {loading ? (
          <div className="flex justify-center py-16">
            <LoadingSpinner size="large" color="white" />
          </div>
        ) : artists.length === 0 ? (
          /* Empty state */
          <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-12 text-center">
            <Music className="h-12 w-12 text-gray-600 mx-auto mb-4" />
            <p className="text-gray-400 text-lg font-medium">
              {activeFilterDescription}
            </p>
            {(debouncedSearch || letterFilter) && (
              <button
                onClick={handleClearFilters}
                className="mt-4 text-[#8B5CF6] hover:text-[#A78BFA] text-sm transition-colors"
              >
                Clear filters
              </button>
            )}
          </div>
        ) : (
          /* Artist grid */
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
            {artists.map(artist => (
              <Link
                key={artist.id}
                href={`/collection?artistId=${artist.id}`}
                className="group bg-[#13131F] border border-[#1C1C28] rounded-xl p-4 flex flex-col items-center text-center hover:border-[#8B5CF6]/50 hover:bg-[#8B5CF6]/5 transition-all"
                title={`Browse releases by ${artist.name}`}
              >
                {/* Artist avatar placeholder */}
                <div className="w-14 h-14 rounded-full bg-[#1C1C28] group-hover:bg-[#8B5CF6]/20 flex items-center justify-center mb-3 transition-colors">
                  <span className="text-xl font-bold text-gray-400 group-hover:text-[#8B5CF6] transition-colors select-none">
                    {artist.name.charAt(0).toUpperCase()}
                  </span>
                </div>
                <span className="text-sm font-medium text-gray-200 group-hover:text-white transition-colors line-clamp-2 leading-snug">
                  {artist.name}
                </span>
              </Link>
            ))}
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && !loading && (
          <div className="flex items-center justify-between mt-6 pt-4 border-t border-[#1C1C28]">
            <p className="text-sm text-gray-400">
              Page {page} of {totalPages}
            </p>
            <div className="flex gap-2">
              <button
                onClick={handlePreviousPage}
                disabled={page <= 1}
                aria-label="Previous page"
                className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-[#13131F] border border-[#1C1C28] text-gray-400 hover:text-white hover:border-[#8B5CF6]/50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <ChevronLeft className="h-4 w-4" />
                <span className="text-sm">Prev</span>
              </button>
              <button
                onClick={handleNextPage}
                disabled={page >= totalPages}
                aria-label="Next page"
                className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-[#13131F] border border-[#1C1C28] text-gray-400 hover:text-white hover:border-[#8B5CF6]/50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <span className="text-sm">Next</span>
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
