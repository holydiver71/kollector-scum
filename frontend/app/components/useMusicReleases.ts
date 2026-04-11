"use client";

import { useState, useEffect, useCallback } from "react";
import { fetchJson, type ApiError } from "../lib/api";
import { clearAuthToken } from "../lib/auth";
import { withRetry } from "../lib/withRetry";

// ── Types ────────────────────────────────────────────────────────────────────

export interface MusicRelease {
  id: number;
  title: string;
  releaseYear: string;
  origReleaseYear?: string;
  artistNames?: string[];
  genreNames?: string[];
  labelName?: string;
  countryName?: string;
  formatName?: string;
  coverImageUrl?: string;
  dateAdded: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MusicReleaseFilters {
  search?: string;
  artistId?: number;
  genreId?: number;
  labelId?: number;
  countryId?: number;
  formatId?: number;
  live?: boolean;
  yearFrom?: number;
  yearTo?: number;
  sortBy?: string;
  sortOrder?: string;
  kollectionId?: number;
}

// ── Hook ─────────────────────────────────────────────────────────────────────

export interface UseMusicReleasesResult {
  releases: MusicRelease[];
  loading: boolean;
  error: string | null;
  currentPage: number;
  totalPages: number;
  totalCount: number;
  /** Navigate to a specific page (noop if out of range). */
  handlePageChange: (page: number) => void;
  /** Re-run the last fetch (e.g. from an error retry button). */
  refetch: () => void;
}

function buildQueryParams(
  filters: MusicReleaseFilters,
  pageSize: number,
  page: number
): URLSearchParams {
  return new URLSearchParams({
    "Pagination.PageNumber": page.toString(),
    "Pagination.PageSize": pageSize.toString(),
    ...(filters.search && { Search: filters.search }),
    ...(filters.artistId && { ArtistId: filters.artistId.toString() }),
    ...(filters.genreId && { GenreId: filters.genreId.toString() }),
    ...(filters.labelId && { LabelId: filters.labelId.toString() }),
    ...(filters.countryId && { CountryId: filters.countryId.toString() }),
    ...(filters.formatId && { FormatId: filters.formatId.toString() }),
    ...(filters.live !== undefined && { Live: filters.live.toString() }),
    ...(filters.yearFrom && { YearFrom: filters.yearFrom.toString() }),
    ...(filters.yearTo && { YearTo: filters.yearTo.toString() }),
    ...(filters.kollectionId && { KollectionId: filters.kollectionId.toString() }),
    ...(filters.sortBy && { SortBy: filters.sortBy }),
    ...(filters.sortOrder && { SortOrder: filters.sortOrder }),
  });
}

function formatFetchError(err: unknown): string {
  if (err && typeof err === "object") {
    const e = err as Record<string, unknown>;
    let msg = typeof e.message === "string" ? e.message : "Failed to load releases";
    if (typeof e.status === "number") msg += ` (status: ${e.status})`;
    if (e.details) {
      try {
        const d =
          typeof e.details === "string" ? e.details : JSON.stringify(e.details);
        msg += ` - ${d}`;
      } catch {
        // ignore stringify errors
      }
    }
    if (typeof e.url === "string") msg += ` [url: ${e.url}]`;
    return msg;
  }
  if (err instanceof Error) return err.message;
  return "Failed to load releases";
}

/**
 * Fetches a paginated music release list with automatic retry on transient
 * failures (network errors, 429, 5xx). Handles 401 by clearing the auth token
 * and redirecting to the home page.
 *
 * @param filters  Filter/sort parameters forwarded to the API.
 * @param pageSize Number of releases per page.
 */
export function useMusicReleases(
  filters: MusicReleaseFilters,
  pageSize: number
): UseMusicReleasesResult {
  // Apply default sort so the header control starts in a known state.
  const effectiveFilters: MusicReleaseFilters = {
    ...filters,
    sortBy: filters.sortBy ?? "title",
    sortOrder: filters.sortOrder ?? "asc",
  };

  const [releases, setReleases] = useState<MusicRelease[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);

  const fetchPage = useCallback(
    async (page: number) => {
      setLoading(true);
      setError(null);

      const params = buildQueryParams(effectiveFilters, pageSize, page);

      try {
        const response = await withRetry<PagedResult<MusicRelease> | MusicRelease[]>(
          () =>
            fetchJson<PagedResult<MusicRelease> | MusicRelease[]>(
              `/api/musicreleases?${params}`
            )
        );

        if (Array.isArray(response)) {
          setReleases(response);
          setCurrentPage(1);
          setTotalPages(1);
          setTotalCount(response.length);
        } else {
          const paged = response as PagedResult<MusicRelease>;
          setReleases(paged.items ?? []);
          setCurrentPage(paged.page ?? 1);
          setTotalPages(paged.totalPages ?? 0);
          setTotalCount(paged.totalCount ?? 0);
        }
      } catch (err) {
        const apiError = err as ApiError;
        if (apiError?.status === 401) {
          clearAuthToken();
          window.location.href = "/";
          return;
        }
        setError(formatFetchError(err));
      } finally {
        setLoading(false);
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [
      pageSize,
      // Spread individual filter fields so the callback updates only when a
      // filter value changes, not when the filters object reference changes.
      effectiveFilters.search,
      effectiveFilters.artistId,
      effectiveFilters.genreId,
      effectiveFilters.labelId,
      effectiveFilters.countryId,
      effectiveFilters.formatId,
      effectiveFilters.live,
      effectiveFilters.yearFrom,
      effectiveFilters.yearTo,
      effectiveFilters.sortBy,
      effectiveFilters.sortOrder,
      effectiveFilters.kollectionId,
    ]
  );

  // Reset to page 1 and fetch whenever the filters or page size change.
  useEffect(() => {
    setCurrentPage(1);
    fetchPage(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetchPage]);

  const handlePageChange = useCallback(
    (page: number) => {
      if (page < 1 || page > totalPages) return;
      setCurrentPage(page);
      fetchPage(page);
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    [totalPages, fetchPage]
  );

  const refetch = useCallback(() => {
    fetchPage(currentPage);
  }, [currentPage, fetchPage]);

  return {
    releases,
    loading,
    error,
    currentPage,
    totalPages,
    totalCount,
    handlePageChange,
    refetch,
  };
}
