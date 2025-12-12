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
  /**
   * When true, do not throw on non-OK responses; instead return `null` so callers
   * can handle transient server errors gracefully. Default: `false`.
   */
  swallowErrors?: boolean;
}

/**
 * Gets the stored authentication token
 */
function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null;
  // Import from auth.ts to avoid duplication
  return localStorage.getItem('auth_token');
}

export async function fetchJson<T = unknown>(path: string, options: FetchJsonOptions = {}): Promise<T> {
  const { timeoutMs = DEFAULT_TIMEOUT_MS, parse = true, ...init } = options;
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeoutMs);

  // Support absolute URLs passed directly
  const url = path.startsWith("http://") || path.startsWith("https://")
    ? path
    : `${API_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`;

  // Add authorization header if token is available
  const token = getAuthToken();
  const headers: HeadersInit = {
    ...init.headers,
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    const res = await fetch(url, { ...init, headers, signal: controller.signal });
    clearTimeout(id);
    if (!res.ok) {
      let body: unknown = null;
      const contentType = res.headers.get("content-type");
      try {
        if (contentType?.includes("application/json")) {
          body = await res.json();
        } else {
          body = await res.text();
        }
      } catch { /* ignore */ }
      // If caller asked to swallow errors (useful for non-critical widgets),
      // return null instead of throwing so the UI can render fallback content.
      if (options.swallowErrors) {
        return null as unknown as T;
      }

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

/**
 * Updates an existing music release in the collection
 * @param id - The ID of the release to update
 * @param data - The updated release data (UpdateMusicReleaseDto)
 * @throws {ApiError} If the request fails (404 if not found, 400 for validation errors, 500 on server error)
 * @returns Promise that resolves with the updated release data
 */
export async function updateRelease(id: number, data: unknown): Promise<unknown> {
  return fetchJson(`/api/musicreleases/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

// Discogs Integration
import type {
  DiscogsSearchRequest,
  DiscogsSearchResult,
  DiscogsRelease,
} from './discogs-types';

/**
 * Search Discogs by catalog number with optional filters
 * @param request - Search parameters including catalog number and optional filters
 * @returns Array of matching releases from Discogs
 */
export async function searchDiscogs(request: DiscogsSearchRequest): Promise<DiscogsSearchResult[]> {
  const params = new URLSearchParams();
  params.append('catalogNumber', request.catalogNumber);
  
  if (request.format) params.append('format', request.format);
  if (request.country) params.append('country', request.country);
  if (request.year) params.append('year', request.year.toString());
  
  return fetchJson<DiscogsSearchResult[]>(`/api/discogs/search?${params.toString()}`);
}

/**
 * Get full release details from Discogs
 * @param releaseId - The Discogs release ID
 * @returns Full release details from Discogs
 */
export async function getDiscogsRelease(releaseId: number): Promise<DiscogsRelease> {
  return fetchJson<DiscogsRelease>(`/api/discogs/release/${releaseId}`);
}

// Now Playing
export interface NowPlayingDto {
  id: number;
  musicReleaseId: number;
  playedAt: string;
}

/**
 * Records a now playing entry for a music release
 * @param musicReleaseId - The ID of the music release being played
 * @returns The created now playing record
 */
export async function createNowPlaying(musicReleaseId: number): Promise<NowPlayingDto> {
  return fetchJson<NowPlayingDto>('/api/nowplaying', {
    method: 'POST',
    body: JSON.stringify({ musicReleaseId }),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

// Play History
export interface PlayHistoryItemDto {
  id: number;
  playedAt: string;
}

export interface PlayHistoryDto {
  musicReleaseId: number;
  playCount: number;
  playDates: PlayHistoryItemDto[];
}

/**
 * Gets the play history for a music release
 * @param musicReleaseId - The ID of the music release
 * @returns The play count and list of all play dates
 */
export async function getPlayHistory(musicReleaseId: number): Promise<PlayHistoryDto> {
  return fetchJson<PlayHistoryDto>(`/api/NowPlaying/release/${musicReleaseId}/history`);
}

/**
 * Deletes a now playing record
 * @param id - The ID of the now playing record
 */
export async function deleteNowPlaying(id: number): Promise<void> {
  return fetchJson<void>(`/api/NowPlaying/${id}`, {
    method: 'DELETE',
    parse: false
  });
}

// Recently Played
export interface RecentlyPlayedItemDto {
  id: number;
  coverFront?: string;
  playedAt: string;
  playCount: number;
}

/**
 * Gets recently played releases with their cover images
 * @param limit - Maximum number of releases to return (default 24)
 * @returns List of recently played releases
 */
export async function getRecentlyPlayed(limit: number = 24): Promise<RecentlyPlayedItemDto[]> {
  // If the recently-played endpoint is temporarily failing (500), treat it as
  // non-fatal for the UI and return an empty list so the widget can show a
  // friendly message rather than crash the app overlay.
  const result = await fetchJson<RecentlyPlayedItemDto[]>(`/api/NowPlaying/recent?limit=${limit}`, { swallowErrors: true });
  return result || [];
}

// Random Release
export interface RandomReleaseResponse {
  id: number;
}

/**
 * Gets the ID of a random release from the collection
 * @returns The ID of a random release
 */
export async function getRandomReleaseId(): Promise<number> {
  const response = await fetchJson<RandomReleaseResponse>('/api/musicreleases/random');
  return response.id;
}

// Kollections
export interface KollectionDto {
  id: number;
  name: string;
  genreIds: number[];
  genreNames: string[];
}

export interface CreateKollectionDto {
  name: string;
  genreIds: number[];
}

export interface UpdateKollectionDto {
  name: string;
  genreIds: number[];
}

export interface PagedKollectionsResponse {
  items: KollectionDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * Gets all kollections with optional search
 * @param search - Optional search term to filter by name
 * @param page - Page number (default 1)
 * @param pageSize - Page size (default 50)
 * @returns Paged list of kollections
 */
export async function getKollections(search?: string, page: number = 1, pageSize: number = 50): Promise<PagedKollectionsResponse> {
  const params = new URLSearchParams();
  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());
  if (search) {
    params.append('search', search);
  }
  return fetchJson<PagedKollectionsResponse>(`/api/kollections?${params.toString()}`);
}

/**
 * Gets a specific kollection by ID
 * @param id - The kollection ID
 * @returns The kollection details
 */
export async function getKollection(id: number): Promise<KollectionDto> {
  return fetchJson<KollectionDto>(`/api/kollections/${id}`);
}

/**
 * Creates a new kollection
 * @param data - The kollection data to create
 * @returns The created kollection
 */
export async function createKollection(data: CreateKollectionDto): Promise<KollectionDto> {
  return fetchJson<KollectionDto>('/api/kollections', {
    method: 'POST',
    body: JSON.stringify(data),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

/**
 * Updates an existing kollection
 * @param id - The kollection ID
 * @param data - The updated kollection data
 * @returns The updated kollection
 */
export async function updateKollection(id: number, data: UpdateKollectionDto): Promise<KollectionDto> {
  return fetchJson<KollectionDto>(`/api/kollections/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

/**
 * Deletes a kollection
 * @param id - The kollection ID
 */
export async function deleteKollection(id: number): Promise<void> {
  return fetchJson<void>(`/api/kollections/${id}`, {
    method: 'DELETE',
    parse: false,
  });
}

// Lists
export interface ListSummaryDto {
  id: number;
  name: string;
  releaseCount: number;
  createdAt: string;
  lastModified: string;
}

export interface ListDto {
  id: number;
  name: string;
  createdAt: string;
  lastModified: string;
  releaseIds: number[];
}

export interface CreateListDto {
  name: string;
}

export interface UpdateListDto {
  name: string;
}

/**
 * Gets all lists
 * @returns List of list summaries
 */
export async function getLists(): Promise<ListSummaryDto[]> {
  return fetchJson<ListSummaryDto[]>('/api/lists');
}

/**
 * Gets a specific list by ID
 * @param id - The list ID
 * @returns List details
 */
export async function getList(id: number): Promise<ListDto> {
  return fetchJson<ListDto>(`/api/lists/${id}`);
}

/**
 * Gets release IDs in a specific list
 * @param id - The list ID
 * @returns List of release IDs
 */
export async function getListReleases(id: number): Promise<number[]> {
  return fetchJson<number[]>(`/api/lists/${id}/releases`);
}

/**
 * Creates a new list
 * @param data - The list creation data
 * @returns The created list
 */
export async function createList(data: CreateListDto): Promise<ListDto> {
  return fetchJson<ListDto>('/api/lists', {
    method: 'POST',
    body: JSON.stringify(data),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

/**
 * Updates an existing list
 * @param id - The list ID
 * @param data - The updated list data
 * @returns The updated list
 */
export async function updateList(id: number, data: UpdateListDto): Promise<ListDto> {
  return fetchJson<ListDto>(`/api/lists/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
    headers: {
      'Content-Type': 'application/json',
    },
  });
}

/**
 * Deletes a list
 * @param id - The list ID
 */
export async function deleteList(id: number): Promise<void> {
  return fetchJson<void>(`/api/lists/${id}`, {
    method: 'DELETE',
    parse: false,
  });
}

/**
 * Adds a release to a list
 * @param listId - The list ID
 * @param releaseId - The release ID
 */
export async function addReleaseToList(listId: number, releaseId: number): Promise<void> {
  return fetchJson<void>(`/api/lists/${listId}/releases`, {
    method: 'POST',
    body: JSON.stringify({ releaseId }),
    headers: {
      'Content-Type': 'application/json',
    },
    parse: false,
  });
}

/**
 * Removes a release from a list
 * @param listId - The list ID
 * @param releaseId - The release ID
 */
export async function removeReleaseFromList(listId: number, releaseId: number): Promise<void> {
  return fetchJson<void>(`/api/lists/${listId}/releases/${releaseId}`, {
    method: 'DELETE',
    parse: false,
  });
}

/**
 * Gets all lists that contain a specific release
 * @param releaseId - The release ID
 * @returns List of list summaries
 */
export async function getListsForRelease(releaseId: number): Promise<ListSummaryDto[]> {
  return fetchJson<ListSummaryDto[]>(`/api/lists/by-release/${releaseId}`);
}
