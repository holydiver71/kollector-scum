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
  details?: any;
  url?: string;
}

interface FetchJsonOptions extends RequestInit {
  timeoutMs?: number;
  parse?: boolean; // allow head / no body
}

export async function fetchJson<T = any>(path: string, options: FetchJsonOptions = {}): Promise<T> {
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
      let body: any = null;
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
    } catch (e) {
      const err: ApiError = new Error(`Failed to parse JSON for ${url}`);
      err.url = url;
      throw err;
    }
  } catch (e: any) {
    clearTimeout(id);
    if (e?.name === 'AbortError') {
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

export async function getHealth() {
  return fetchJson('/api/health');
}

export async function getPagedCount(endpoint: string): Promise<number> {
  const data = await fetchJson(`${endpoint}?pageSize=1`);
  return data?.totalCount || 0;
}
