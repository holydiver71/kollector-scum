"use client";

import { useState, useCallback, useRef } from "react";
import { fetchJson } from "../../lib/api";

// ─── Types ────────────────────────────────────────────────────────────────────

/** A single cover-art candidate returned by GET /api/images/search. */
export interface CoverArtSearchResult {
  /** MusicBrainz Release ID */
  mbId: string;
  artist: string;
  title: string;
  year?: number;
  format?: string;
  country?: string;
  label?: string;
  /** Catalogue number (from Discogs or MusicBrainz) */
  catalogueNumber?: string;
  /** Full-resolution image URL from Cover Art Archive (null if unavailable) */
  imageUrl?: string;
  /** 250px thumbnail URL from Cover Art Archive (null if unavailable) */
  thumbnailUrl?: string;
  /**
   * Normalised confidence score 0.0–1.0.
   * Derived from the MusicBrainz search score or Discogs match.
   */
  confidence: number;
  /** Human-readable confidence label ("Exact match" | "Good match" | "Possible match") */
  confidenceLabel: string;
}

// ─── Hook ─────────────────────────────────────────────────────────────────────

interface UseImageSearchResult {
  /** Current search results (empty array when no search has been run). */
  results: CoverArtSearchResult[];
  /** True while the search request is in-flight. */
  isLoading: boolean;
  /** Error message when the last search failed, null otherwise. */
  error: string | null;
  /** Executes an immediate search for the given query string and optional catalogue number. */
  search: (query: string, catalogueNumber?: string) => Promise<void>;
  /** Clears results, error and loading state. */
  clear: () => void;
}

/**
 * Hook that manages cover-art image search state.
 *
 * - Calls `GET /api/images/search?q=<query>&catalogueNumber=<optional>` on the backend.
 * - Supports searching via MusicBrainz (free text) and Discogs (catalogue number).
 * - Cancels any in-flight request when a new search is triggered.
 * - Supports debounced auto-search via the returned `search` function.
 */
export function useImageSearch(): UseImageSearchResult {
  const [results, setResults] = useState<CoverArtSearchResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // AbortController to cancel stale in-flight requests
  const abortRef = useRef<AbortController | null>(null);

  const search = useCallback(async (query: string, catalogueNumber?: string) => {
    const trimmed = query.trim();
    if (!trimmed) {
      setResults([]);
      setError(null);
      return;
    }

    // Cancel any previous in-flight search
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      let url = `/api/images/search?q=${encodeURIComponent(trimmed)}&limit=8`;
      if (catalogueNumber?.trim()) {
        url += `&catalogueNumber=${encodeURIComponent(catalogueNumber.trim())}`;
      }

      const data = await fetchJson<CoverArtSearchResult[]>(
        url,
        { signal: controller.signal },
      );
      setResults(data ?? []);
    } catch (err: unknown) {
      if ((err as Error).name === "AbortError") {
        // Request was intentionally cancelled; do not update state
        return;
      }
      const apiErr = err as { status?: number };
      if (apiErr.status === 204) {
        // 204 No Content: no results found
        setResults([]);
        return;
      }
      setError("Could not fetch cover art. Please try again.");
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clear = useCallback(() => {
    abortRef.current?.abort();
    setResults([]);
    setError(null);
    setIsLoading(false);
  }, []);

  return { results, isLoading, error, search, clear };
}
