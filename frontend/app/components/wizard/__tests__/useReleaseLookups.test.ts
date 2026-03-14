/**
 * Unit tests for the useReleaseLookups hook.
 * Verifies that lookup lists are fetched in parallel and the loading/error
 * state is managed correctly.
 */
import { renderHook, waitFor } from "@testing-library/react";
import { useReleaseLookups } from "../useReleaseLookups";
import { fetchJson } from "../../../lib/api";

jest.mock("../../../lib/api", () => ({
  fetchJson: jest.fn(),
}));

const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

const ITEMS = { items: [{ id: 1, name: "Test Item" }] };

beforeEach(() => {
  jest.clearAllMocks();
  mockFetchJson.mockResolvedValue(ITEMS);
});

describe("useReleaseLookups", () => {
  it("starts in a loading state", () => {
    const { result } = renderHook(() => useReleaseLookups());
    expect(result.current.loading).toBe(true);
  });

  it("resolves loading to false after all fetches complete", async () => {
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));
  });

  it("populates all lookup lists from successful responses", async () => {
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.artists).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.labels).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.genres).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.countries).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.formats).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.packagings).toEqual([{ id: 1, name: "Test Item" }]);
    expect(result.current.stores).toEqual([{ id: 1, name: "Test Item" }]);
  });

  it("sets error to null on successful load", async () => {
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBeNull();
  });

  it("fetches exactly 7 endpoints in parallel", async () => {
    renderHook(() => useReleaseLookups());
    await waitFor(() =>
      expect(mockFetchJson).toHaveBeenCalledTimes(7)
    );
  });

  it("fetches the correct API endpoints", async () => {
    renderHook(() => useReleaseLookups());
    await waitFor(() =>
      expect(mockFetchJson).toHaveBeenCalledTimes(7)
    );
    const calledUrls = mockFetchJson.mock.calls.map((c) => c[0]);
    expect(calledUrls).toContain("/api/artists?pageSize=1000");
    expect(calledUrls).toContain("/api/labels?pageSize=1000");
    expect(calledUrls).toContain("/api/genres?pageSize=100");
    expect(calledUrls).toContain("/api/countries?pageSize=300");
    expect(calledUrls).toContain("/api/formats?pageSize=100");
    expect(calledUrls).toContain("/api/packagings?pageSize=100");
    expect(calledUrls).toContain("/api/stores?pageSize=1000");
  });

  it("sets an error string when a fetch rejects", async () => {
    mockFetchJson.mockRejectedValue(new Error("Network error"));
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBeTruthy();
  });

  it("leaves lookup lists empty when a fetch rejects", async () => {
    mockFetchJson.mockRejectedValue(new Error("Network error"));
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.artists).toEqual([]);
    expect(result.current.labels).toEqual([]);
    expect(result.current.genres).toEqual([]);
  });

  it("handles an items-less response gracefully", async () => {
    // Some endpoints may return an unexpected shape; null-coalescence should protect us
    mockFetchJson.mockResolvedValue({} as never);
    const { result } = renderHook(() => useReleaseLookups());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.artists).toEqual([]);
    expect(result.current.error).toBeNull();
  });
});
