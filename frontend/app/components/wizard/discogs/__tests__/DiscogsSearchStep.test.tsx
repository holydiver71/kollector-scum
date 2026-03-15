/**
 * Unit tests for DiscogsSearchStep – Step 1 of the Discogs wizard.
 *
 * Covers:
 *  - Rendering the form inputs
 *  - Validation: blocking submit when catalogue number is empty
 *  - Successful search advances via onSearchSuccess
 *  - API errors and no-results are surfaced via onSearchError
 *  - initialValues pre-populate the form for Back navigation
 *  - Clear button resets all fields
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsSearchStep from "../DiscogsSearchStep";
import { searchDiscogs } from "../../../../lib/api";

jest.mock("../../../../lib/api", () => ({
  searchDiscogs: jest.fn(),
}));
const mockSearchDiscogs = searchDiscogs as jest.MockedFunction<typeof searchDiscogs>;

const MOCK_RESULTS = [
  {
    id: 1,
    title: "Test Album",
    artist: "Test Artist",
    format: "Vinyl",
    country: "UK",
    year: "2020",
    label: "Test Label",
    catalogNumber: "CAT001",
    resourceUrl: "https://api.discogs.com/releases/1",
  },
];

const noop = () => {};

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsSearchStep – rendering", () => {
  it("renders the catalogue number input", () => {
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    expect(
      screen.getByLabelText(/catalogue number/i)
    ).toBeInTheDocument();
  });

  it("renders optional filter inputs for format, country, and year", () => {
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    expect(screen.getByLabelText(/format/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/country/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/year/i)).toBeInTheDocument();
  });

  it("renders Search Discogs and Clear buttons", () => {
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /clear/i })
    ).toBeInTheDocument();
  });
});

describe("DiscogsSearchStep – initialValues", () => {
  it("pre-populates the catalogue number from initialValues", () => {
    render(
      <DiscogsSearchStep
        initialValues={{ catalogNumber: "SHVL804", format: "Vinyl" }}
        onSearchSuccess={noop}
        onSearchError={noop}
      />
    );
    expect(screen.getByLabelText(/catalogue number/i)).toHaveValue("SHVL804");
  });

  it("pre-populates optional filter fields from initialValues", () => {
    render(
      <DiscogsSearchStep
        initialValues={{ catalogNumber: "X", format: "CD", country: "US", year: 2021 }}
        onSearchSuccess={noop}
        onSearchError={noop}
      />
    );
    expect(screen.getByLabelText(/format/i)).toHaveValue("CD");
    expect(screen.getByLabelText(/country/i)).toHaveValue("US");
    expect(screen.getByLabelText(/year/i)).toHaveValue(2021);
  });
});

describe("DiscogsSearchStep – validation", () => {
  it("disables the Search button when the catalogue number field is empty", () => {
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).toBeDisabled();
  });

  it("enables the Search button once the user types a catalogue number", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).not.toBeDisabled();
  });
});

describe("DiscogsSearchStep – successful search", () => {
  it("calls onSearchSuccess with results and the request on a successful search", async () => {
    mockSearchDiscogs.mockResolvedValueOnce(MOCK_RESULTS);
    const user = userEvent.setup();
    const onSearchSuccess = jest.fn();
    render(
      <DiscogsSearchStep onSearchSuccess={onSearchSuccess} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    await waitFor(() =>
      expect(onSearchSuccess).toHaveBeenCalledWith(
        MOCK_RESULTS,
        expect.objectContaining({ catalogNumber: "CAT001" })
      )
    );
  });

  it("includes optional filter values in the search request", async () => {
    mockSearchDiscogs.mockResolvedValueOnce(MOCK_RESULTS);
    const user = userEvent.setup();
    const onSearchSuccess = jest.fn();
    render(
      <DiscogsSearchStep onSearchSuccess={onSearchSuccess} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    await user.type(screen.getByLabelText(/format/i), "Vinyl");
    await user.type(screen.getByLabelText(/country/i), "UK");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    await waitFor(() =>
      expect(onSearchSuccess).toHaveBeenCalledWith(
        MOCK_RESULTS,
        expect.objectContaining({ format: "Vinyl", country: "UK" })
      )
    );
  });

  it("shows 'Searching…' text while the request is in flight", async () => {
    mockSearchDiscogs.mockReturnValueOnce(new Promise(() => {}));
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    expect(await screen.findByText(/searching/i)).toBeInTheDocument();
  });
});

describe("DiscogsSearchStep – no results / errors", () => {
  it("calls onSearchError when the search returns no results", async () => {
    mockSearchDiscogs.mockResolvedValueOnce([]);
    const user = userEvent.setup();
    const onSearchError = jest.fn();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={onSearchError} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "NOTFOUND");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    await waitFor(() =>
      expect(onSearchError).toHaveBeenCalledWith(
        expect.stringMatching(/no results found/i)
      )
    );
  });

  it("shows an inline alert when the search returns no results", async () => {
    mockSearchDiscogs.mockResolvedValueOnce([]);
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "NOTFOUND");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    expect(await screen.findByRole("alert")).toBeInTheDocument();
  });

  it("calls onSearchError when the API throws", async () => {
    mockSearchDiscogs.mockRejectedValueOnce(new Error("Network error"));
    const user = userEvent.setup();
    const onSearchError = jest.fn();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={onSearchError} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    await waitFor(() =>
      expect(onSearchError).toHaveBeenCalledWith("Network error")
    );
  });
});

describe("DiscogsSearchStep – clear", () => {
  it("resets all fields when the Clear button is clicked", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep onSearchSuccess={noop} onSearchError={noop} />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "CAT001");
    await user.type(screen.getByLabelText(/format/i), "Vinyl");
    await user.click(screen.getByRole("button", { name: /clear/i }));
    expect(screen.getByLabelText(/catalogue number/i)).toHaveValue("");
    expect(screen.getByLabelText(/format/i)).toHaveValue("");
  });
});
