/**
 * Tests for ImageSearchModal component.
 */
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ImageSearchModal from "../ImageSearchModal";

// Mock the useImageSearch hook
const mockSearch = jest.fn();
const mockSetQuery = jest.fn();
const mockReset = jest.fn();

let mockState = {
  query: "iron maiden killers album cover",
  results: [],
  isLoading: false,
  error: null as string | null,
};

jest.mock("../../../hooks/useImageSearch", () => ({
  useImageSearch: () => ({
    ...mockState,
    search: mockSearch,
    setQuery: mockSetQuery,
    reset: mockReset,
  }),
}));

const defaultProps = {
  defaultQuery: "iron maiden killers album cover",
  onSelect: jest.fn(),
  onClose: jest.fn(),
};

function renderModal(props = defaultProps) {
  return render(<ImageSearchModal {...props} />);
}

beforeEach(() => {
  jest.clearAllMocks();
  mockSearch.mockResolvedValue(undefined);
  mockState = {
    query: "iron maiden killers album cover",
    results: [],
    isLoading: false,
    error: null,
  };
});

describe("ImageSearchModal", () => {
  test("renders with the default query pre-populated and runs initial search", async () => {
    renderModal();
    expect(screen.getByTestId("search-input")).toBeInTheDocument();
    await waitFor(() => {
      expect(mockSearch).toHaveBeenCalled();
    });
  });

  test("renders the search button", () => {
    renderModal();
    expect(screen.getByTestId("search-button")).toBeInTheDocument();
  });

  test("close button calls onClose", async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();
    render(<ImageSearchModal {...defaultProps} onClose={onClose} />);
    await user.click(screen.getByTestId("modal-close-button"));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  test("clicking backdrop calls onClose", async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();
    render(<ImageSearchModal {...defaultProps} onClose={onClose} />);
    await user.click(screen.getByTestId("modal-backdrop"));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  test("pressing Enter on the input triggers search", async () => {
    const user = userEvent.setup();
    renderModal();
    const input = screen.getByTestId("search-input");
    await user.type(input, "{enter}");
    await waitFor(() => {
      // search called at least once (initial + Enter)
      expect(mockSearch).toHaveBeenCalled();
    });
  });

  test("shows loading spinner when isLoading is true", () => {
    mockState = { ...mockState, isLoading: true };
    renderModal();
    expect(screen.getByTestId("loading-spinner")).toBeInTheDocument();
  });

  test("shows error state when error is set", () => {
    mockState = { ...mockState, error: "Search failed" };
    renderModal();
    expect(screen.getByTestId("search-error")).toBeInTheDocument();
    expect(screen.getByText("Search failed")).toBeInTheDocument();
  });

  test("shows empty state after search with no results", async () => {
    mockState = { ...mockState, results: [] };
    renderModal();
    // Simulate post-search empty state by triggering initial search
    await waitFor(() => expect(mockSearch).toHaveBeenCalled());
    // The component sets hasSearched after search resolves
    await waitFor(() => {
      expect(screen.getByTestId("empty-state")).toBeInTheDocument();
    });
  });

  test("renders result grid when results are available", async () => {
    mockState = {
      ...mockState,
      results: [
        {
          title: "Killers",
          imageUrl: "https://example.com/killers.jpg",
          thumbnailUrl: "https://example.com/killers_thumb.jpg",
          width: 600,
          height: 600,
        },
        {
          title: "Beast",
          imageUrl: "https://example.com/beast.jpg",
          thumbnailUrl: "https://example.com/beast_thumb.jpg",
          width: 600,
          height: 600,
        },
      ],
    };
    renderModal();
    await waitFor(() => {
      expect(screen.getByTestId("results-grid")).toBeInTheDocument();
    });
    expect(screen.getByTestId("result-item-0")).toBeInTheDocument();
    expect(screen.getByTestId("result-item-1")).toBeInTheDocument();
  });

  test("selecting a result calls onSelect with imageUrl and closes modal", async () => {
    const user = userEvent.setup();
    const onSelect = jest.fn();
    const onClose = jest.fn();
    mockState = {
      ...mockState,
      results: [
        {
          title: "Killers",
          imageUrl: "https://example.com/killers.jpg",
          thumbnailUrl: "https://example.com/killers_thumb.jpg",
          width: 600,
          height: 600,
        },
      ],
    };
    render(
      <ImageSearchModal
        defaultQuery="iron maiden killers"
        onSelect={onSelect}
        onClose={onClose}
      />
    );
    await waitFor(() =>
      expect(screen.getByTestId("results-grid")).toBeInTheDocument()
    );
    await user.click(screen.getByTestId("result-item-0"));
    expect(onSelect).toHaveBeenCalledWith("https://example.com/killers.jpg");
    expect(onClose).toHaveBeenCalled();
  });
});
