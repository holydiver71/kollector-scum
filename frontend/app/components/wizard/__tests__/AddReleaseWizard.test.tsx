/**
 * Integration tests for the AddReleaseWizard component.
 *
 * Panels are mocked to focus tests on wizard-shell behaviour: navigation,
 * validation, loading states, and submit handling. The actual content of each
 * panel is exercised by panel-level tests.
 */
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AddReleaseWizard from "../AddReleaseWizard";
import { fetchJson } from "../../../lib/api";

// ── Lookup hook mock ─────────────────────────────────────────────────────────

const mockUseReleaseLookups = jest.fn();
jest.mock("../useReleaseLookups", () => ({
  useReleaseLookups: () => mockUseReleaseLookups(),
}));

// ── API mock ─────────────────────────────────────────────────────────────────

jest.mock("../../../lib/api", () => ({
  fetchJson: jest.fn(),
}));
const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

// ── Panel mocks ──────────────────────────────────────────────────────────────

jest.mock("../panels/BasicInformationPanel", () => ({
  __esModule: true,
  default: function MockBasicInfo({ onChange, errors }: {
    onChange: (updates: Record<string, unknown>) => void;
    errors?: Record<string, string>;
  }) {
    return (
      <div data-testid="basic-panel">
        {errors?.title && (
          <p role="alert" data-testid="title-error">
            {errors.title}
          </p>
        )}
        {errors?.artists && (
          <p role="alert" data-testid="artists-error">
            {errors.artists}
          </p>
        )}
        <button
          data-testid="fill-basic"
          onClick={() =>
            onChange({
              title: "My Release",
              artistIds: [1],
              artistNames: ["Artist A"],
            })
          }
        >
          Fill
        </button>
      </div>
    );
  },
}));

jest.mock("../panels/ClassificationPanel", () => ({
  __esModule: true,
  default: function MockClassification() {
    return <div data-testid="classification-panel" />;
  },
}));

jest.mock("../panels/LabelInformationPanel", () => ({
  __esModule: true,
  default: function MockLabel() {
    return <div data-testid="label-panel" />;
  },
}));

jest.mock("../panels/PurchaseInformationPanel", () => ({
  __esModule: true,
  default: function MockPurchase() {
    return <div data-testid="purchase-panel" />;
  },
}));

jest.mock("../panels/ImagesPanel", () => ({
  __esModule: true,
  default: function MockImages() {
    return <div data-testid="images-panel" />;
  },
}));

jest.mock("../panels/TrackListingPanel", () => ({
  __esModule: true,
  default: function MockTracks() {
    return <div data-testid="tracks-panel" />;
  },
}));

jest.mock("../panels/ExternalLinksPanel", () => ({
  __esModule: true,
  default: function MockLinks() {
    return <div data-testid="links-panel" />;
  },
}));

jest.mock("../panels/DraftPreviewPanel", () => ({
  __esModule: true,
  default: function MockDraftPreview({
    onGoBack,
    onSubmit,
    isSubmitting,
    submitError,
  }: {
    onGoBack: () => void;
    onSubmit: () => void;
    isSubmitting?: boolean;
    submitError?: string | null;
  }) {
    return (
      <div data-testid="draft-preview">
        {submitError && (
          <p role="alert" data-testid="submit-error">
            {submitError}
          </p>
        )}
        <button data-testid="go-back-btn" onClick={onGoBack}>
          Back
        </button>
        <button
          data-testid="save-btn"
          onClick={onSubmit}
          disabled={!!isSubmitting}
        >
          {isSubmitting ? "Saving..." : "Save Release"}
        </button>
      </div>
    );
  },
}));

// ── Fixtures ─────────────────────────────────────────────────────────────────

