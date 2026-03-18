/**
 * Tests for ImageSearchModal component.
 */
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ImageSearchModal from "../ImageSearchModal";

// ── Mock useImageSearch ───────────────────────────────────────────────────────

const mockSearch = jest.fn();
const mockClear = jest.fn();
const mockSearchState = {
  results: [] as ReturnType<typeof import("../useImageSearch")["useImageSearch"]>["results"],
  isLoading: false,
  error: null as string | null,
  search: mockSearch,
  clear: mockClear,
};

jest.mock("../useImageSearch", () => ({
  useImageSearch: () => mockSearchState,
}));

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

describe("ImageSearchModal", () => {
  const defaultProps = {
    defaultQuery: "Iron Maiden Killers 1981",
    onSelect: jest.fn(),
    onClose: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockSearchState.results = [];
    mockSearchState.isLoading = false;
    mockSearchState.error = null;
  });

  it("renders with default query pre-populated", () => {
    render(<ImageSearchModal {...defaultProps} />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveValue("Iron Maiden Killers 1981");
  });

  it("triggers initial search on mount", () => {
    render(<ImageSearchModal {...defaultProps} />);
    expect(mockSearch).toHaveBeenCalledWith("Iron Maiden Killers 1981", undefined);
  });

  it("calls onClose when close button is clicked", async () => {
    render(<ImageSearchModal {...defaultProps} />);
    const closeBtn = screen.getByRole("button", { name: /close/i });
    await userEvent.click(closeBtn);
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("calls onClose when Escape key is pressed", () => {
    render(<ImageSearchModal {...defaultProps} />);
    fireEvent.keyDown(window, { key: "Escape" });
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("shows loading spinner when isLoading is true", () => {
    mockSearchState.isLoading = true;
    render(<ImageSearchModal {...defaultProps} />);
    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("shows error message when error is set", () => {
    mockSearchState.error = "Could not fetch cover art.";
    render(<ImageSearchModal {...defaultProps} />);
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });

  it("shows empty state when no results", () => {
    mockSearchState.results = [];
    render(<ImageSearchModal {...defaultProps} />);
    expect(screen.getByText(/no cover art found/i)).toBeInTheDocument();
  });

  it("renders result cards when results are available", () => {
    mockSearchState.results = [MOCK_RESULT];
    render(<ImageSearchModal {...defaultProps} />);
    expect(screen.getByText("Killers")).toBeInTheDocument();
    expect(screen.getByText("Iron Maiden")).toBeInTheDocument();
    expect(screen.getByText("Exact match")).toBeInTheDocument();
  });

  it("calls onSelect with image and thumbnail URLs when a result is selected", async () => {
    mockSearchState.results = [MOCK_RESULT];
    render(<ImageSearchModal {...defaultProps} />);

    const card = screen.getByRole("button", {
      name: /select iron maiden – killers/i,
    });
    await userEvent.click(card);

    expect(defaultProps.onSelect).toHaveBeenCalledWith(
      MOCK_RESULT.imageUrl,
      MOCK_RESULT.thumbnailUrl,
    );
  });

  it("calls search when form is submitted", async () => {
    render(<ImageSearchModal {...defaultProps} />);
    const submitBtn = screen.getByRole("button", { name: /search/i });
    await userEvent.click(submitBtn);
    expect(mockSearch).toHaveBeenCalled();
  });
});
