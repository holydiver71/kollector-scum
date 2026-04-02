import React from "react";
import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
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

  it("renders a theme-aware segmented progress wheel while importing", async () => {
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

    const wheel = await screen.findByTestId("discogs-import-progress-wheel");
    expect(wheel).toHaveAttribute("aria-valuenow", "15");

    const segments = within(wheel).getAllByTestId("discogs-import-progress-segment");
    expect(segments).toHaveLength(32);
    expect(segments.filter((segment) => segment.getAttribute("data-active") === "true")).toHaveLength(5);
    expect(segments[0].getAttribute("style")).toContain("var(--theme-accent)");
    expect(segments[5].getAttribute("style")).toContain("var(--theme-card-border)");

    isCompleted = true;

    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    await waitFor(() => {
      expect(screen.getByText("Import Successful!")).toBeInTheDocument();
    });
  });
});