const LOADED_LOOKUPS = {
  loading: false,
  error: null,
  artists: [],
  labels: [],
  genres: [],
  countries: [],
  formats: [],
  packagings: [],
  stores: [],
};

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Click the Next / Preview Release button in the wizard footer */
async function clickNext(user: ReturnType<typeof userEvent.setup>) {
  await user.click(
    screen.getByRole("button", { name: /next|preview release/i })
  );
}

/** Fill step 0 required fields, then advance to step 1 */
async function fillAndAdvanceFromStep0(
  user: ReturnType<typeof userEvent.setup>
) {
  await user.click(screen.getByTestId("fill-basic"));
  await clickNext(user);
}

/** Advance through all optional steps (1–6) and land on Draft Preview */
async function navigateToPreview(
  user: ReturnType<typeof userEvent.setup>
) {
  await fillAndAdvanceFromStep0(user);
  for (let i = 1; i <= 5; i++) {
    await clickNext(user);
  }
  // Step 6's "Next" button is labelled "Preview Release"
  await user.click(screen.getByRole("button", { name: /preview release/i }));
}

// ── Test suite ────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.clearAllMocks();
  mockUseReleaseLookups.mockReturnValue(LOADED_LOOKUPS);
  // Suppress scrollTo errors in jsdom
  window.scrollTo = jest.fn() as typeof window.scrollTo;
});

describe("AddReleaseWizard – loading state", () => {
  it("displays a loading spinner while lookups are in flight", () => {
    mockUseReleaseLookups.mockReturnValue({ ...LOADED_LOOKUPS, loading: true });
    render(<AddReleaseWizard />);
    expect(screen.getByText("Loading release data…")).toBeInTheDocument();
    expect(screen.queryByTestId("basic-panel")).not.toBeInTheDocument();
  });

  it("shows the Basic Information panel once lookups have loaded", () => {
    render(<AddReleaseWizard />);
    expect(screen.getByTestId("basic-panel")).toBeInTheDocument();
    expect(screen.queryByText("Loading release data…")).not.toBeInTheDocument();
  });

  it("shows a non-blocking warning when lookup data failed to load", () => {
    mockUseReleaseLookups.mockReturnValue({
      ...LOADED_LOOKUPS,
      error: "Failed to load",
    });
    render(<AddReleaseWizard />);
    expect(
      screen.getByText(/autocomplete options may be limited/i)
    ).toBeInTheDocument();
    // Wizard is still usable
    expect(screen.getByTestId("basic-panel")).toBeInTheDocument();
  });
});

describe("AddReleaseWizard – step indicators", () => {
  it("shows Step 1 of 8 and the Basic Information heading on initial render", () => {
    render(<AddReleaseWizard />);
    expect(screen.getByText("Step 1 of 8")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Basic Information" })).toBeInTheDocument();
  });

  it("advances the step counter when navigating forward", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await fillAndAdvanceFromStep0(user);
    expect(screen.getByText("Step 2 of 8")).toBeInTheDocument();
  });
});

describe("AddReleaseWizard – step 0 validation", () => {
  it("shows title and artists errors when Next is clicked with empty form", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await clickNext(user);
    expect(await screen.findByTestId("title-error")).toBeInTheDocument();
    expect(screen.getByTestId("artists-error")).toBeInTheDocument();
  });

  it("shows the validation summary banner when there are errors", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await clickNext(user);
    expect(
      await screen.findByText(/please fix the highlighted fields/i)
    ).toBeInTheDocument();
  });

  it("does not advance to step 2 when validation fails", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await clickNext(user);
    expect(screen.queryByTestId("classification-panel")).not.toBeInTheDocument();
    expect(screen.getByTestId("basic-panel")).toBeInTheDocument();
  });

  it("advances to step 2 after required fields are filled", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await fillAndAdvanceFromStep0(user);
    expect(screen.getByTestId("classification-panel")).toBeInTheDocument();
    expect(screen.getByText("Step 2 of 8")).toBeInTheDocument();
  });
});

