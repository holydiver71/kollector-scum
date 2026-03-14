"use client";

import { useState, useEffect } from "react";
import { fetchJson } from "../../lib/api";
import type { LookupItem } from "./types";

/** All lookup lists needed by the wizard panels */
export interface ReleaseLookups {
  artists: LookupItem[];
  labels: LookupItem[];
  genres: LookupItem[];
  countries: LookupItem[];
  formats: LookupItem[];
  packagings: LookupItem[];
  stores: LookupItem[];
}

/** Return value of the hook */
export interface UseReleaseLookupsResult extends ReleaseLookups {
  loading: boolean;
  error: string | null;
}

interface PagedResponse {
  items: LookupItem[];
}

const EMPTY: ReleaseLookups = {
  artists: [],
  labels: [],
  genres: [],
  countries: [],
  formats: [],
  packagings: [],
  stores: [],
};

/**
 * Fetches all lookup lists required by the Add Release wizard in a single
 * parallel batch on mount.
 *
 * Returns the lists alongside `loading` and `error` flags so callers can
 * show an appropriate loading state or error message while data is in flight.
 */
export function useReleaseLookups(): UseReleaseLookupsResult {
  const [lookups, setLookups] = useState<ReleaseLookups>(EMPTY);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const fetchAll = async () => {
      setLoading(true);
      setError(null);

      try {
        const [
          artistsRes,
          labelsRes,
          genresRes,
          countriesRes,
          formatsRes,
          packagingsRes,
          storesRes,
        ] = await Promise.all([
          fetchJson<PagedResponse>("/api/artists?pageSize=1000"),
          fetchJson<PagedResponse>("/api/labels?pageSize=1000"),
          fetchJson<PagedResponse>("/api/genres?pageSize=100"),
          fetchJson<PagedResponse>("/api/countries?pageSize=300"),
          fetchJson<PagedResponse>("/api/formats?pageSize=100"),
          fetchJson<PagedResponse>("/api/packagings?pageSize=100"),
          fetchJson<PagedResponse>("/api/stores?pageSize=1000"),
        ]);

        if (cancelled) return;

        setLookups({
          artists: artistsRes.items ?? [],
          labels: labelsRes.items ?? [],
          genres: genresRes.items ?? [],
          countries: countriesRes.items ?? [],
          formats: formatsRes.items ?? [],
          packagings: packagingsRes.items ?? [],
          stores: storesRes.items ?? [],
        });
      } catch (err) {
        if (cancelled) return;
        console.error("useReleaseLookups: failed to load lookup data:", err);
        setError("Failed to load form data. Please refresh the page.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchAll();

    return () => {
      cancelled = true;
    };
  }, []);

  return { ...lookups, loading, error };
}
