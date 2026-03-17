/**
 * Unit tests for DiscogsAddReleaseWizard – the three-step container.
 *
 * The individual step components are mocked so these tests focus purely on
 * the wizard shell: step transitions, step guards, action routing, and
 * success/cancel delegation.
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsAddReleaseWizard from "../DiscogsAddReleaseWizard";
import { fetchJson } from "../../../../lib/api";
import type { DiscogsSearchRequest, DiscogsSearchResult, DiscogsRelease } from "../../../../lib/discogs-types";

// ── Mocks ──────────────────────────────────────────────────────────────────────

jest.mock("../../../../lib/api", () => ({
  fetchJson: jest.fn(),
}));
const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

// Mock the three step components so we can drive them via test helpers
jest.mock("../DiscogsSearchStep", () => ({
  __esModule: true,
  default: function MockSearchStep({
    onSearchSuccess,
    onSearchError,
    initialValues,
  }: {
    onSearchSuccess: (results: DiscogsSearchResult[], request: DiscogsSearchRequest) => void;
    onSearchError: (msg: string) => void;
    initialValues?: DiscogsSearchRequest;
  }) {
    return (
      <div data-testid="search-step">
        <span data-testid="initial-cat">{initialValues?.catalogNumber ?? ""}</span>
        <button
          data-testid="trigger-search-success"
          onClick={() =>
            onSearchSuccess(
              [
                {
                  id: 1,
                  title: "Test Album",
                  artist: "Test Artist",
                  format: "Vinyl",
                  country: "UK",
                  year: "2020",
                  label: "Test Label",
                  catalogNumber: "CAT001",
                  resourceUrl: "",
                },
              ],
              { catalogNumber: "CAT001" }
            )
          }
        >
          Trigger Search Success
        </button>
        <button
          data-testid="trigger-search-error"
          onClick={() => onSearchError("Something went wrong")}
        >
          Trigger Search Error
        </button>
      </div>
    );
  },
}));

jest.mock("../DiscogsResultsStep", () => ({
  __esModule: true,
  default: function MockResultsStep({
    results,
    selectedResult,
    onSelectResult,
    onContinue,
    onBack,
  }: {
    results: DiscogsSearchResult[];
    selectedResult: DiscogsSearchResult | null;
    onSelectResult: (r: DiscogsSearchResult) => void;
    onContinue: (r: DiscogsSearchResult) => void;
    onBack: () => void;
  }) {
    return (
      <div data-testid="results-step">
        <span data-testid="result-count">{results.length}</span>
        <span data-testid="selected-id">{selectedResult?.id ?? "none"}</span>
        <button
          data-testid="trigger-select"
          onClick={() => onSelectResult(results[0])}
        >
          Select First
        </button>
        <button
          data-testid="trigger-continue"
          onClick={() => onContinue(results[0])}
        >
          Continue
        </button>
        <button data-testid="trigger-back" onClick={onBack}>
          Back
        </button>
      </div>
    );
  },
}));

jest.mock("../DiscogsDetailsStep", () => ({
  __esModule: true,
  default: function MockDetailsStep({
    searchResult,
    onBack,
    onAddToCollection,
    onEditRelease,
    isAdding,
  }: {
    searchResult: DiscogsSearchResult;
    onBack: () => void;
    onAddToCollection: (r: DiscogsRelease) => void;
    onEditRelease: (r: DiscogsRelease) => void;
    isAdding?: boolean;
  }) {
    const fakeRelease = {
      id: searchResult.id,
      title: "Full Release",
      artists: [{ id: 1, name: "Test Artist" }],
      labels: [],
      formats: [],
      genres: [],
      styles: [],
      country: "UK",
      releaseDate: "2020-01-01",
      year: 2020,
      tracklist: [],
      images: [],
      identifiers: [],
      uri: "",
      resourceUrl: "",
    } as DiscogsRelease;

    return (
      <div data-testid="details-step">
        <span data-testid="details-title">{searchResult.title}</span>
        <span data-testid="is-adding">{isAdding ? "yes" : "no"}</span>
        <button
          data-testid="trigger-add"
          onClick={() => onAddToCollection(fakeRelease)}
        >
          Add to Collection
        </button>
        <button
          data-testid="trigger-edit"
          onClick={() => onEditRelease(fakeRelease)}
        >
          Edit Release
        </button>
        <button data-testid="trigger-back" onClick={onBack}>
          Back to Results
        </button>
      </div>
    );
  },
}));

// Mock AddReleaseWizard for the edit path
jest.mock("../../AddReleaseWizard", () => ({
  __esModule: true,
  default: function MockAddReleaseWizard({
    onSuccess,
    onCancel,
  }: {
    onSuccess?: (id: number) => void;
    onCancel?: () => void;
  }) {
    return (
      <div data-testid="manual-wizard">
        <button data-testid="manual-save" onClick={() => onSuccess?.(99)}>
          Save
        </button>
        <button data-testid="manual-cancel" onClick={onCancel}>
          Cancel
        </button>
      </div>
    );
  },
}));

// ── Helpers ────────────────────────────────────────────────────────────────────

async function advanceToResults(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId("trigger-search-success"));
}

async function advanceToDetails(user: ReturnType<typeof userEvent.setup>) {
  await advanceToResults(user);
  await user.click(screen.getByTestId("trigger-continue"));
}

// ── Tests ──────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsAddReleaseWizard – initial render", () => {
  it("shows the search step on initial render", () => {
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    expect(screen.getByTestId("search-step")).toBeInTheDocument();
    expect(screen.queryByTestId("results-step")).not.toBeInTheDocument();
  });

  it("shows a Cancel link on the search step", () => {
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });

  it("calls onCancel when the Cancel link is clicked", async () => {
    const user = userEvent.setup();
    const onCancel = jest.fn();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={onCancel} />
    );
    await user.click(screen.getByRole("button", { name: /cancel/i }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});

describe("DiscogsAddReleaseWizard – step transitions", () => {
  it("advances to the results step after a successful search", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToResults(user);
    expect(screen.getByTestId("results-step")).toBeInTheDocument();
    expect(screen.queryByTestId("search-step")).not.toBeInTheDocument();
  });

  it("passes search results to the results step", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToResults(user);
    expect(screen.getByTestId("result-count")).toHaveTextContent("1");
  });

  it("advances to the details step when Continue is clicked in results", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    expect(screen.getByTestId("details-step")).toBeInTheDocument();
    expect(screen.queryByTestId("results-step")).not.toBeInTheDocument();
  });

  it("returns to search when Back is pressed from results", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToResults(user);
    await user.click(screen.getByTestId("trigger-back"));
    expect(screen.getByTestId("search-step")).toBeInTheDocument();
  });

  it("returns to results when Back to Results is pressed from details", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-back"));
    expect(screen.getByTestId("results-step")).toBeInTheDocument();
  });

  it("preserves the search request in initialValues when going back to search", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToResults(user);
    await user.click(screen.getByTestId("trigger-back"));
    // initialValues.catalogNumber should be CAT001 (from the mock search)
    expect(screen.getByTestId("initial-cat")).toHaveTextContent("CAT001");
  });
});

describe("DiscogsAddReleaseWizard – selection persistence", () => {
  it("preserves the selected result when navigating back from details to results", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToResults(user);
    // Select the first result
    await user.click(screen.getByTestId("trigger-select"));
    // Advance then go back
    await user.click(screen.getByTestId("trigger-continue"));
    await user.click(screen.getByTestId("trigger-back"));
    // Selected ID should still be 1
    expect(screen.getByTestId("selected-id")).toHaveTextContent("1");
  });
});

describe("DiscogsAddReleaseWizard – Add to Collection", () => {
  it("POSTs to /api/musicreleases and calls onSuccess with the release ID", async () => {
    mockFetchJson.mockResolvedValue({ release: { id: 42 } });
    const user = userEvent.setup();
    const onSuccess = jest.fn();
    render(
      <DiscogsAddReleaseWizard onSuccess={onSuccess} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-add"));
    await waitFor(() =>
      expect(mockFetchJson).toHaveBeenCalledWith(
        "/api/musicreleases",
        expect.objectContaining({ method: "POST" })
      )
    );
    await waitFor(() => expect(onSuccess).toHaveBeenCalledWith(42));
  });

  it("shows an error alert when the POST fails", async () => {
    mockFetchJson.mockRejectedValueOnce(new Error("Server error"));
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-add"));
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent(/server error/i);
  });
});

describe("DiscogsAddReleaseWizard – Edit Release", () => {
  it("switches to the manual AddReleaseWizard when Edit Release is clicked", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-edit"));
    expect(screen.getByTestId("manual-wizard")).toBeInTheDocument();
    expect(screen.queryByTestId("details-step")).not.toBeInTheDocument();
  });

  it("calls onSuccess when the manual wizard saves", async () => {
    mockFetchJson.mockResolvedValue({ release: { id: 99 } });
    const user = userEvent.setup();
    const onSuccess = jest.fn();
    render(
      <DiscogsAddReleaseWizard onSuccess={onSuccess} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-edit"));
    await user.click(screen.getByTestId("manual-save"));
    await waitFor(() => expect(onSuccess).toHaveBeenCalledWith(99));
  });

  it("returns to the details step when the manual wizard is cancelled", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsAddReleaseWizard onSuccess={jest.fn()} onCancel={jest.fn()} />
    );
    await advanceToDetails(user);
    await user.click(screen.getByTestId("trigger-edit"));
    await user.click(screen.getByTestId("manual-cancel"));
    expect(screen.getByTestId("details-step")).toBeInTheDocument();
    expect(screen.queryByTestId("manual-wizard")).not.toBeInTheDocument();
  });
});
