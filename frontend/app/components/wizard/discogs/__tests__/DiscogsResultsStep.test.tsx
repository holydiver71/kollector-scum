/**
 * Tests for DiscogsResultsStep – step 2 of the Discogs wizard.
 *
 * Covers:
 *  - Rendering a list of results
 *  - Requiring a selection before continuing
 *  - Inline validation message
 *  - Keyboard accessibility
 *  - Back navigation
 */

import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsResultsStep from "../DiscogsResultsStep";
import type { DiscogsSearchResult } from "../../../../lib/discogs-types";

// ── Fixtures ──────────────────────────────────────────────────────────────────

const RESULT_A: DiscogsSearchResult = {
  id: 1,
  title: "Abbey Road",
  artist: "The Beatles",
  format: "Vinyl",
  country: "UK",
  year: "1969",
  label: "Apple",
  catalogNumber: "PCS-7088",
  resourceUrl: "",
};

const RESULT_B: DiscogsSearchResult = {
  id: 2,
  title: "Let It Be",
  artist: "The Beatles",
  format: "Vinyl",
  country: "UK",
  year: "1970",
  label: "Apple",
  catalogNumber: "PCS-7096",
  resourceUrl: "",
};

// ─── Helpers ──────────────────────────────────────────────────────────────────

function setup(props?: Partial<Parameters<typeof DiscogsResultsStep>[0]>) {
  const defaults = {
    results: [RESULT_A, RESULT_B],
    onContinue: jest.fn(),
    onBack: jest.fn(),
  };
  const user = userEvent.setup();
  const view = render(
    <DiscogsResultsStep {...defaults} {...props} />
  );
  return { user, onContinue: defaults.onContinue, onBack: defaults.onBack, ...view };
}

// ─── Tests ────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsResultsStep – rendering", () => {
  it("renders all result titles", () => {
    setup();
    expect(screen.getByText("Abbey Road")).toBeInTheDocument();
    expect(screen.getByText("Let It Be")).toBeInTheDocument();
  });

  it("shows the result count in the header", () => {
    setup();
    expect(screen.getByText(/2 matches found/i)).toBeInTheDocument();
  });

  it("shows singular 'match' when there is one result", () => {
    setup({ results: [RESULT_A] });
    expect(screen.getByText(/1 match found/i)).toBeInTheDocument();
  });

  it("renders a Continue button", () => {
    setup();
    expect(
      screen.getByRole("button", { name: /view details/i })
    ).toBeInTheDocument();
  });

  it("renders a Back button", () => {
    setup();
    expect(screen.getByRole("button", { name: /back/i })).toBeInTheDocument();
  });
});

describe("DiscogsResultsStep – selection required", () => {
  it("blocks Continue when no result is selected", async () => {
    const { user, onContinue } = setup();
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(/please select a release/i);
    expect(onContinue).not.toHaveBeenCalled();
  });

  it("shows the inline validation error only after the user attempts to continue", () => {
    setup();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("allows continuing once a result is selected", async () => {
    const { user, onContinue } = setup();
    // Click the first result row (radio)
    const radios = screen.getAllByRole("radio");
    await user.click(radios[0]);
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_A);
  });

  it("passes the selected result to onContinue", async () => {
    const { user, onContinue } = setup();
    const radios = screen.getAllByRole("radio");
    await user.click(radios[1]);
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_B);
  });

  it("clears the validation error once a selection is made", async () => {
    const { user } = setup();
    await user.click(screen.getByRole("button", { name: /view details/i }));
    await screen.findByRole("alert");
    const radios = screen.getAllByRole("radio");
    await user.click(radios[0]);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});

describe("DiscogsResultsStep – pre-selection", () => {
  it("honours initialSelection so Continue works immediately", async () => {
    const { user, onContinue } = setup({ initialSelection: RESULT_A });
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_A);
  });

  it("marks the pre-selected result as aria-checked", () => {
    setup({ initialSelection: RESULT_B });
    const radios = screen.getAllByRole("radio");
    expect(radios[1]).toHaveAttribute("aria-checked", "true");
  });
});

describe("DiscogsResultsStep – keyboard accessibility", () => {
  it("selects a result with the Enter key", async () => {
    const { user, onContinue } = setup();
    const radios = screen.getAllByRole("radio");
    radios[0].focus();
    await user.keyboard("{Enter}");
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_A);
  });

  it("selects a result with the Space key", async () => {
    const { user, onContinue } = setup();
    const radios = screen.getAllByRole("radio");
    radios[1].focus();
    await user.keyboard(" ");
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(onContinue).toHaveBeenCalledWith(RESULT_B);
  });
});

describe("DiscogsResultsStep – back navigation", () => {
  it("calls onBack when Back is clicked", async () => {
    const { user, onBack } = setup();
    await user.click(screen.getByRole("button", { name: /back/i }));
    expect(onBack).toHaveBeenCalled();
  });
});
