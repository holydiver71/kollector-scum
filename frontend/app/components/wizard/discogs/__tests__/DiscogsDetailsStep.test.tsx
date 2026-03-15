/**
 * Tests for DiscogsDetailsStep – step 3 of the Discogs wizard.
 *
 * Covers:
 *  - Loading state while the release is being fetched
 *  - Error state when the fetch fails
 *  - Rendering key release metadata from the fetched release
 *  - New-entity detection badges
 *  - Add to Collection callback
 *  - Edit Release callback
 *  - Back navigation
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsDetailsStep from "../DiscogsDetailsStep";
import type { DiscogsSearchResult, DiscogsRelease } from "../../../../lib/discogs-types";

// ── Mock api ──────────────────────────────────────────────────────────────────

jest.mock("../../../../lib/api", () => ({
  getDiscogsRelease: jest.fn(),
}));

import { getDiscogsRelease } from "../../../../lib/api";
const mockGetDiscogsRelease = getDiscogsRelease as jest.MockedFunction<
  typeof getDiscogsRelease
>;

// ── Fixtures ──────────────────────────────────────────────────────────────────

const SEARCH_RESULT: DiscogsSearchResult = {
  id: 999,
  title: "Abbey Road",
  artist: "The Beatles",
  format: "Vinyl",
  country: "UK",
  year: "1969",
  label: "Apple",
  catalogNumber: "PCS-7088",
  resourceUrl: "",
};

const RELEASE: DiscogsRelease = {
  id: 999,
  title: "Abbey Road",
  artists: [{ id: 1, name: "The Beatles" }],
  labels: [{ id: 10, name: "Apple Records", catalogNumber: "PCS 7088" }],
  formats: [{ name: "Vinyl", qty: "1", descriptions: ["LP"] }],
  genres: ["Rock"],
  styles: ["Classic Rock"],
  country: "UK",
  releaseDate: "1969-09-26",
  year: 1969,
  tracklist: [
    { position: "A1", title: "Come Together", duration: "4:19" },
    { position: "A2", title: "Something", duration: "3:03" },
  ],
  images: [
    {
      type: "primary",
      uri: "https://img.discogs.com/cover.jpg",
      uri150: "https://img.discogs.com/thumb.jpg",
      width: 600,
      height: 600,
    },
  ],
  identifiers: [{ type: "Barcode", value: "094638241522" }],
  uri: "https://www.discogs.com/release/999",
  resourceUrl: "https://api.discogs.com/releases/999",
};

// ─── Helpers ──────────────────────────────────────────────────────────────────

const defaultProps = {
  searchResult: SEARCH_RESULT,
  onAddToCollection: jest.fn(),
  onEditRelease: jest.fn(),
  onBack: jest.fn(),
  existingArtists: [],
  existingLabels: [],
  existingGenres: [],
  existingCountries: [],
  existingFormats: [],
};

function setup(props = defaultProps) {
  const user = userEvent.setup();
  const view = render(<DiscogsDetailsStep {...props} />);
  return { user, ...view };
}

// ─── Tests ────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsDetailsStep – loading state", () => {
  it("shows a loading spinner while data is being fetched", () => {
    mockGetDiscogsRelease.mockImplementation(
      () => new Promise(() => {}) // never resolves
    );
    setup();
    expect(screen.getByText(/loading release details/i)).toBeInTheDocument();
  });
});

describe("DiscogsDetailsStep – error state", () => {
  it("shows an error message when the fetch fails", async () => {
    mockGetDiscogsRelease.mockRejectedValueOnce(new Error("API error"));
    setup();
    expect(await screen.findByText(/api error/i)).toBeInTheDocument();
  });

  it("shows a Back to Results button in the error state", async () => {
    mockGetDiscogsRelease.mockRejectedValueOnce(new Error("fail"));
    setup();
    await screen.findByText(/fail/i);
    expect(
      screen.getByRole("button", { name: /back to results/i })
    ).toBeInTheDocument();
  });
});

describe("DiscogsDetailsStep – release details", () => {
  beforeEach(() => {
    mockGetDiscogsRelease.mockResolvedValue(RELEASE);
  });

  it("renders the release title", async () => {
    setup();
    expect(await screen.findByText("Abbey Road")).toBeInTheDocument();
  });

  it("renders the artist name", async () => {
    setup();
    expect(await screen.findByText("The Beatles")).toBeInTheDocument();
  });

  it("renders the tracklist tracks", async () => {
    setup();
    expect(await screen.findByText("Come Together")).toBeInTheDocument();
    expect(await screen.findByText("Something")).toBeInTheDocument();
  });

  it("renders Add to Collection button", async () => {
    setup();
    await screen.findByText("Abbey Road");
    expect(
      screen.getByRole("button", { name: /add to collection/i })
    ).toBeInTheDocument();
  });

  it("renders Edit Release button", async () => {
    setup();
    await screen.findByText("Abbey Road");
    expect(
      screen.getByRole("button", { name: /edit release/i })
    ).toBeInTheDocument();
  });

  it("renders Back to Results button", async () => {
    setup();
    await screen.findByText("Abbey Road");
    expect(
      screen.getByRole("button", { name: /back to results/i })
    ).toBeInTheDocument();
  });
});

describe("DiscogsDetailsStep – new-entity detection", () => {
  beforeEach(() => {
    mockGetDiscogsRelease.mockResolvedValue(RELEASE);
  });

  it("shows new entities notice when entities are not in the existing lists", async () => {
    setup();
    expect(await screen.findByText(/new entr/i)).toBeInTheDocument();
  });

  it("does not show new entities notice when all entities exist", async () => {
    const props = {
      ...defaultProps,
      existingArtists: ["The Beatles"],
      existingLabels: ["Apple Records"],
      existingGenres: ["Rock", "Classic Rock"],
      existingCountries: ["UK"],
      existingFormats: ["Vinyl"],
    };
    setup(props);
    await screen.findByText("Abbey Road");
    expect(
      screen.queryByText(/new entries will be created/i)
    ).not.toBeInTheDocument();
  });
});

describe("DiscogsDetailsStep – callbacks", () => {
  beforeEach(() => {
    mockGetDiscogsRelease.mockResolvedValue(RELEASE);
  });

  it("calls onAddToCollection with a MappedDiscogsRelease when Add is clicked", async () => {
    const onAddToCollection = jest.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(
      <DiscogsDetailsStep {...defaultProps} onAddToCollection={onAddToCollection} />
    );
    await screen.findByText("Abbey Road");
    await user.click(screen.getByRole("button", { name: /add to collection/i }));
    await waitFor(() => {
      expect(onAddToCollection).toHaveBeenCalledWith(
        expect.objectContaining({
          dto: expect.objectContaining({ title: "Abbey Road" }),
          sourceImages: expect.objectContaining({
            cover: "https://img.discogs.com/cover.jpg",
          }),
        })
      );
    });
  });

  it("calls onEditRelease with a MappedDiscogsRelease when Edit is clicked", async () => {
    const onEditRelease = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsDetailsStep {...defaultProps} onEditRelease={onEditRelease} />
    );
    await screen.findByText("Abbey Road");
    await user.click(screen.getByRole("button", { name: /edit release/i }));
    expect(onEditRelease).toHaveBeenCalledWith(
      expect.objectContaining({
        dto: expect.objectContaining({ title: "Abbey Road" }),
      })
    );
  });

  it("calls onBack when Back to Results is clicked", async () => {
    const onBack = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsDetailsStep {...defaultProps} onBack={onBack} />
    );
    await screen.findByText("Abbey Road");
    await user.click(screen.getByRole("button", { name: /back to results/i }));
    expect(onBack).toHaveBeenCalled();
  });
});
