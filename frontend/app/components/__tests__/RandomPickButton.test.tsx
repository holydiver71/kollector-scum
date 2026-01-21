import React from "react";
import { render, screen, waitFor } from "@testing-library/react";
import { RandomPickButton } from "../RandomPickButton";
import * as api from "../../lib/api";

// Mock the API module
jest.mock("../../lib/api", () => ({
  getRandomReleaseId: jest.fn(),
}));

// Mock next/navigation
const mockPush = jest.fn();
jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

describe("RandomPickButton Component", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders the button", () => {
    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });
    expect(button).toBeInTheDocument();
  });

  it("has proper accessibility attributes", () => {
    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });
    expect(button).toHaveAttribute("aria-label", "View random release");
    expect(button).toHaveAttribute("title", "View random release");
  });

  it("navigates to random release on click", async () => {
    (api.getRandomReleaseId as jest.Mock).mockResolvedValue(42);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    fireEvent.click(button);

    await waitFor(() => {
      expect(api.getRandomReleaseId).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith("/releases/42");
    });
  });

  it("shows loading state while fetching", async () => {
    // Create a promise we can control
    let resolvePromise: (value: number) => void;
    const controlledPromise = new Promise<number>((resolve) => {
      resolvePromise = resolve;
    });
    (api.getRandomReleaseId as jest.Mock).mockReturnValue(controlledPromise);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    fireEvent.click(button);

    // Button should be disabled while loading
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("title", "Loading...");

    // Resolve the promise
    resolvePromise!(123);

    await waitFor(() => {
      expect(button).not.toBeDisabled();
    });
  });

  it("displays error message when API call fails", async () => {
    const apiError = new Error("Server error") as api.ApiError;
    apiError.status = 500;
    (api.getRandomReleaseId as jest.Mock).mockRejectedValue(apiError);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    fireEvent.click(button);

    await waitFor(() => {
      expect(
        screen.getByText("Server error. Please try again later.")
      ).toBeInTheDocument();
    });
  });

  it("displays specific error for empty collection", async () => {
    const apiError = new Error("Not found") as api.ApiError;
    apiError.status = 404;
    (api.getRandomReleaseId as jest.Mock).mockRejectedValue(apiError);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    fireEvent.click(button);

    await waitFor(() => {
      expect(
        screen.getByText("No releases in collection yet.")
      ).toBeInTheDocument();
    });
  });

  it("allows dismissing error message", async () => {
    const apiError = new Error("Error") as api.ApiError;
    apiError.status = 500;
    (api.getRandomReleaseId as jest.Mock).mockRejectedValue(apiError);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    fireEvent.click(button);

    await waitFor(() => {
      expect(
        screen.getByText("Server error. Please try again later.")
      ).toBeInTheDocument();
    });

    // Click dismiss button
    const dismissButton = screen.getByRole("button", { name: /dismiss error/i });
    fireEvent.click(dismissButton);

    expect(
      screen.queryByText("Server error. Please try again later.")
    ).not.toBeInTheDocument();
  });

  it("applies custom className", () => {
    render(<RandomPickButton className="custom-class" />);
    const button = screen.getByRole("button", { name: /view random release/i });
    expect(button).toHaveClass("custom-class");
  });

  it("prevents multiple clicks while loading", async () => {
    let resolvePromise: (value: number) => void;
    const controlledPromise = new Promise<number>((resolve) => {
      resolvePromise = resolve;
    });
    (api.getRandomReleaseId as jest.Mock).mockReturnValue(controlledPromise);

    render(<RandomPickButton />);
    const button = screen.getByRole("button", { name: /view random release/i });

    // Click multiple times
    fireEvent.click(button);
    fireEvent.click(button);
    fireEvent.click(button);

    // Should only be called once
    expect(api.getRandomReleaseId).toHaveBeenCalledTimes(1);

    // Resolve and cleanup
    resolvePromise!(123);
    await waitFor(() => {
      expect(button).not.toBeDisabled();
    });
  });
});
