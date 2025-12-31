import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { DeleteCollectionButton } from "../DeleteCollectionButton";
import * as api from "../../lib/api";

// Mock the API module
jest.mock("../../lib/api");

describe("DeleteCollectionButton", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders the delete collection button", () => {
    render(<DeleteCollectionButton />);
    
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    expect(button).toBeInTheDocument();
  });

  it("fetches collection count and shows confirmation dialog when clicked", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockResolvedValue(10);
    
    render(<DeleteCollectionButton />);
    
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    await waitFor(() => {
      expect(mockGetCollectionCount).toHaveBeenCalledTimes(1);
    });

    // Check if confirmation dialog appears with album count
    await waitFor(() => {
      expect(screen.getByText(/10 albums/i)).toBeInTheDocument();
      expect(screen.getByText(/this will permanently delete/i)).toBeInTheDocument();
    });
  });

  it("shows correct message for empty collection", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockResolvedValue(0);
    
    render(<DeleteCollectionButton />);
    
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    await waitFor(() => {
      expect(mockGetCollectionCount).toHaveBeenCalledTimes(1);
    });

    // Check if confirmation dialog shows empty collection message
    await waitFor(() => {
      expect(screen.getByText(/your collection is currently empty/i)).toBeInTheDocument();
    });
  });

  it("calls deleteCollection and onDeleteSuccess when confirmed", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockResolvedValue(5);
    const mockDeleteCollection = jest.spyOn(api, "deleteCollection").mockResolvedValue({
      albumsDeleted: 5,
      success: true,
      message: "Successfully deleted 5 albums",
    });
    const onDeleteSuccess = jest.fn();
    
    render(<DeleteCollectionButton onDeleteSuccess={onDeleteSuccess} />);
    
    // Click the button
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    // Wait for dialog to appear
    await waitFor(() => {
      expect(screen.getByText(/5 albums/i)).toBeInTheDocument();
    });

    // Click the confirm button
    const confirmButton = screen.getByRole("button", { name: /delete collection/i });
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteCollection).toHaveBeenCalledTimes(1);
      expect(onDeleteSuccess).toHaveBeenCalledWith(5);
    });
  });

  it("handles errors when fetching collection count", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockRejectedValue({
      status: 500,
      message: "Server error",
    });
    
    render(<DeleteCollectionButton />);
    
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    await waitFor(() => {
      expect(mockGetCollectionCount).toHaveBeenCalledTimes(1);
    });

    // Check if error message is displayed
    await waitFor(() => {
      expect(screen.getByText(/failed to fetch collection count/i)).toBeInTheDocument();
    });
  });

  it("handles errors when deleting collection", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockResolvedValue(5);
    const mockDeleteCollection = jest.spyOn(api, "deleteCollection").mockRejectedValue({
      status: 500,
      message: "Server error",
    });
    
    render(<DeleteCollectionButton />);
    
    // Click the button
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    // Wait for dialog to appear
    await waitFor(() => {
      expect(screen.getByText(/5 albums/i)).toBeInTheDocument();
    });

    // Click the confirm button
    const confirmButton = screen.getByRole("button", { name: /delete collection/i });
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteCollection).toHaveBeenCalledTimes(1);
    });

    // Check if error message is displayed
    await waitFor(() => {
      expect(screen.getByText(/server error/i)).toBeInTheDocument();
    });
  });

  it("can cancel the delete action", async () => {
    const mockGetCollectionCount = jest.spyOn(api, "getCollectionCount").mockResolvedValue(5);
    const mockDeleteCollection = jest.spyOn(api, "deleteCollection").mockResolvedValue({
      albumsDeleted: 5,
      success: true,
      message: "Successfully deleted 5 albums",
    });
    
    render(<DeleteCollectionButton />);
    
    // Click the button
    const button = screen.getByRole("button", { name: /delete entire collection/i });
    fireEvent.click(button);

    // Wait for dialog to appear
    await waitFor(() => {
      expect(screen.getByText(/5 albums/i)).toBeInTheDocument();
    });

    // Click the cancel button
    const cancelButton = screen.getByRole("button", { name: /cancel/i });
    fireEvent.click(cancelButton);

    // Ensure deleteCollection was not called
    expect(mockDeleteCollection).not.toHaveBeenCalled();

    // Dialog should be closed
    await waitFor(() => {
      expect(screen.queryByText(/5 albums/i)).not.toBeInTheDocument();
    });
  });
});
