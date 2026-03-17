/**
 * Tests for the reworked ImagesPanel (Step 5) component.
 */
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ImagesPanel from "../panels/ImagesPanel";
import type { WizardFormData, ValidationErrors } from "../types";
import { EMPTY_FORM_DATA } from "../types";

// ── Mock ImageSearchModal ─────────────────────────────────────────────────────

jest.mock("../ImageSearchModal", () => ({
  __esModule: true,
  default: function MockModal({
    onSelect,
    onClose,
    defaultQuery,
  }: {
    onSelect: (img: string, thumb: string) => void;
    onClose: () => void;
    defaultQuery: string;
  }) {
    return (
      <div data-testid="search-modal">
        <span data-testid="modal-query">{defaultQuery}</span>
        <button
          onClick={() => onSelect("https://caa.example.com/img.jpg", "https://caa.example.com/thumb.jpg")}
        >
          Select Image
        </button>
        <button onClick={onClose}>Close Modal</button>
      </div>
    );
  },
}));

// ── Mock fetch for upload ─────────────────────────────────────────────────────

global.fetch = jest.fn();
const mockFetch = global.fetch as jest.Mock;

// ── Mock fetchJson for download ───────────────────────────────────────────────

jest.mock("../../../lib/api", () => ({
  fetchJson: jest.fn(),
  API_BASE_URL: "http://localhost:5072",
}));
import { fetchJson } from "../../../lib/api";
const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

// ─── Helpers ──────────────────────────────────────────────────────────────────

function buildData(overrides: Partial<WizardFormData> = {}): WizardFormData {
  return {
    ...EMPTY_FORM_DATA,
    title: "Killers",
    artistDisplayNames: ["Iron Maiden"],
    releaseYear: "1981",
    ...overrides,
  };
}

function renderPanel(
  data: WizardFormData = buildData(),
  errors: ValidationErrors = {},
  onChange = jest.fn(),
) {
  return render(<ImagesPanel data={data} onChange={onChange} errors={errors} />);
}

// ─── Tests ────────────────────────────────────────────────────────────────────

describe("ImagesPanel", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders Search Web and Upload File buttons for Cover Front", () => {
    renderPanel();
    expect(screen.getByRole("button", { name: /search web/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /upload file/i })).toBeInTheDocument();
  });

  it("shows Back Cover and Thumbnail text inputs", () => {
    renderPanel();
    expect(screen.getByLabelText(/back cover/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/thumbnail/i)).toBeInTheDocument();
  });

  it("opens ImageSearchModal when 'Search Web' is clicked", async () => {
    renderPanel();
    await userEvent.click(screen.getByRole("button", { name: /search web/i }));
    expect(screen.getByTestId("search-modal")).toBeInTheDocument();
  });

  it("pre-fills modal query with artist, title and year", async () => {
    renderPanel(buildData({ title: "Killers", artistDisplayNames: ["Iron Maiden"], releaseYear: "1981" }));
    await userEvent.click(screen.getByRole("button", { name: /search web/i }));
    expect(screen.getByTestId("modal-query")).toHaveTextContent("Iron Maiden Killers 1981");
  });

  it("closes ImageSearchModal when Close is clicked", async () => {
    renderPanel();
    await userEvent.click(screen.getByRole("button", { name: /search web/i }));
    await userEvent.click(screen.getByRole("button", { name: /close modal/i }));
    expect(screen.queryByTestId("search-modal")).not.toBeInTheDocument();
  });

  it("calls onChange with coverFront and thumbnail after image is selected from modal", async () => {
    mockFetchJson.mockResolvedValue({ filename: "stored-cover.jpg", thumbnailFilename: "stored-thumb.jpg" } as ReturnType<typeof mockFetchJson>);

    const onChange = jest.fn();
    renderPanel(buildData(), {}, onChange);

    await userEvent.click(screen.getByRole("button", { name: /search web/i }));
    await userEvent.click(screen.getByRole("button", { name: /select image/i }));

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(
        expect.objectContaining({
          images: expect.objectContaining({
            coverFront: "stored-cover.jpg",
            thumbnail: "stored-thumb.jpg",
          }),
        }),
      );
    });
  });

  it("shows error when upload file is too large", async () => {
    renderPanel();

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;

    // Use a plain object as the File to control the size property in jsdom
    const bigFile = { name: "cover.jpg", type: "image/jpeg", size: 6 * 1024 * 1024 };
    Object.defineProperty(input, "files", {
      value: { 0: bigFile, length: 1, item: () => bigFile },
      configurable: true,
    });

    fireEvent.change(input);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
  });

  it("shows error when upload file has invalid type", async () => {
    renderPanel();

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;

    const pdfFile = { name: "document.pdf", type: "application/pdf", size: 1024 };
    Object.defineProperty(input, "files", {
      value: { 0: pdfFile, length: 1, item: () => pdfFile },
      configurable: true,
    });

    fireEvent.change(input);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
  });

  it("shows thumbnail hint text", () => {
    renderPanel();
    expect(
      screen.getByText(/auto-generated from cover front/i),
    ).toBeInTheDocument();
  });
});
