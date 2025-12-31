"use client";
import { useState } from "react";
import { deleteCollection, getCollectionCount, ApiError } from "../lib/api";
import { ConfirmDialog } from "./ConfirmDialog";

export interface DeleteCollectionButtonProps {
  onDeleteSuccess?: (deletedCount: number) => void;
  onDeleteError?: (error: ApiError) => void;
  className?: string;
}

/**
 * Delete Collection button component for removing all albums from the user's collection
 * Features:
 * - Fetches current album count before showing confirmation
 * - Two-step confirmation dialog with detailed message
 * - Loading states
 * - Error handling
 * - Accessible with keyboard navigation
 */
export function DeleteCollectionButton({
  onDeleteSuccess,
  onDeleteError,
  className = "",
}: DeleteCollectionButtonProps) {
  const [showConfirm, setShowConfirm] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isFetchingCount, setIsFetchingCount] = useState(false);
  const [albumCount, setAlbumCount] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);

  const handleDeleteClick = async () => {
    setError(null);
    setIsFetchingCount(true);

    try {
      // Fetch the current album count
      const count = await getCollectionCount();
      setAlbumCount(count);
      setShowConfirm(true);
    } catch (err) {
      const apiError = err as ApiError;
      let errorMessage = "Failed to fetch collection count. Please try again.";

      if (apiError.status === 401) {
        errorMessage = "You must be logged in to delete your collection.";
      } else if (apiError.message?.includes("timeout")) {
        errorMessage = "Request timed out. Please check your connection and try again.";
      }

      setError(errorMessage);

      if (onDeleteError) {
        onDeleteError(apiError);
      }
    } finally {
      setIsFetchingCount(false);
    }
  };

  const handleCancel = () => {
    setShowConfirm(false);
    setError(null);
  };

  const handleConfirmDelete = async () => {
    setIsDeleting(true);
    setError(null);

    try {
      const response = await deleteCollection();
      setShowConfirm(false);

      // Call success callback if provided
      if (onDeleteSuccess) {
        onDeleteSuccess(response.albumsDeleted);
      }
    } catch (err) {
      const apiError = err as ApiError;

      // Close dialog first, then show error
      setShowConfirm(false);

      // Set error message based on status
      let errorMessage = "Failed to delete collection. Please try again.";

      if (apiError.status === 401) {
        errorMessage = "You must be logged in to delete your collection.";
      } else if (apiError.status === 404) {
        errorMessage = "User not found. Please try logging in again.";
      } else if (apiError.status === 500) {
        errorMessage = "Server error. Please try again later.";
      } else if (apiError.message?.includes("timeout")) {
        errorMessage = "Request timed out. Please check your connection and try again.";
      }

      setError(errorMessage);

      // Call error callback if provided
      if (onDeleteError) {
        onDeleteError(apiError);
      }
    } finally {
      setIsDeleting(false);
    }
  };

  const confirmationMessage = albumCount === 0
    ? "Your collection is currently empty. Are you sure you want to proceed?"
    : `This will permanently delete all ${albumCount} album${albumCount !== 1 ? 's' : ''} from your collection. Your collection will be empty after this action. This cannot be undone.`;

  return (
    <>
      <button
        type="button"
        onClick={handleDeleteClick}
        disabled={isDeleting || isFetchingCount}
        className={`inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors ${className}`}
        aria-label="Delete entire collection"
        title={isDeleting ? "Deleting collection..." : isFetchingCount ? "Loading..." : "Delete entire collection"}
      >
        {/* Trash Icon */}
        <svg
          className={`w-5 h-5 mr-2 ${(isDeleting || isFetchingCount) ? 'animate-pulse' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
          />
        </svg>
        {isDeleting ? "Deleting..." : isFetchingCount ? "Loading..." : "Delete Collection"}
      </button>

      {/* Error message display */}
      {error && !showConfirm && (
        <div className="fixed bottom-4 right-4 max-w-md bg-red-50 border border-red-200 rounded-lg p-4 shadow-lg z-50">
          <div className="flex items-start gap-3">
            <svg
              className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5"
              fill="currentColor"
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
            <div className="flex-1">
              <h3 className="text-sm font-medium text-red-800">
                Delete Failed
              </h3>
              <p className="text-sm text-red-700 mt-1">{error}</p>
            </div>
            <button
              type="button"
              onClick={() => setError(null)}
              className="text-red-400 hover:text-red-600"
              aria-label="Dismiss error"
            >
              <svg
                className="w-5 h-5"
                fill="currentColor"
                viewBox="0 0 20 20"
                aria-hidden="true"
              >
                <path
                  fillRule="evenodd"
                  d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                  clipRule="evenodd"
                />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Confirmation Dialog */}
      <ConfirmDialog
        isOpen={showConfirm}
        title="Delete Entire Collection"
        message={confirmationMessage}
        confirmLabel={isDeleting ? "Deleting..." : "Delete Collection"}
        cancelLabel="Cancel"
        onConfirm={handleConfirmDelete}
        onCancel={handleCancel}
        isDangerous={true}
      />
    </>
  );
}
