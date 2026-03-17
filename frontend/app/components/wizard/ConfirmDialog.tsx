"use client";

/**
 * ConfirmDialog
 *
 * A simple modal confirmation dialog used to guard destructive navigation
 * actions (e.g. cancelling a wizard and discarding unsaved data).
 */
interface ConfirmDialogProps {
  isOpen: boolean;
  title?: string;
  message?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Called when the user confirms the action */
  onConfirm: () => void;
  /** Called when the user dismisses the dialog without confirming */
  onDismiss: () => void;
}

/**
 * Renders a centred overlay modal asking the user to confirm before
 * proceeding.  Clicking the backdrop dismisses without confirming.
 */
export default function ConfirmDialog({
  isOpen,
  title = "Discard changes?",
  message = "Any data you've entered will be lost. This action cannot be undone.",
  confirmLabel = "Discard & leave",
  cancelLabel = "Keep editing",
  onConfirm,
  onDismiss,
}: ConfirmDialogProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onDismiss}
        aria-hidden="true"
      />
      {/* Modal */}
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        className="relative bg-[#13131F] border border-[#1C1C28] rounded-2xl p-6 shadow-2xl max-w-sm w-full mx-4 space-y-4"
      >
        <h2 id="confirm-dialog-title" className="text-lg font-bold text-white">
          {title}
        </h2>
        <p className="text-sm text-gray-400">{message}</p>
        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onDismiss}
            className="px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 transition-colors"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            className="px-5 py-2.5 rounded-xl text-sm font-bold bg-red-600 hover:bg-red-500 text-white transition-colors shadow-lg shadow-red-900/30"
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
