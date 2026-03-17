/**
 * Unit tests for DiscogsDetailsStep – Step 3 of the Discogs wizard.
 *
 * Covers:
 *  - Loading state while fetching the release
 *  - Error state when the fetch fails
 *  - Successful render of release metadata (title, artist, year, etc.)
 *  - Tracklist rendering
 *  - "Add to Collection" button calls onAddToCollection
 *  - "Edit Release" button calls onEditRelease
 *  - "Back to Results" button calls onBack
 *  - isAdding prop disables action buttons and shows loading indicator
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsDetailsStep from "../DiscogsDetailsStep";
import { getDiscogsRelease } from "../../../../lib/api";
import type { DiscogsRelease, DiscogsSearchResult } from "../../../../lib/discogs-types";

// Mock API
jest.mock("../../../../lib/api", () => ({
  getDiscogsRelease: jest.fn(),
}));
const mockGetDiscogsRelease = getDiscogsRelease as jest.MockedFunction<
  typeof getDiscogsRelease
>;

// Mock next/image
jest.mock("next/image", () => ({
  __esModule: true,
  default: function MockImage({ alt }: { alt: string }) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img alt={alt} />;
  },
}));

// ─── Fixtures ─────────────────────────────────────────────────────────────────

const SEARCH_RESULT: DiscogsSearchResult = {
  id: 12345,
  title: "Dark Side",
  artist: "Pink Floyd",
  format: "Vinyl",
  country: "UK",
  year: "1973",
  label: "Harvest",
  catalogNumber: "SHVL 804",
  resourceUrl: "https://api.discogs.com/releases/12345",
};

const FULL_RELEASE: DiscogsRelease = {
  id: 12345,
  title: "The Dark Side of the Moon",
  artists: [{ id: 1, name: "Pink Floyd" }],
  labels: [{ id: 10, name: "Harvest", catalogNumber: "SHVL 804" }],
  formats: [{ name: "Vinyl", qty: "1", descriptions: ["LP"] }],
  genres: ["Rock"],
  styles: ["Psychedelic Rock"],
  country: "UK",
  releaseDate: "1973-03-01",
  year: 1973,
  tracklist: [
    { position: "A1", title: "Speak to Me", duration: "1:30" },
    { position: "A2", title: "Breathe", duration: "2:43" },
  ],
  images: [
    {
      type: "primary",
      uri: "https://img.discogs.com/cover.jpg",
      uri150: "https://img.discogs.com/cover-150.jpg",
      width: 600,
      height: 600,
    },
  ],
  identifiers: [{ type: "Barcode", value: "5099902987927" }],
  uri: "https://www.discogs.com/release/12345",
  resourceUrl: "https://api.discogs.com/releases/12345",
};

const noop = () => {};

beforeEach(() => {
  jest.clearAllMocks();
});

// ─── Loading state ─────────────────────────────────────────────────────────────

describe("DiscogsDetailsStep – loading state", () => {
  it("shows a loading spinner while fetching the release", () => {
    mockGetDiscogsRelease.mockReturnValueOnce(new Promise(() => {}));
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    expect(screen.getByRole("img", { name: /loading release details/i })).toBeInTheDocument();
  });

  it("hides the spinner after the release loads", async () => {
    mockGetDiscogsRelease.mockResolvedValueOnce(FULL_RELEASE);
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    await waitFor(() =>
      expect(
        screen.queryByRole("img", { name: /loading release details/i })
      ).not.toBeInTheDocument()
    );
  });
});

// ─── Error state ───────────────────────────────────────────────────────────────

describe("DiscogsDetailsStep – error state", () => {
  it("shows an error alert when the API call fails", async () => {
    mockGetDiscogsRelease.mockRejectedValueOnce(new Error("Network failure"));
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent(/network failure/i);
  });

  it("renders a Back to Results button in the error state", async () => {
    mockGetDiscogsRelease.mockRejectedValueOnce(new Error("Oops"));
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    await screen.findByRole("alert");
    expect(
      screen.getByRole("button", { name: /back to results/i })
    ).toBeInTheDocument();
  });
});

// ─── Successful render ─────────────────────────────────────────────────────────

describe("DiscogsDetailsStep – release details", () => {
  beforeEach(() => {
    mockGetDiscogsRelease.mockResolvedValue(FULL_RELEASE);
  });

  it("displays the release title", async () => {
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    expect(
      await screen.findByRole("heading", { name: /the dark side of the moon/i })
    ).toBeInTheDocument();
  });

  it("displays the artist name", async () => {
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    await screen.findByRole("heading", { name: /the dark side/i });
    expect(screen.getByText(/pink floyd/i)).toBeInTheDocument();
  });

  it("displays tracklist entries", async () => {
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    expect(await screen.findByText("Speak to Me")).toBeInTheDocument();
    expect(screen.getByText("Breathe")).toBeInTheDocument();
  });

  it("renders the 'Add to Collection' and 'Edit Release' buttons", async () => {
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    await screen.findByRole("heading", { name: /the dark side/i });
    expect(
      screen.getByRole("button", { name: /add to collection/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /edit release/i })
    ).toBeInTheDocument();
  });
});

// ─── Action buttons ────────────────────────────────────────────────────────────

describe("DiscogsDetailsStep – actions", () => {
  beforeEach(() => {
    mockGetDiscogsRelease.mockResolvedValue(FULL_RELEASE);
  });

  it("calls onAddToCollection with the full release when the button is clicked", async () => {
    const user = userEvent.setup();
    const onAddToCollection = jest.fn();
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={onAddToCollection}
        onEditRelease={noop}
      />
    );
    await screen.findByRole("button", { name: /add to collection/i });
    await user.click(screen.getByRole("button", { name: /add to collection/i }));
    expect(onAddToCollection).toHaveBeenCalledWith(FULL_RELEASE);
  });

  it("calls onEditRelease with the full release when the button is clicked", async () => {
    const user = userEvent.setup();
    const onEditRelease = jest.fn();
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={onEditRelease}
      />
    );
    await screen.findByRole("button", { name: /edit release/i });
    await user.click(screen.getByRole("button", { name: /edit release/i }));
    expect(onEditRelease).toHaveBeenCalledWith(FULL_RELEASE);
  });

  it("calls onBack when Back to Results is clicked", async () => {
    const user = userEvent.setup();
    const onBack = jest.fn();
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={onBack}
        onAddToCollection={noop}
        onEditRelease={noop}
      />
    );
    await screen.findByRole("button", { name: /back to results/i });
    await user.click(screen.getByRole("button", { name: /back to results/i }));
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("disables Add to Collection and Edit Release buttons while isAdding is true", async () => {
    render(
      <DiscogsDetailsStep
        searchResult={SEARCH_RESULT}
        onBack={noop}
        onAddToCollection={noop}
        onEditRelease={noop}
        isAdding
      />
    );
    await screen.findByRole("button", { name: /adding/i });
    expect(screen.getByRole("button", { name: /adding/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /edit release/i })).toBeDisabled();
  });
});
