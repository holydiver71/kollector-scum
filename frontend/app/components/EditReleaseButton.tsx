"use client";
import { useRouter } from "next/navigation";

export interface EditReleaseButtonProps {
  releaseId: number;
  releaseTitle: string;
  className?: string;
}

/**
 * Edit button component for navigating to the release edit page
 * Features:
 * - Navigates to /releases/[id]/edit page
 * - Accessible with keyboard navigation
 * - Clean pencil/edit icon
 */
export function EditReleaseButton({
  releaseId,
  releaseTitle,
  className = "",
}: EditReleaseButtonProps) {
  const router = useRouter();

  const handleEditClick = () => {
    router.push(`/releases/${releaseId}/edit`);
  };

  return (
    <button
      type="button"
      onClick={handleEditClick}
      className={`inline-flex items-center justify-center p-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors ${className}`}
      aria-label={`Edit ${releaseTitle}`}
      title="Edit release"
      data-testid="edit-release-button"
    >
      {/* Pencil/Edit Icon */}
      <svg
        className="w-5 h-5"
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
          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
        />
      </svg>
    </button>
  );
}
