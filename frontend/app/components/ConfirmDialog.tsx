"use client";
import { useEffect, useRef } from "react";

export interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
  isDangerous?: boolean;
}

/**
 * Reusable confirmation dialog component with accessibility features
 * Supports keyboard navigation (Tab, Enter, Escape)
 */

/** Opacity applied to body/message text to visually distinguish it from heading text */
const DIALOG_MESSAGE_OPACITY = 0.8;

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  onConfirm,
  onCancel,
  isDangerous = false,
}: ConfirmDialogProps) {
  const cancelButtonRef = useRef<HTMLButtonElement>(null);
  const dialogRef = useRef<HTMLDivElement>(null);

  // Focus management: focus cancel button when dialog opens
  useEffect(() => {
    if (isOpen && cancelButtonRef.current) {
      cancelButtonRef.current.focus();
    }
  }, [isOpen]);

  // Handle Escape key to close dialog
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen) {
        onCancel();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      // Prevent body scroll when dialog is open
      document.body.style.overflow = "hidden";
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "";
    };
  }, [isOpen, onCancel]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={(e) => {
        // Close on backdrop click
        if (e.target === e.currentTarget) {
          onCancel();
        }
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="dialog-title"
      aria-describedby="dialog-description"
    >
      <div
        ref={dialogRef}
        className="rounded-lg shadow-xl max-w-md w-full p-6 transform transition-all border"
        style={{
          backgroundColor: "var(--theme-card-bg)",
          borderColor: "var(--theme-card-border)",
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Dialog Header */}
        <div className="mb-4">
          <h2
            id="dialog-title"
            className="text-xl font-semibold"
            style={{ color: "var(--theme-card-text)" }}
          >
            {title}
          </h2>
        </div>

        {/* Dialog Content */}
        <div className="mb-6">
          <p
            id="dialog-description"
            className="text-sm"
            style={{ color: "var(--theme-card-text)", opacity: DIALOG_MESSAGE_OPACITY }}
          >
            {message}
          </p>
        </div>

        {/* Dialog Actions */}
        <div className="flex gap-3 justify-end">
          <button
            ref={cancelButtonRef}
            type="button"
            onClick={onCancel}
            className="px-4 py-2 text-sm font-medium rounded-md border focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-400 transition-opacity hover:opacity-70"
            style={{
              color: "var(--theme-card-text)",
              borderColor: "var(--theme-card-border)",
              backgroundColor: "transparent",
            }}
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            className={`px-4 py-2 text-sm font-medium text-white rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors ${
              isDangerous
                ? "bg-red-600 hover:bg-red-700 focus:ring-red-500 shadow-lg"
                : "focus:ring-2"
            }`}
            style={
              !isDangerous
                ? {
                    backgroundColor: "var(--theme-accent)",
                    color: "var(--theme-foreground)",
                  }
                : undefined
            }
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
