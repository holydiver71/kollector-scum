"use client";

import { useEffect, useRef, useState } from "react";
import { getUserCollectionCount, deleteUserCollection } from "../lib/admin";

export interface DeleteUserCollectionDialogProps {
  /** Whether the dialog is visible */
  isOpen: boolean;
  /** ID of the deactivated user whose collection should be deleted */
  userId: string;
  /** Display name or email of the user, shown before the count is loaded */
  userEmail: string;
  /** Called when the admin confirms and deletion completes successfully */
  onDeleted: (deletedCount: number) => void;
  /** Called when the admin cancels or closes the dialog */
  onCancel: () => void;
}

/**
 * Confirmation dialog for permanently deleting a deactivated user's collection.
 * Fetches the release count from the API when opened so the admin can see
 * exactly how many items will be destroyed before confirming.
 */
export function DeleteUserCollectionDialog({
  isOpen,
  userId,
  userEmail,
  onDeleted,
  onCancel,
}: DeleteUserCollectionDialogProps) {
  const cancelButtonRef = useRef<HTMLButtonElement>(null);

  const [isLoadingCount, setIsLoadingCount] = useState(false);
  const [releaseCount, setReleaseCount] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  // Fetch release count whenever the dialog opens
  useEffect(() => {
    if (!isOpen) return;

    setReleaseCount(null);
    setFetchError(null);
    setDeleteError(null);
    setIsDeleting(false);
    setIsLoadingCount(true);

    getUserCollectionCount(userId)
      .then((data) => setReleaseCount(data.count))
      .catch(() => setFetchError("Could not load collection count. You can still proceed with deletion."))
      .finally(() => setIsLoadingCount(false));
  }, [isOpen, userId]);

  // Focus cancel button when dialog opens
  useEffect(() => {
    if (isOpen && cancelButtonRef.current) {
      cancelButtonRef.current.focus();
    }
  }, [isOpen]);

  // Escape key closes the dialog (unless deletion is in progress)
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen && !isDeleting) {
        onCancel();
      }
    };
    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      document.body.style.overflow = "hidden";
    }
    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "";
    };
  }, [isOpen, isDeleting, onCancel]);

  const handleConfirm = async () => {
    setIsDeleting(true);
    setDeleteError(null);
    try {
      const response = await deleteUserCollection(userId);
      onDeleted(response.albumsDeleted);
    } catch {
      setDeleteError("Deletion failed. Please try again.");
      setIsDeleting(false);
    }
  };

  if (!isOpen) return null;

  const countLabel =
    isLoadingCount
      ? "Loading…"
      : releaseCount !== null
      ? releaseCount === 1
        ? "1 release"
        : `${releaseCount} releases`
      : "an unknown number of releases";

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isDeleting) onCancel();
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="dcd-title"
      aria-describedby="dcd-description"
    >
      <div
        className="rounded-lg shadow-2xl w-full max-w-lg p-6 border border-red-900/60"
        style={{ backgroundColor: "var(--theme-card-bg)" }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-start gap-3 mb-5">
          {/* Warning icon */}
          <div className="flex-shrink-0 w-10 h-10 rounded-full bg-red-900/30 flex items-center justify-center mt-0.5">
            <svg className="w-5 h-5 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
            </svg>
          </div>
          <div>
            <h2
              id="dcd-title"
              className="text-lg font-semibold text-white"
            >
              Permanently Delete Collection
            </h2>
            <p className="text-xs text-red-400 font-medium mt-0.5">This action cannot be undone</p>
          </div>
        </div>

        {/* Body */}
        <div id="dcd-description" className="space-y-4 mb-6">
          {/* User info box */}
          <div className="rounded-md bg-gray-800 border border-gray-700 px-4 py-3 space-y-1">
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-400 w-16 flex-shrink-0">User</span>
              <span className="text-white font-medium break-all">{userEmail}</span>
            </div>
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-400 w-16 flex-shrink-0">Releases</span>
              {isLoadingCount ? (
                <span className="text-gray-400 italic">Loading…</span>
              ) : (
                <span className={`font-semibold ${releaseCount === 0 ? "text-gray-400" : "text-orange-400"}`}>
                  {releaseCount ?? "—"}
                </span>
              )}
            </div>
          </div>

          {fetchError && (
            <p className="text-xs text-yellow-400">{fetchError}</p>
          )}

          <p className="text-sm text-gray-300 leading-relaxed">
            You are about to permanently delete{" "}
            <strong className="text-white">{countLabel}</strong> and all associated cover art images from{" "}
            <strong className="text-white">{userEmail}</strong>&apos;s collection.
          </p>

          <div className="rounded-md bg-red-950/40 border border-red-800/50 px-4 py-3">
            <p className="text-sm text-red-300">
              The user account will be preserved, but their entire music collection will be
              destroyed and{" "}
              <strong className="text-red-200">cannot be recovered</strong>.
            </p>
          </div>

          {deleteError && (
            <p className="text-sm text-red-400 font-medium">{deleteError}</p>
          )}
        </div>

        {/* Actions */}
        <div className="flex gap-3 justify-end">
          <button
            ref={cancelButtonRef}
            type="button"
            onClick={onCancel}
            disabled={isDeleting}
            className="px-4 py-2 text-sm font-medium rounded-md border border-gray-600 text-gray-300 hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-gray-500 transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={isDeleting || isLoadingCount}
            className="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500 shadow-lg transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isDeleting ? "Deleting…" : "Delete Collection"}
          </button>
        </div>
      </div>
    </div>
  );
}
