import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { DiscogsImportDialog } from "../DiscogsImportDialog";
import { fetchJson } from "../../lib/api";

jest.mock("../../lib/api", () => ({
  fetchJson: jest.fn(),
}));

describe("DiscogsImportDialog", () => {
  const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  it("renders spinner with discogs logo and three step progress panels", async () => {
    let isCompleted = false;

    mockFetchJson.mockImplementation((url, options) => {
      if (url === "/api/import/discogs" && options?.method === "POST") {
        return Promise.resolve({
          jobId: "job-1",
          status: "Queued",
        }) as Promise<any>;
      }

      if (url === "/api/import/discogs/status?jobId=job-1") {
        return Promise.resolve({
          jobId: "job-1",
          status: isCompleted ? "Succeeded" : "Running",
          totalReleases: 20,
          effectiveTotal: 20,
          imported: isCompleted ? 20 : 3,
          skipped: 0,
          failed: 0,
          completed: isCompleted,
          success: isCompleted,
          errors: [],
          duration: "00:01:00",
        }) as Promise<any>;
      }

      throw new Error(`Unexpected fetchJson call: ${url}`);
    });

    render(
      <DiscogsImportDialog
        isOpen={true}
        onClose={jest.fn()}
        onSuccess={jest.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText("Discogs Username"), {
      target: { value: "test-user" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Import Collection" }));

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    const spinner = await screen.findByTestId("discogs-import-progress-spinner");
    expect(spinner).toHaveAttribute("aria-valuenow", "15");
    expect(screen.getByTestId("discogs-import-logo")).toBeInTheDocument();
    expect(screen.queryByText("15%")).not.toBeInTheDocument();

    expect(screen.getByTestId("discogs-import-step-title-1")).toHaveTextContent("Step 1/3: Importing releases");
    expect(screen.getByTestId("discogs-import-step-title-2")).toHaveTextContent("Step 2/3: Enriching tracklists");
    expect(screen.getByTestId("discogs-import-step-title-3")).toHaveTextContent("Step 3/3: Mirroring cover art");
    expect(screen.getByTestId("discogs-import-current-status-text")).toHaveTextContent(/Crate-digging through Discogs shelves|Lining up the next stack of records|Cueing side B and syncing metadata|Nudging the pitch while indexes settle/);
    expect(screen.getByTestId("discogs-import-step-count-1")).toHaveTextContent("3/20");
    expect(screen.getByTestId("discogs-import-step-count-2")).toHaveTextContent("0/3");
    expect(screen.getByTestId("discogs-import-step-count-3")).toHaveTextContent("0/3");
    expect(screen.getByTestId("discogs-import-step-pending-1")).toBeInTheDocument();
    expect(screen.queryByTestId("discogs-import-step-complete-1")).not.toBeInTheDocument();

    isCompleted = true;

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    await waitFor(() => {
      expect(screen.getByText("Import Successful!")).toBeInTheDocument();
    });
  });

  it("shows per-step counters and tick marks as phases complete", async () => {
    let isCompleted = false;

    mockFetchJson.mockImplementation((url, options) => {
      if (url === "/api/import/discogs" && options?.method === "POST") {
        return Promise.resolve({
          jobId: "job-2",
          status: "Queued",
        }) as Promise<any>;
      }

      if (url === "/api/import/discogs/status?jobId=job-2") {
        return Promise.resolve({
          jobId: "job-2",
          status: isCompleted ? "Succeeded" : "Running",
          totalReleases: 10,
          effectiveTotal: 10,
          imported: 10,
          skipped: 0,
          failed: 0,
          percentage: isCompleted ? 100 : 94,
          completed: isCompleted,
          success: isCompleted,
          errors: [],
          duration: "00:00:15",
        }) as Promise<any>;
      }

      throw new Error(`Unexpected fetchJson call: ${url}`);
    });

    render(
      <DiscogsImportDialog
        isOpen={true}
        onClose={jest.fn()}
        onSuccess={jest.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText("Discogs Username"), {
      target: { value: "test-user" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Import Collection" }));

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    const spinner = await screen.findByTestId("discogs-import-progress-spinner");
    expect(spinner).toHaveAttribute("aria-valuenow", "94");
    expect(screen.queryByText("94%")).not.toBeInTheDocument();
    expect(screen.getByTestId("discogs-import-step-count-1")).toHaveTextContent("10/10");
    expect(screen.getByTestId("discogs-import-step-complete-1")).toBeInTheDocument();
    expect(screen.getByTestId("discogs-import-step-count-2")).toHaveTextContent("9/10");
    expect(screen.getByTestId("discogs-import-step-pending-2")).toBeInTheDocument();
    expect(screen.getByTestId("discogs-import-step-count-3")).toHaveTextContent("0/10");

    isCompleted = true;

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    await waitFor(() => {
      expect(screen.getByText("Import Successful!")).toBeInTheDocument();
    });
  });

  it("shows cooldown overlay with countdown timer when API rate limit is active", async () => {
    // Set cooldown to 30 seconds from now
    const cooldownUntil = new Date(Date.now() + 30000).toISOString();

    mockFetchJson.mockImplementation((url, options) => {
      if (url === "/api/import/discogs" && options?.method === "POST") {
        return Promise.resolve({ jobId: "job-3", status: "Queued" }) as Promise<any>;
      }
      if (url === "/api/import/discogs/status?jobId=job-3") {
        return Promise.resolve({
          jobId: "job-3",
          status: "Running",
          totalReleases: 20,
          effectiveTotal: 20,
          imported: 5,
          skipped: 0,
          failed: 0,
          percentage: 18,
          completed: false,
          success: false,
          errors: [],
          cooldownUntilUtc: cooldownUntil,
        }) as Promise<any>;
      }
      throw new Error(`Unexpected fetchJson call: ${url}`);
    });

    render(
      <DiscogsImportDialog
        isOpen={true}
        onClose={jest.fn()}
        onSuccess={jest.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText("Discogs Username"), {
      target: { value: "test-user" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Import Collection" }));

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    // Wait for cooldown panel to appear
    await waitFor(() => {
      expect(screen.getByTestId("discogs-import-cooldown")).toBeInTheDocument();
    });

    expect(screen.getByTestId("discogs-import-cooldown-timer")).toBeInTheDocument();
    expect(screen.getByText(/API Rate Limit/i)).toBeInTheDocument();
    expect(screen.getByText(/Resuming automatically/i)).toBeInTheDocument();
  });
});
