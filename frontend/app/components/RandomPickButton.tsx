"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Shuffle } from "lucide-react";
import { getRandomReleaseId, ApiError } from "../lib/api";

export interface RandomPickButtonProps {
  className?: string;
}

/**
 * Icon button component that navigates to a random release from the collection
 * Features:
 * - Fetches a random release ID from the API
 * - Navigates to the release details page
 * - Loading states during fetch
 * - Error handling with user feedback
 * - Accessible with keyboard navigation
 */
export function RandomPickButton({ className = "" }: RandomPickButtonProps) {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleClick = async () => {
    if (isLoading) return;

    setIsLoading(true);
    setError(null);

    try {
      const randomId = await getRandomReleaseId();
      router.push(`/releases/${randomId}`);
    } catch (err) {
      const apiError = err as ApiError;

      let errorMessage = "Failed to get random release. Please try again.";

      if (apiError.status === 404) {
        errorMessage = "No releases in collection yet.";
      } else if (apiError.status === 500) {
        errorMessage = "Server error. Please try again later.";
      } else if (apiError.message?.includes("timeout")) {
        errorMessage = "Request timed out. Please check your connection.";
      }

      setError(errorMessage);

      // Auto-dismiss error after 3 seconds
      setTimeout(() => setError(null), 3000);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={handleClick}
        disabled={isLoading}
        className={`inline-flex items-center justify-center p-2 text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded-full focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors ${className}`}
        aria-label="View random release"
        title={isLoading ? "Loading..." : "View random release"}
      >
        <Shuffle
          className={`w-5 h-5 ${isLoading ? "animate-spin" : ""}`}
          aria-hidden="true"
        />
      </button>

      {/* Error toast */}
      {error && (
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
              <p className="text-sm text-red-700">{error}</p>
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
    </>
  );
}
