"use client";

import { useState, useRef, useCallback } from "react";

/** A single image result from the Google image search proxy. */
export interface ImageSearchResult {
  title: string;
  imageUrl: string;
  thumbnailUrl: string;
  width: number;
  height: number;
}

/** State and actions returned by the hook. */
export interface UseImageSearchReturn {
  query: string;
  results: ImageSearchResult[];
  isLoading: boolean;
  error: string | null;
  /** Update the query text without triggering a search. */
  setQuery: (q: string) => void;
  /** Trigger a search with the current (or supplied) query. */
  search: (q?: string) => Promise<void>;
  /** Clear results and error state. */
  reset: () => void;
}

/**
 * Hook for searching web images via the backend Google image search proxy
 * (`GET /api/images/search?q=<query>`).
 *
 * The `query` ref is used inside the `search` callback to avoid stale closure
 * issues while still allowing the callback to be stable (no `query` in the
 * dependency array).
 */
export function useImageSearch(): UseImageSearchReturn {
  const [query, setQueryState] = useState<string>("");
  const queryRef = useRef<string>("");
  const [results, setResults] = useState<ImageSearchResult[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const setQuery = useCallback((q: string) => {
    queryRef.current = q;
    setQueryState(q);
  }, []);

  const search = useCallback(async (overrideQuery?: string) => {
    const q = (overrideQuery ?? queryRef.current).trim();
    if (!q) return;

    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch(
        `/api/images/search?q=${encodeURIComponent(q)}`
      );

      if (res.status === 204) {
        setResults([]);
        return;
      }

      if (!res.ok) {
        const text = await res.text().catch(() => "Unknown error");
        throw new Error(text || `Search failed (${res.status})`);
      }

      const data: ImageSearchResult[] = await res.json();
      setResults(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Image search failed.");
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const reset = useCallback(() => {
    setResults([]);
    setError(null);
  }, []);

  return { query, results, isLoading, error, setQuery, search, reset };
}
