/**
 * Tests for the reworked ImagesPanel (wizard step 4 / Images).
 *
 * Focuses on:
 *  - "Search Web" button opens ImageSearchModal
 *  - "Upload File" button triggers hidden file input
 *  - Client-side upload validation (size, extension)
 *  - Thumbnail is auto-filled after successful upload
 */
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ImagesPanel from "../panels/ImagesPanel";
import type { WizardFormData } from "../types";

// Mock the ImageSearchModal so we can assert it opens without rendering it fully
jest.mock("../ImageSearchModal", () => ({
  __esModule: true,
  default: function MockImageSearchModal({
    onClose,
    onSelect,
  }: {
    onClose: () => void;
    onSelect: (url: string) => void;
    defaultQuery: string;
  }) {
    return (
      <div data-testid="image-search-modal">
        <button
          data-testid="modal-select"
          onClick={() => onSelect("https://example.com/cover.jpg")}
        />
        <button data-testid="modal-close" onClick={onClose} />
      </div>
    );
  },
}));

const BASE_DATA: WizardFormData = {
  title: "Killers",
  artistIds: [],
  artistNames: ["Iron Maiden"],
  artistDisplayNames: ["Iron Maiden"],
  genreIds: [],
  genreNames: [],
  live: false,
  formatName: "",
  packagingName: "",
  countryName: "",
  releaseYear: "1981",
  origReleaseYear: "",
  labelName: "",
  labelNumber: "",
  upc: "",
  purchaseInfo: { currency: "GBP" },
  images: {},
  media: [],
  links: [],
};

function renderPanel(
  dataOverride: Partial<WizardFormData> = {},
  onChange = jest.fn()
) {
  const data = { ...BASE_DATA, ...dataOverride };
  return render(
    <ImagesPanel data={data} onChange={onChange} errors={{}} />
  );
}

beforeEach(() => {
  jest.clearAllMocks();
  // Reset fetch mock to a default response (use jest.fn() directly, not Response constructor)
  (global.fetch as jest.Mock).mockResolvedValue({
    ok: true,
    status: 200,
    json: async () => ({
      filename: "abc.jpg",
      publicUrl: "https://cdn.example.com/abc.jpg",
      thumbnailFilename: "thumb-abc.jpg",
      thumbnailPublicUrl: "https://cdn.example.com/thumb-abc.jpg",
    }),
    text: async () => "",
  });
});

describe("ImagesPanel", () => {
  // ── Search Web button ────────────────────────────────────────────────────

  test("renders 'Search Web' and 'Upload File' buttons", () => {
    renderPanel();
    expect(screen.getByTestId("search-web-button")).toBeInTheDocument();
    expect(screen.getByTestId("upload-file-button")).toBeInTheDocument();
  });

  test("clicking 'Search Web' opens the ImageSearchModal", async () => {
    const user = userEvent.setup();
    renderPanel();
    expect(screen.queryByTestId("image-search-modal")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("search-web-button"));
    expect(screen.getByTestId("image-search-modal")).toBeInTheDocument();
  });

  test("selecting an image from the modal calls onChange with coverFront and thumbnail", async () => {
    const user = userEvent.setup();
    const onChange = jest.fn();
    renderPanel({}, onChange);
    await user.click(screen.getByTestId("search-web-button"));
    await user.click(screen.getByTestId("modal-select"));

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/api/images/download"),
        expect.objectContaining({ method: "POST" })
      );
    });

    await waitFor(() => {
      const calls = onChange.mock.calls;
      expect(calls.length).toBeGreaterThan(0);
      const lastCall = calls[calls.length - 1][0];
      expect(lastCall.images.coverFront).toBeTruthy();
      expect(lastCall.images.thumbnail).toBeTruthy();
    });
  });

  test("closing the modal without selecting hides it", async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId("search-web-button"));
    expect(screen.getByTestId("image-search-modal")).toBeInTheDocument();
    await user.click(screen.getByTestId("modal-close"));
    expect(screen.queryByTestId("image-search-modal")).not.toBeInTheDocument();
  });

  // ── Upload File button ───────────────────────────────────────────────────

  test("clicking 'Upload File' triggers the hidden file input", async () => {
    const user = userEvent.setup();
    renderPanel();
    const fileInput = screen.getByTestId("file-input") as HTMLInputElement;
    const clickSpy = jest.spyOn(fileInput, "click");
    await user.click(screen.getByTestId("upload-file-button"));
    expect(clickSpy).toHaveBeenCalled();
  });

  test("uploading a file with invalid extension shows an error", async () => {
    renderPanel();
    const fileInput = screen.getByTestId("file-input") as HTMLInputElement;
    const file = new File(["content"], "cover.bmp", { type: "image/bmp" });
    Object.defineProperty(fileInput, "files", { value: [file], configurable: true });
    fireEvent.change(fileInput);
    await waitFor(() => {
      expect(screen.getByTestId("upload-error")).toBeInTheDocument();
      expect(screen.getByTestId("upload-error").textContent).toContain(".bmp");
    });
  });

  test("uploading a file over 5 MB shows a size error", async () => {
    renderPanel();
    const fileInput = screen.getByTestId("file-input") as HTMLInputElement;
    // Create a file whose .size property > 5MB
    const bigContent = new Uint8Array(6 * 1024 * 1024);
    const file = new File([bigContent], "huge.jpg", { type: "image/jpeg" });
    Object.defineProperty(fileInput, "files", { value: [file], configurable: true });
    fireEvent.change(fileInput);
    await waitFor(() => {
      expect(screen.getByTestId("upload-error")).toBeInTheDocument();
      expect(screen.getByTestId("upload-error").textContent).toContain("too large");
    });
  });

  test("successful upload sets coverFront and thumbnail via onChange", async () => {
    const onChange = jest.fn();
    renderPanel({}, onChange);
    const fileInput = screen.getByTestId("file-input") as HTMLInputElement;
    const file = new File(["img"], "cover.jpg", { type: "image/jpeg" });
    Object.defineProperty(fileInput, "files", { value: [file], configurable: true });
    fireEvent.change(fileInput);

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining("/api/images/upload"),
        expect.objectContaining({ method: "POST" })
      );
    });

    await waitFor(() => {
      const calls = onChange.mock.calls;
      expect(calls.length).toBeGreaterThan(0);
      const lastCall = calls[calls.length - 1][0];
      expect(lastCall.images.coverFront).toBeTruthy();
      expect(lastCall.images.thumbnail).toBeTruthy();
    });
  });

  // ── Thumbnail note ───────────────────────────────────────────────────────

  test("renders the thumbnail auto-generate note", () => {
    renderPanel();
    expect(
      screen.getByText(/Auto-generated from Cover Front/i)
    ).toBeInTheDocument();
  });
});
