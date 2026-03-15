"use client";

import React, { useState, useEffect } from "react";
import { fetchJson } from "../../lib/api";
import type { ApiError } from "../../lib/api";
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
 * Fetches a single lookup endpoint with exponential back-off on 429 / 5xx.
 * Returns an empty array on unrecoverable failure so one bad endpoint does
 * not prevent the rest of the wizard from loading.
 */
async function fetchLookup(path: string): Promise<LookupItem[]> {
  const maxAttempts = 3;
  let attempt = 0;
  while (attempt < maxAttempts) {
    attempt += 1;
    try {
      const res = await fetchJson<PagedResponse>(path);
      return res?.items ?? [];
    } catch (err) {
      const apiErr = err as ApiError;
      const isRetryable = !apiErr?.status || apiErr.status === 429 || apiErr.status >= 500;
      if (!isRetryable || attempt >= maxAttempts) {
        console.warn(`useReleaseLookups: giving up on ${path} after ${attempt} attempt(s):`, err);
        return [];
      }
      const delayMs = apiErr.status === 429 && apiErr.retryAfter
        ? apiErr.retryAfter * 1000
        : 1000 * Math.pow(2, attempt - 1);
      await new Promise(r => setTimeout(r, delayMs));
    }
  }
  return [];
}

/**
 * Lookup groups keyed by the first wizard step that needs them.
 *
 * Step 0 – Basic Information  → artists
 * Step 1 – Classification     → formats, packagings, countries, genres
 * Step 2 – Label & Dates      → labels
 * Step 3 – Purchase Info      → stores
 */
const STEP_GROUPS: Record<number, Array<{ key: keyof ReleaseLookups; path: string }>> = {
  0: [{ key: "artists",    path: "/api/artists?pageSize=1000" }],
  1: [
    { key: "formats",    path: "/api/formats?pageSize=100" },
    { key: "packagings", path: "/api/packagings?pageSize=100" },
    { key: "countries",  path: "/api/countries?pageSize=300" },
    { key: "genres",     path: "/api/genres?pageSize=100" },
  ],
  2: [{ key: "labels",   path: "/api/labels?pageSize=1000" }],
  3: [{ key: "stores",   path: "/api/stores?pageSize=1000" }],
};

/**
 * Fetches all lookup lists required by the Add Release wizard, lazy-loading
 * each group only when the wizard reaches the step that needs it.
 *
 * @param currentStep - The currently active wizard step (0-based). Defaults to
 *   the last step so all lookup groups are fetched when no step is provided.
 */
export function useReleaseLookups(currentStep: number = 3): UseReleaseLookupsResult {
  const [lookups, setLookups] = useState<ReleaseLookups>(EMPTY);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  // Track which step groups have already been fetched so we never re-fetch.
  const fetchedGroups = React.useRef<Set<number>>(new Set());

  useEffect(() => {
    // Find the highest group whose trigger step has been reached and that
    // hasn't been fetched yet.
    const groupsToFetch = Object.entries(STEP_GROUPS)
      .filter(([step]) => Number(step) <= currentStep && !fetchedGroups.current.has(Number(step)))
      .map(([step, entries]) => ({ step: Number(step), entries }));

    if (groupsToFetch.length === 0) return;

    let cancelled = false;

    const fetchGroups = async () => {
      setLoading(true);

      await Promise.all(
        groupsToFetch.map(async ({ step, entries }) => {
          const results = await Promise.all(entries.map(e => fetchLookup(e.path)));
          if (cancelled) return;
          fetchedGroups.current.add(step);
          setLookups(prev => {
            const next = { ...prev };
            entries.forEach((e, i) => { next[e.key] = results[i]; });
            return next;
          });
        })
      );

      if (!cancelled) setLoading(false);
    };

    fetchGroups().catch(err => {
      if (!cancelled) {
        console.error("useReleaseLookups: unexpected error:", err);
        setError("Some lookup lists could not be loaded. You can still proceed but autocomplete options may be limited.");
        setLoading(false);
      }
    });

    return () => { cancelled = true; };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentStep]);

  return { ...lookups, loading, error };
}