describe("AddReleaseWizard – navigation", () => {
  it("navigates back to step 1 from step 2 using the Previous button", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await fillAndAdvanceFromStep0(user);
    await user.click(screen.getByRole("button", { name: /previous/i }));
    expect(screen.getByTestId("basic-panel")).toBeInTheDocument();
    expect(screen.getByText("Step 1 of 8")).toBeInTheDocument();
  });

  it("calls onCancel when the Cancel button is clicked on step 0", async () => {
    const onCancel = jest.fn();
    const user = userEvent.setup();
    render(<AddReleaseWizard onCancel={onCancel} />);
    await user.click(screen.getByRole("button", { name: /cancel/i }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it("shows 'Cancel' on step 0 and 'Previous' on subsequent steps", async () => {
    const onCancel = jest.fn();
    const user = userEvent.setup();
    render(<AddReleaseWizard onCancel={onCancel} />);
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
    await fillAndAdvanceFromStep0(user);
    expect(screen.getByRole("button", { name: /previous/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /cancel/i })).not.toBeInTheDocument();
  });

  it("reaches the Draft Preview panel after navigating through all 7 steps", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    expect(screen.getByTestId("draft-preview")).toBeInTheDocument();
  });

  it("hides the standard footer on the Draft Preview step", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    // No Next/Previous buttons in the wizard footer on preview
    expect(screen.queryByRole("button", { name: /^next$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^previous$/i })).not.toBeInTheDocument();
  });
});

describe("AddReleaseWizard – preview panel go-back", () => {
  it("navigates back to step 6 when onGoBack is triggered from Draft Preview", async () => {
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    await user.click(screen.getByTestId("go-back-btn"));
    expect(screen.getByTestId("links-panel")).toBeInTheDocument();
    expect(screen.getByText("Step 7 of 8")).toBeInTheDocument();
  });
});

describe("AddReleaseWizard – submission", () => {
  it("calls POST /api/musicreleases when Save Release is clicked", async () => {
    mockFetchJson.mockResolvedValueOnce({ release: { id: 42 } });
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    await user.click(screen.getByTestId("save-btn"));
    await waitFor(() => {
      expect(mockFetchJson).toHaveBeenCalledWith(
        "/api/musicreleases",
        expect.objectContaining({ method: "POST" })
      );
    });
  });

  it("calls onSuccess with the release ID after a successful POST", async () => {
    const onSuccess = jest.fn();
    mockFetchJson.mockResolvedValueOnce({ release: { id: 42 } });
    const user = userEvent.setup();
    render(<AddReleaseWizard onSuccess={onSuccess} />);
    await navigateToPreview(user);
    await user.click(screen.getByTestId("save-btn"));
    await waitFor(() => expect(onSuccess).toHaveBeenCalledWith(42));
  });

  it("shows a submit error when the POST request throws", async () => {
    mockFetchJson.mockRejectedValueOnce(new Error("Server exploded"));
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    await user.click(screen.getByTestId("save-btn"));
    await waitFor(() =>
      expect(screen.getByTestId("submit-error")).toBeInTheDocument()
    );
    expect(screen.getByTestId("submit-error")).toHaveTextContent(
      /Server exploded/i
    );
  });

  it("shows a submit error when the POST response contains no release ID", async () => {
    mockFetchJson.mockResolvedValueOnce({ release: {} });
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    await user.click(screen.getByTestId("save-btn"));
    await waitFor(() =>
      expect(screen.getByTestId("submit-error")).toBeInTheDocument()
    );
  });

  it("disables the Save button while submission is in progress", async () => {
    // Use a never-resolving promise to keep isSubmitting true indefinitely
    mockFetchJson.mockReturnValueOnce(new Promise(() => {}));
    const user = userEvent.setup();
    render(<AddReleaseWizard />);
    await navigateToPreview(user);
    const saveBtn = screen.getByTestId("save-btn");
    await user.click(saveBtn);
    expect(saveBtn).toBeDisabled();
  });
});
