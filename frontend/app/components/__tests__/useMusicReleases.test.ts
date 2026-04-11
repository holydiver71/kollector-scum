import { renderHook, waitFor } from "@testing-library/react";
import { useMusicReleases, type MusicRelease, type PagedResult } from "../useMusicReleases";

// ── Mocks ─────────────────────────────────────────────────────────────────────

jest.mock("../../lib/api", () => ({
  fetchJson: jest.fn(),
  API_BASE_URL: "http://localhost:5072",
}));

jest.mock("../../lib/auth", () => ({
  clearAuthToken: jest.fn(),
}));

// Bypass retry delays in tests — just call the function directly.
jest.mock("../../lib/withRetry", () => ({
  withRetry: (fn: () => Promise<unknown>) => fn(),
  defaultIsRetryable: jest.fn(),
  httpOnlyIsRetryable: jest.fn(),
}));

import { fetchJson } from "../../lib/api";
import { clearAuthToken } from "../../lib/auth";

const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;
const mockClearAuthToken = clearAuthToken as jest.MockedFunction<typeof clearAuthToken>;

// ── Fixtures ──────────────────────────────────────────────────────────────────

function makeRelease(id: number): MusicRelease {
  return {
    id,
    title: `Release ${id}`,
    releaseYear: "2000-01-01",
    artistNames: ["Artist"],
    dateAdded: "2024-01-01",
  };
}

function makePagedResult(
  items: MusicRelease[],
  totalCount: number,
  totalPages: number,
  page = 1
): PagedResult<MusicRelease> {
  return { items, page, pageSize: 60, totalCount, totalPages };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe("useMusicReleases", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // jsdom's window.location is not fully writable; replace just href via
    // delete + defineProperty so redirect tests can observe the assignment.
    delete (window as { location?: Location }).location;
    (window as unknown as { location: { href: string } }).location = { href: "/" };
    window.scrollTo = jest.fn();
  });

  it("starts in loading state", () => {
    mockFetchJson.mockReturnValue(new Promise(() => {}));
    const { result } = renderHook(() => useMusicReleases({}, 60));
    expect(result.current.loading).toBe(true);
    expect(result.current.releases).toEqual([]);
    expect(result.current.error).toBeNull();
  });

  it("populates state from a paged API response", async () => {
    const releases = [makeRelease(1), makeRelease(2)];
    mockFetchJson.mockResolvedValue(makePagedResult(releases, 2, 1));

    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.releases).toHaveLength(2);
    expect(result.current.totalCount).toBe(2);
    expect(result.current.totalPages).toBe(1);
    expect(result.current.currentPage).toBe(1);
    expect(result.current.error).toBeNull();
  });

  it("handles a flat-array API response (legacy shape)", async () => {
    const releases = [makeRelease(10), makeRelease(11)];
    mockFetchJson.mockResolvedValue(releases);

    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.releases).toHaveLength(2);
    expect(result.current.totalCount).toBe(2);
    expect(result.current.totalPages).toBe(1);
    expect(result.current.currentPage).toBe(1);
  });

  it("sets error state on fetch failure", async () => {
    const err = Object.assign(new Error("Server error"), { status: 500 });
    mockFetchJson.mockRejectedValue(err);

    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.error).toMatch(/Server error/);
    expect(result.current.releases).toEqual([]);
  });

  it("redirects on 401 and clears auth token", async () => {
    const err = Object.assign(new Error("Unauthorized"), { status: 401 });
    mockFetchJson.mockRejectedValue(err);

    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(mockClearAuthToken).toHaveBeenCalled());
    // jsdom resolves relative paths: "/" becomes "http://localhost/"
    expect(window.location.href).toMatch(/\/$/);
    expect(result.current.releases).toEqual([]);
  });

  it("includes filter params in the API URL", async () => {
    mockFetchJson.mockResolvedValue(makePagedResult([], 0, 0));

    renderHook(() =>
      useMusicReleases({ search: "metal", artistId: 5, genreId: 3 }, 20)
    );
    await waitFor(() => expect(mockFetchJson).toHaveBeenCalled());

    const url = mockFetchJson.mock.calls[0][0] as string;
    expect(url).toContain("Search=metal");
    expect(url).toContain("ArtistId=5");
    expect(url).toContain("GenreId=3");
    expect(url).toContain("Pagination.PageSize=20");
  });

  it("applies default sort (title asc) when none provided", async () => {
    mockFetchJson.mockResolvedValue(makePagedResult([], 0, 0));

    renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(mockFetchJson).toHaveBeenCalled());

    const url = mockFetchJson.mock.calls[0][0] as string;
    expect(url).toContain("SortBy=title");
    expect(url).toContain("SortOrder=asc");
  });

  it("handlePageChange fetches the requested page", async () => {
    const page1 = makePagedResult([makeRelease(1)], 10, 2, 1);
    const page2 = makePagedResult([makeRelease(2)], 10, 2, 2);
    mockFetchJson.mockResolvedValueOnce(page1).mockResolvedValueOnce(page2);

    const { result } = renderHook(() => useMusicReleases({}, 5));
    await waitFor(() => expect(result.current.loading).toBe(false));

    result.current.handlePageChange(2);
    await waitFor(() => expect(result.current.currentPage).toBe(2));
    expect(result.current.releases[0].id).toBe(2);
  });

  it("handlePageChange is a noop for out-of-range pages", async () => {
    mockFetchJson.mockResolvedValue(makePagedResult([], 0, 1));
    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(result.current.loading).toBe(false));

    const callsBefore = mockFetchJson.mock.calls.length;
    result.current.handlePageChange(99);
    expect(mockFetchJson.mock.calls.length).toBe(callsBefore);
  });

  it("refetch re-runs the last fetch", async () => {
    mockFetchJson.mockResolvedValue(makePagedResult([makeRelease(1)], 1, 1));
    const { result } = renderHook(() => useMusicReleases({}, 60));
    await waitFor(() => expect(result.current.loading).toBe(false));

    const callsBefore = mockFetchJson.mock.calls.length;
    result.current.refetch();
    await waitFor(() => expect(mockFetchJson.mock.calls.length).toBeGreaterThan(callsBefore));
  });
});
