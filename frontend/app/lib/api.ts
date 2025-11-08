// Centralized API helper for frontend
// Provides: configurable base URL, timeout handling, unified error formatting

const DEFAULT_TIMEOUT_MS = 8000;

// Derive base URL priority:
// 1. Explicit env var NEXT_PUBLIC_API_BASE_URL (e.g. http://localhost:5072)
// 2. If window available and same-origin desired, allow relative ('')
// 3. Fallback hard-coded dev port
export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ||
  (typeof window !== "undefined" ? "" : "http://localhost:5072");

export interface ApiError extends Error {
  status?: number;
  details?: unknown;
  url?: string;
}

interface FetchJsonOptions extends RequestInit {
  timeoutMs?: number;
  parse?: boolean; // allow head / no body
}

export async function fetchJson<T = unknown>(path: string, options: FetchJsonOptions = {}): Promise<T> {
  const { timeoutMs = DEFAULT_TIMEOUT_MS, parse = true, ...init } = options;
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeoutMs);

  // Support absolute URLs passed directly
  const url = path.startsWith("http://") || path.startsWith("https://")
    ? path
    : `${API_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`;

  try {
    const res = await fetch(url, { ...init, signal: controller.signal });
    clearTimeout(id);
    if (!res.ok) {
      let body: unknown = null;
      try { body = await res.json(); } catch { /* ignore */ }
      const err: ApiError = new Error(`Request failed (${res.status}) ${res.statusText}`);
      err.status = res.status;
      err.details = body;
      err.url = url;
      throw err;
    }
    if (!parse) return undefined as unknown as T;
    try {
      return await res.json();
    } catch {
      const err: ApiError = new Error(`Failed to parse JSON for ${url}`);
      err.url = url;
      throw err;
    }
  } catch (e: unknown) {
    clearTimeout(id);
    if (e && typeof e === 'object' && 'name' in e && e.name === 'AbortError') {
      const err: ApiError = new Error(`Request timeout after ${timeoutMs}ms: ${url}`);
      err.url = url;
      throw err;
    }
    if (e instanceof Error) throw e;
    const err: ApiError = new Error(`Unknown fetch error for ${url}`);
    err.url = url;
    throw err;
  }
}

interface HealthResponse {
  status: string;
  timestamp: string;
  service: string;
  version: string;
}

export async function getHealth(): Promise<HealthResponse> {
  return fetchJson<HealthResponse>('/api/health');
}

interface PagedResponse {
  totalCount?: number;
}

export async function getPagedCount(endpoint: string): Promise<number> {
  const data = await fetchJson<PagedResponse>(`${endpoint}?pageSize=1`);
  return data?.totalCount || 0;
}

// Search Suggestions
export interface SearchSuggestion {
  type: string; // 'release', 'artist', 'label'
  id: number;
  name: string;
  subtitle?: string;
}

export async function getSearchSuggestions(query: string, limit: number = 10): Promise<SearchSuggestion[]> {
  if (!query || query.length < 2) return [];
  return fetchJson<SearchSuggestion[]>(`/api/musicreleases/suggestions?query=${encodeURIComponent(query)}&limit=${limit}`);
}

// Collection Statistics
export interface YearStatistic {
  year: number;
  count: number;
}

export interface GenreStatistic {
  genreId: number;
  genreName: string;
  count: number;
  percentage: number;
}

export interface FormatStatistic {
  formatId: number;
  formatName: string;
  count: number;
  percentage: number;
}

export interface CountryStatistic {
  countryId: number;
  countryName: string;
  count: number;
  percentage: number;
}

export interface MusicReleaseSummary {
  id: number;
  title: string;
  releaseYear: string;
  artistNames?: string[];
  genreNames?: string[];
  labelName?: string;
  countryName?: string;
  formatName?: string;
  coverImageUrl?: string;
  dateAdded: string;
}

export interface CollectionStatistics {
  totalReleases: number;
  totalArtists: number;
  totalGenres: number;
  totalLabels: number;
  releasesByYear: YearStatistic[];
  releasesByGenre: GenreStatistic[];
  releasesByFormat: FormatStatistic[];
  releasesByCountry: CountryStatistic[];
  totalValue?: number;
  averagePrice?: number;
  mostExpensiveRelease?: MusicReleaseSummary;
  recentlyAdded: MusicReleaseSummary[];
}

export async function getCollectionStatistics(): Promise<CollectionStatistics> {
  return fetchJson<CollectionStatistics>('/api/musicreleases/statistics');
}

/**
 * Deletes a music release from the collection
 * @param id - The ID of the release to delete
 * @throws {ApiError} If the request fails (404 if not found, 500 on server error)
 * @returns Promise that resolves when deletion is complete
 */
export async function deleteRelease(id: number): Promise<void> {
  await fetchJson<void>(`/api/musicreleases/${id}`, {
    method: 'DELETE',
    parse: false, // DELETE returns 204 No Content (no response body)
  });
}
