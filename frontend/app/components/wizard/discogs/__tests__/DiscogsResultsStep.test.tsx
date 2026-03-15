/**
 * Unit tests for DiscogsResultsStep – Step 2 of the Discogs wizard.
 *
 * Covers:
 *  - Rendering the results list with count
 *  - Result selection highlights the selected card
 *  - Continue button is disabled until a result is selected
 *  - Inline status message when no result is selected
 *  - Continue button calls onContinue with the selected result
 *  - Back button calls onBack
 *  - Selected result persists across re-renders (controlled by parent)
 */

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsResultsStep from "../DiscogsResultsStep";
import type { DiscogsSearchResult } from "../../../../lib/discogs-types";

// next/image mock
jest.mock("next/image", () => ({
  __esModule: true,
  default: function MockImage({ alt }: { alt: string }) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img alt={alt} />;
  },
}));

// ─── Fixtures ─────────────────────────────────────────────────────────────────

const RESULT_A: DiscogsSearchResult = {
  id: 1,
  title: "Album One",
  artist: "Artist A",
  format: "Vinyl",
  country: "UK",
  year: "2020",
  label: "Label A",
  catalogNumber: "CAT001",
  resourceUrl: "https://api.discogs.com/releases/1",
};

const RESULT_B: DiscogsSearchResult = {
  id: 2,
  title: "Album Two",
  artist: "Artist B",
  format: "CD",
  country: "US",
  year: "2019",
  label: "Label B",
  catalogNumber: "CAT002",
  resourceUrl: "https://api.discogs.com/releases/2",
};

const RESULTS = [RESULT_A, RESULT_B];

const noop = () => {};

// ─── Tests ────────────────────────────────────────────────────────────────────

describe("DiscogsResultsStep – rendering", () => {
  it("displays the result count in the heading area", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(screen.getByText(/2 matches found/i)).toBeInTheDocument();
  });

  it("renders a card for each result", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(screen.getByRole("button", { name: /select album one/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /select album two/i })).toBeInTheDocument();
  });

  it("shows 'match' (singular) for a single result", () => {
    render(
      <DiscogsResultsStep
        results={[RESULT_A]}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(screen.getByText(/1 match found/i)).toBeInTheDocument();
  });
});

describe("DiscogsResultsStep – selection", () => {
  it("calls onSelectResult with the clicked result", async () => {
    const user = userEvent.setup();
    const onSelectResult = jest.fn();
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={onSelectResult}
        onContinue={noop}
        onBack={noop}
      />
    );
    await user.click(screen.getByRole("button", { name: /select album one/i }));
    expect(onSelectResult).toHaveBeenCalledWith(RESULT_A);
  });

  it("marks the selected result card as pressed (aria-pressed=true)", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={RESULT_A}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    const card = screen.getByRole("button", { name: /select album one/i });
    expect(card).toHaveAttribute("aria-pressed", "true");
  });

  it("does not mark other result cards as pressed", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={RESULT_A}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    const other = screen.getByRole("button", { name: /select album two/i });
    expect(other).toHaveAttribute("aria-pressed", "false");
  });
});

describe("DiscogsResultsStep – Continue button", () => {
  it("is disabled when no result is selected", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(
      screen.getByRole("button", { name: /continue/i })
    ).toBeDisabled();
  });

  it("is enabled after a result is selected", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={RESULT_A}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(
      screen.getByRole("button", { name: /continue/i })
    ).not.toBeDisabled();
  });

  it("shows a 'Select a release to continue' hint when nothing is selected", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(
      screen.getByRole("status")
    ).toHaveTextContent(/select a release to continue/i);
  });

  it("hides the hint once a result is selected", () => {
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={RESULT_A}
        onSelectResult={noop}
        onContinue={noop}
        onBack={noop}
      />
    );
    expect(
      screen.queryByRole("status")
    ).not.toBeInTheDocument();
  });

  it("calls onContinue with the selected result when Continue is clicked", async () => {
    const user = userEvent.setup();
    const onContinue = jest.fn();
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={RESULT_A}
        onSelectResult={noop}
        onContinue={onContinue}
        onBack={noop}
      />
    );
    await user.click(screen.getByRole("button", { name: /continue/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_A);
  });
});

describe("DiscogsResultsStep – Back button", () => {
  it("calls onBack when the Back button is clicked", async () => {
    const user = userEvent.setup();
    const onBack = jest.fn();
    render(
      <DiscogsResultsStep
        results={RESULTS}
        selectedResult={null}
        onSelectResult={noop}
        onContinue={noop}
        onBack={onBack}
      />
    );
    await user.click(screen.getByRole("button", { name: /back/i }));
    expect(onBack).toHaveBeenCalledTimes(1);
  });
});
