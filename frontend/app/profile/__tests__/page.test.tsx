import React from "react";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import ProfilePage from "../page";
import { getCollectionCount } from "../../lib/api";

const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

jest.mock("../../lib/api", () => ({
  getCollectionCount: jest.fn(),
}));

jest.mock("../../components/ThemeSwitcher", () => ({
  __esModule: true,
  default: () => <div data-testid="theme-switcher">Theme Switcher</div>,
}));

jest.mock("../../components/DeleteCollectionButton", () => ({
  DeleteCollectionButton: ({
    onDeleteSuccess,
    disabled,
  }: {
    onDeleteSuccess?: (deletedCount: number) => void;
    disabled?: boolean;
  }) => (
    <button type="button" disabled={disabled} onClick={() => onDeleteSuccess?.(3)}>
      Delete Collection
    </button>
  ),
}));

jest.mock("../../components/DiscogsImportDialog", () => ({
  DiscogsImportDialog: ({
    isOpen,
    onClose,
    onSuccess,
  }: {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
  }) => {
    if (!isOpen) return null;
    return (
      <div data-testid="discogs-import-dialog">
        <button type="button" onClick={onSuccess}>Complete Import</button>
        <button type="button" onClick={onClose}>Close Import</button>
      </div>
    );
  },
}));

describe("ProfilePage", () => {
  const mockGetCollectionCount = getCollectionCount as jest.MockedFunction<typeof getCollectionCount>;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders import section above delete collection", async () => {
    mockGetCollectionCount.mockResolvedValue(0);

    render(<ProfilePage />);

    const importHeading = await screen.findByRole("heading", {
      name: "Import Collection From Discogs",
    });
    const deleteHeading = await screen.findByRole("heading", {
      name: "Delete Collection",
    });

    expect(importHeading).toBeInTheDocument();
    expect(deleteHeading).toBeInTheDocument();

    const position = importHeading.compareDocumentPosition(deleteHeading);
    expect(position & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("enables import button when collection is empty", async () => {
    mockGetCollectionCount.mockResolvedValue(0);

    render(<ProfilePage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeEnabled();
    });
    expect(screen.getByRole("button", { name: "Delete Collection" })).toBeDisabled();
  });

  it("disables import button and shows explanatory text when collection is not empty", async () => {
    mockGetCollectionCount.mockResolvedValue(5);

    render(<ProfilePage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeDisabled();
    });

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Delete Collection" })).toBeEnabled();
    });

    const importButton = screen.getByRole("button", {
      name: "Import collection from Discogs",
    });

    expect(importButton).toBeDisabled();
    expect(screen.getByRole("button", { name: "Delete Collection" })).toBeEnabled();
    expect(
      screen.getByText(
        /Import is available only when your collection is empty\. If you already have albums in your collection, you must delete your collection first before using this feature\./i
      )
    ).toBeInTheDocument();
  });

  it("opens import dialog when import is enabled and clicked", async () => {
    mockGetCollectionCount.mockResolvedValue(0);

    render(<ProfilePage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeEnabled();
    });

    const importButton = screen.getByRole("button", {
      name: "Import collection from Discogs",
    });
    fireEvent.click(importButton);

    expect(screen.getByTestId("discogs-import-dialog")).toBeInTheDocument();
  });

  it("refreshes count after successful import and disables import when collection becomes non-empty", async () => {
    const countSequence = [0, 4];
    let callIndex = 0;
    mockGetCollectionCount.mockImplementation(() => {
      const next = countSequence[Math.min(callIndex, countSequence.length - 1)];
      callIndex += 1;
      return Promise.resolve(next);
    });

    render(<ProfilePage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeEnabled();
    });

    const importButton = screen.getByRole("button", {
      name: "Import collection from Discogs",
    });

    fireEvent.click(importButton);
    fireEvent.click(screen.getByRole("button", { name: "Complete Import" }));

    await waitFor(() => {
      expect(
        screen.getByText("Successfully imported your Discogs collection.")
      ).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeDisabled();
    });

    expect(mockPush).toHaveBeenCalledWith("/");
  });

  it("refreshes count after delete success and enables import when collection becomes empty", async () => {
    const countSequence = [7, 0];
    let callIndex = 0;
    mockGetCollectionCount.mockImplementation(() => {
      const next = countSequence[Math.min(callIndex, countSequence.length - 1)];
      callIndex += 1;
      return Promise.resolve(next);
    });

    render(<ProfilePage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeDisabled();
    });

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Delete Collection" })).toBeEnabled();
    });

    fireEvent.click(screen.getByRole("button", { name: "Delete Collection" }));

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Import collection from Discogs" })
      ).toBeEnabled();
    });
    expect(screen.getByRole("button", { name: "Delete Collection" })).toBeDisabled();
  });
});
