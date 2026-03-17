/**
 * Tests for useImageSearch hook.
 */
import { act, renderHook, waitFor } from "@testing-library/react";
import { useImageSearch } from "../useImageSearch";

// ── API mock ──────────────────────────────────────────────────────────────────

jest.mock("../../../lib/api", () => ({
  fetchJson: jest.fn(),
  API_BASE_URL: "http://localhost:5072",
}));

import { fetchJson } from "../../../lib/api";
const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

const MOCK_RESULT = {
  mbId: "abc-123",
  artist: "Iron Maiden",
  title: "Killers",
  year: 1981,
  format: "Vinyl",
  country: "GB",
  label: "EMI",
  imageUrl: "https://caa.example.com/img.jpg",
  thumbnailUrl: "https://caa.example.com/thumb.jpg",
  confidence: 1.0,
  confidenceLabel: "Exact match",
};

// ─── Tests ────────────────────────────────────────────────────────────────────

describe("useImageSearch", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("starts with empty state", () => {
    const { result } = renderHook(() => useImageSearch());
    expect(result.current.results).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it("sets isLoading while searching", async () => {
    let resolveSearch!: (v: unknown) => void;
    mockFetchJson.mockReturnValue(
      new Promise((r) => {
        resolveSearch = r;
      }) as ReturnType<typeof mockFetchJson>,
    );

    const { result } = renderHook(() => useImageSearch());

    act(() => {
      result.current.search("Iron Maiden");
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      resolveSearch([MOCK_RESULT]);
    });

    expect(result.current.isLoading).toBe(false);
  });

  it("returns results on successful search", async () => {
    mockFetchJson.mockResolvedValue([MOCK_RESULT] as ReturnType<
      typeof mockFetchJson
    >);

    const { result } = renderHook(() => useImageSearch());

    await act(async () => {
      await result.current.search("Iron Maiden Killers");
    });

    expect(result.current.results).toHaveLength(1);
    expect(result.current.results[0].title).toBe("Killers");
    expect(result.current.error).toBeNull();
  });

  it("sets error on failed search", async () => {
    mockFetchJson.mockRejectedValue(new Error("Network error"));

    const { result } = renderHook(() => useImageSearch());

    await act(async () => {
      await result.current.search("Unknown Album");
    });

    expect(result.current.results).toEqual([]);
    expect(result.current.error).not.toBeNull();
  });

  it("clears results for empty query without calling API", async () => {
    const { result } = renderHook(() => useImageSearch());

    await act(async () => {
      await result.current.search("   ");
    });

    expect(mockFetchJson).not.toHaveBeenCalled();
    expect(result.current.results).toEqual([]);
  });

  it("clear() resets all state", async () => {
    mockFetchJson.mockResolvedValue([MOCK_RESULT] as ReturnType<
      typeof mockFetchJson
    >);

    const { result } = renderHook(() => useImageSearch());
    await act(async () => {
      await result.current.search("Iron Maiden");
    });

    act(() => {
      result.current.clear();
    });

    expect(result.current.results).toEqual([]);
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
  });
});
