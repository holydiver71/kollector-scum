/**
 * Tests for DiscogsAddReleaseWizard – the container that orchestrates
 * the three-step Discogs import flow.
 *
 * All child step components are mocked so this test concentrates on:
 *  - Initial rendering (search step shown first)
 *  - Advancing from search to results
 *  - Advancing from results to details
 *  - Navigating backwards
 *  - Add-to-collection success path
 *  - Add-to-collection error handling
 *  - Edit-release handoff
 *  - Step indicator rendering
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsAddReleaseWizard from "../DiscogsAddReleaseWizard";
import type { DiscogsSearchRequest, DiscogsSearchResult } from "../../../../lib/discogs-types";
import type { MappedDiscogsRelease } from "../mapDiscogsRelease";

// ── Mock child steps ──────────────────────────────────────────────────────────

const mockSearchSuccess = jest.fn();
const mockSearchError = jest.fn();
const mockResultContinue = jest.fn();
const mockResultBack = jest.fn();
const mockAddToCollection = jest.fn();
const mockEditRelease = jest.fn();
const mockDetailsBack = jest.fn();

jest.mock("../DiscogsSearchStep", () => ({
  __esModule: true,
  default: function MockSearchStep({
    onSearchSuccess,
    onSearchError,
    onSwitchToManual,
  }: {
    onSearchSuccess: (r: DiscogsSearchResult[], req: DiscogsSearchRequest) => void;
    onSearchError: (msg: string) => void;
    onSwitchToManual?: () => void;
  }) {
    mockSearchSuccess.mockImplementation(onSearchSuccess);
    mockSearchError.mockImplementation(onSearchError);
    return (
      <div data-testid="search-step">
        <button
          onClick={() =>
            onSearchSuccess(
              [
                {
                  id: 1,
                  title: "Abbey Road",
                  artist: "The Beatles",
                  format: "Vinyl",
                  country: "UK",
                  year: "1969",
                  label: "Apple",
                  catalogNumber: "PCS-7088",
                  resourceUrl: "",
                },
              ],
              { catalogNumber: "PCS-7088" }
            )
          }
        >
          trigger-search-success
        </button>
        <button onClick={() => onSearchError("No results")}>
          trigger-search-error
        </button>
        {onSwitchToManual && (
          <button onClick={onSwitchToManual}>switch-to-manual</button>
        )}
      </div>
    );
  },
}));

jest.mock("../DiscogsResultsStep", () => ({
  __esModule: true,
  default: function MockResultsStep({
    onContinue,
    onBack,
  }: {
    results: DiscogsSearchResult[];
    onContinue: (r: DiscogsSearchResult) => void;
    onBack: () => void;
  }) {
    mockResultContinue.mockImplementation(onContinue);
    mockResultBack.mockImplementation(onBack);
    return (
      <div data-testid="results-step">
        <button
          onClick={() =>
            onContinue({
              id: 1,
              title: "Abbey Road",
              artist: "The Beatles",
              format: "Vinyl",
              country: "UK",
              year: "1969",
              label: "Apple",
              catalogNumber: "PCS-7088",
              resourceUrl: "",
            })
          }
        >
          trigger-result-continue
        </button>
        <button onClick={onBack}>trigger-result-back</button>
      </div>
    );
  },
}));

const mockMapped: MappedDiscogsRelease = {
  dto: { title: "Abbey Road", artistNames: ["The Beatles"] },
  sourceImages: {
    cover: "https://img.discogs.com/cover.jpg",
    thumbnail: "https://img.discogs.com/thumb.jpg",
  },
};

jest.mock("../DiscogsDetailsStep", () => ({
  __esModule: true,
  default: function MockDetailsStep({
    onAddToCollection,
    onEditRelease,
    onBack,
  }: {
    onAddToCollection: (m: MappedDiscogsRelease) => void;
    onEditRelease: (m: MappedDiscogsRelease) => void;
    onBack: () => void;
  }) {
    mockAddToCollection.mockImplementation(onAddToCollection);
    mockEditRelease.mockImplementation(onEditRelease);
    mockDetailsBack.mockImplementation(onBack);
    return (
      <div data-testid="details-step">
        <button onClick={() => onAddToCollection(mockMapped)}>
          trigger-add
        </button>
        <button onClick={() => onEditRelease(mockMapped)}>
          trigger-edit
        </button>
        <button onClick={onBack}>trigger-details-back</button>
      </div>
    );
  },
}));

// ── Mock lookups hook ─────────────────────────────────────────────────────────

jest.mock("../../useReleaseLookups", () => ({
  useReleaseLookups: () => ({
    artists: [],
    labels: [],
    genres: [],
    countries: [],
    formats: [],
    packagings: [],
    stores: [],
    loading: false,
    error: null,
  }),
}));

// ── Mock fetchJson ────────────────────────────────────────────────────────────

jest.mock("../../../../lib/api", () => ({
  fetchJson: jest.fn(),
}));

import { fetchJson } from "../../../../lib/api";
const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

// ── Mock downloadDiscogsImages ────────────────────────────────────────────────

jest.mock("../mapDiscogsRelease", () => ({
  ...jest.requireActual("../mapDiscogsRelease"),
  downloadDiscogsImages: jest.fn().mockResolvedValue(undefined),
}));

// ─── Helpers ──────────────────────────────────────────────────────────────────

const defaultProps = {
  onSuccess: jest.fn(),
  onEditRelease: jest.fn(),
  onCancel: jest.fn(),
};

async function advanceToResults(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByText("trigger-search-success"));
}

async function advanceToDetails(user: ReturnType<typeof userEvent.setup>) {
  await advanceToResults(user);
  await user.click(await screen.findByText("trigger-result-continue"));
}

// ─── Tests ────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsAddReleaseWizard – initial state", () => {
  it("shows the search step on first render", () => {
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    expect(screen.getByTestId("search-step")).toBeInTheDocument();
    expect(screen.queryByTestId("results-step")).not.toBeInTheDocument();
    expect(screen.queryByTestId("details-step")).not.toBeInTheDocument();
  });

  it("shows the step indicator with Search, Results, Details labels", () => {
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    expect(screen.getByText("Search")).toBeInTheDocument();
    expect(screen.getByText("Results")).toBeInTheDocument();
    expect(screen.getByText("Details")).toBeInTheDocument();
  });
});

describe("DiscogsAddReleaseWizard – search → results", () => {
  it("advances to the results step after a successful search", async () => {
    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await advanceToResults(user);
    expect(await screen.findByTestId("results-step")).toBeInTheDocument();
    expect(screen.queryByTestId("search-step")).not.toBeInTheDocument();
  });

  it("shows a search error notice on the search step", async () => {
    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await user.click(screen.getByText("trigger-search-error"));
    expect(await screen.findByText(/no results/i)).toBeInTheDocument();
    // Still on search step
    expect(screen.getByTestId("search-step")).toBeInTheDocument();
  });
});

describe("DiscogsAddReleaseWizard – results → details", () => {
  it("advances to the details step after a result is selected", async () => {
    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await advanceToDetails(user);
    expect(await screen.findByTestId("details-step")).toBeInTheDocument();
    expect(screen.queryByTestId("results-step")).not.toBeInTheDocument();
  });
});

describe("DiscogsAddReleaseWizard – back navigation", () => {
  it("goes back to search from results", async () => {
    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await advanceToResults(user);
    await user.click(await screen.findByText("trigger-result-back"));
    expect(screen.getByTestId("search-step")).toBeInTheDocument();
  });

  it("goes back to results from details", async () => {
    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await advanceToDetails(user);
    await user.click(await screen.findByText("trigger-details-back"));
    expect(await screen.findByTestId("results-step")).toBeInTheDocument();
  });
});

describe("DiscogsAddReleaseWizard – add to collection", () => {
  it("calls onSuccess after a successful add", async () => {
    const onSuccess = jest.fn();
    mockFetchJson.mockResolvedValueOnce({ release: { id: 42 } });

    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard
        {...defaultProps}
        onSuccess={onSuccess}
      />
    );
    await advanceToDetails(user);
    await user.click(await screen.findByText("trigger-add"));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledWith(42);
    });
  });

  it("shows an error message when the API call fails", async () => {
    mockFetchJson.mockRejectedValueOnce(new Error("Server error"));

    const user = userEvent.setup();
    render(<DiscogsAddReleaseWizard {...defaultProps} />);
    await advanceToDetails(user);
    await user.click(await screen.findByText("trigger-add"));

    expect(await screen.findByText(/server error/i)).toBeInTheDocument();
  });
});

describe("DiscogsAddReleaseWizard – edit release handoff", () => {
  it("calls onEditRelease with the mapped DTO and source images", async () => {
    const onEditRelease = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard
        {...defaultProps}
        onEditRelease={onEditRelease}
      />
    );
    await advanceToDetails(user);
    await user.click(await screen.findByText("trigger-edit"));

    expect(onEditRelease).toHaveBeenCalledWith(
      mockMapped.dto,
      mockMapped.sourceImages
    );
  });
});

describe("DiscogsAddReleaseWizard – cancel / manual switch", () => {
  it("calls onCancel when switch-to-manual is clicked inside the search step", async () => {
    const onCancel = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard
        {...defaultProps}
        onCancel={onCancel}
      />
    );
    await user.click(screen.getByText("switch-to-manual"));
    expect(onCancel).toHaveBeenCalled();
  });
});
