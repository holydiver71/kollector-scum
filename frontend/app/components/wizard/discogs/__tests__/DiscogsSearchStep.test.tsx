/**
 * Tests for DiscogsSearchStep – step 1 of the Discogs wizard.
 *
 * Covers field-level validation and the success / error callbacks.
 * The searchDiscogs API call is mocked so no network I/O occurs.
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DiscogsSearchStep from "../DiscogsSearchStep";

// ── Mock api ──────────────────────────────────────────────────────────────────

jest.mock("../../../../lib/api", () => ({
  searchDiscogs: jest.fn(),
}));

import { searchDiscogs } from "../../../../lib/api";
const mockSearchDiscogs = searchDiscogs as jest.MockedFunction<typeof searchDiscogs>;

// ─── Helpers ──────────────────────────────────────────────────────────────────

const defaultProps = {
  onSearchSuccess: jest.fn(),
  onSearchError: jest.fn(),
};

function setup(props = defaultProps) {
  const user = userEvent.setup();
  const view = render(<DiscogsSearchStep {...props} />);
  return { user, ...view };
}

// ─── Tests ────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
});

describe("DiscogsSearchStep – rendering", () => {
  it("renders the catalogue number input", () => {
    setup();
    expect(screen.getByLabelText(/catalogue number/i)).toBeInTheDocument();
  });

  it("renders optional filter fields", () => {
    setup();
    expect(screen.getByLabelText(/format/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/country/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/year/i)).toBeInTheDocument();
  });

  it("renders the Search Discogs button", () => {
    setup();
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).toBeInTheDocument();
  });

  it("renders the Clear button", () => {
    setup();
    expect(screen.getByRole("button", { name: /clear/i })).toBeInTheDocument();
  });

  it("shows manual entry link when onSwitchToManual is provided", () => {
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={jest.fn()}
        onSwitchToManual={jest.fn()}
      />
    );
    expect(screen.getByText(/switch to manual entry/i)).toBeInTheDocument();
  });

  it("does not show manual entry link when onSwitchToManual is not provided", () => {
    setup();
    expect(screen.queryByText(/switch to manual entry/i)).not.toBeInTheDocument();
  });

  it("pre-populates fields from initialValues", () => {
    render(
      <DiscogsSearchStep
        initialValues={{ catalogNumber: "ABC-123", format: "Vinyl", country: "UK", year: 1985 }}
        onSearchSuccess={jest.fn()}
        onSearchError={jest.fn()}
      />
    );
    expect(screen.getByLabelText(/catalogue number/i)).toHaveValue("ABC-123");
    expect(screen.getByLabelText(/format/i)).toHaveValue("Vinyl");
    expect(screen.getByLabelText(/country/i)).toHaveValue("UK");
    expect(screen.getByLabelText(/year/i)).toHaveValue(1985);
  });
});

describe("DiscogsSearchStep – validation", () => {
  it("disables the submit button when catalogue number is empty", () => {
    setup();
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).toBeDisabled();
  });

  it("enables the submit button when catalogue number is typed", async () => {
    const { user } = setup();
    await user.type(screen.getByLabelText(/catalogue number/i), "ABC-123");
    expect(
      screen.getByRole("button", { name: /search discogs/i })
    ).not.toBeDisabled();
  });

  it("shows a year validation error for an out-of-range year", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={jest.fn()}
      />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "XYZ-001");
    await user.clear(screen.getByLabelText(/year/i));
    await user.type(screen.getByLabelText(/year/i), "1800");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    expect(
      await screen.findByText(/year must be between/i)
    ).toBeInTheDocument();
  });

  it("clears the year error once a valid year is typed", async () => {
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={jest.fn()}
      />
    );
    await user.type(screen.getByLabelText(/catalogue number/i), "ABC-001");
    await user.type(screen.getByLabelText(/year/i), "1800");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    await screen.findByText(/year must be between/i);
    await user.clear(screen.getByLabelText(/year/i));
    await user.type(screen.getByLabelText(/year/i), "1969");
    await waitFor(() =>
      expect(screen.queryByText(/year must be between/i)).not.toBeInTheDocument()
    );
  });
});

describe("DiscogsSearchStep – successful search", () => {
  const mockResults = [
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
  ];

  it("calls onSearchSuccess with results and request after a successful search", async () => {
    mockSearchDiscogs.mockResolvedValueOnce(mockResults);
    const onSearchSuccess = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={onSearchSuccess}
        onSearchError={jest.fn()}
      />
    );

    await user.type(screen.getByLabelText(/catalogue number/i), "PCS-7088");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));

    await waitFor(() => {
      expect(onSearchSuccess).toHaveBeenCalledWith(
        mockResults,
        expect.objectContaining({ catalogNumber: "PCS-7088" })
      );
    });
  });

  it("shows 'Searching…' while the request is in flight", async () => {
    mockSearchDiscogs.mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve([]), 500))
    );
    const { user } = setup();
    await user.type(screen.getByLabelText(/catalogue number/i), "PCS-7088");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));
    expect(screen.getByText(/searching/i)).toBeInTheDocument();
  });

  it("includes optional filters in the search request when provided", async () => {
    mockSearchDiscogs.mockResolvedValueOnce(mockResults);
    const onSearchSuccess = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={onSearchSuccess}
        onSearchError={jest.fn()}
      />
    );

    await user.type(screen.getByLabelText(/catalogue number/i), "PCS-7088");
    await user.type(screen.getByLabelText(/format/i), "Vinyl");
    await user.type(screen.getByLabelText(/country/i), "UK");
    await user.clear(screen.getByLabelText(/year/i));
    await user.type(screen.getByLabelText(/year/i), "1969");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));

    await waitFor(() => {
      expect(onSearchSuccess).toHaveBeenCalledWith(
        mockResults,
        expect.objectContaining({
          catalogNumber: "PCS-7088",
          format: "Vinyl",
          country: "UK",
          year: 1969,
        })
      );
    });
  });
});

describe("DiscogsSearchStep – error handling", () => {
  it("calls onSearchError when no results are found", async () => {
    mockSearchDiscogs.mockResolvedValueOnce([]);
    const onSearchError = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={onSearchError}
      />
    );

    await user.type(screen.getByLabelText(/catalogue number/i), "NOTFOUND-001");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));

    await waitFor(() => {
      expect(onSearchError).toHaveBeenCalledWith(
        expect.stringContaining("NOTFOUND-001")
      );
    });
  });

  it("calls onSearchError when the API throws", async () => {
    mockSearchDiscogs.mockRejectedValueOnce(new Error("Network failure"));
    const onSearchError = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={onSearchError}
      />
    );

    await user.type(screen.getByLabelText(/catalogue number/i), "ABC-001");
    await user.click(screen.getByRole("button", { name: /search discogs/i }));

    await waitFor(() => {
      expect(onSearchError).toHaveBeenCalledWith("Network failure");
    });
  });
});

describe("DiscogsSearchStep – clear button", () => {
  it("clears all fields when Clear is clicked", async () => {
    const { user } = setup();
    await user.type(screen.getByLabelText(/catalogue number/i), "ABC-123");
    await user.type(screen.getByLabelText(/format/i), "Vinyl");
    await user.click(screen.getByRole("button", { name: /clear/i }));

    expect(screen.getByLabelText(/catalogue number/i)).toHaveValue("");
    expect(screen.getByLabelText(/format/i)).toHaveValue("");
  });
});

describe("DiscogsSearchStep – manual entry link", () => {
  it("calls onSwitchToManual when the link is clicked", async () => {
    const onSwitchToManual = jest.fn();
    const user = userEvent.setup();
    render(
      <DiscogsSearchStep
        onSearchSuccess={jest.fn()}
        onSearchError={jest.fn()}
        onSwitchToManual={onSwitchToManual}
      />
    );
    await user.click(screen.getByText(/switch to manual entry/i));
    expect(onSwitchToManual).toHaveBeenCalled();
  });
});
