"use client";
import { useState } from "react";
import { DeleteCollectionButton } from "../components/DeleteCollectionButton";
import ThemeSwitcher from "../components/ThemeSwitcher";
import type { ThemeName } from "../contexts/ThemeContext";

export default function ProfilePage() {
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const handleDeleteSuccess = (deletedCount: number) => {
    setSuccessMessage(`Successfully deleted ${deletedCount} album${deletedCount !== 1 ? 's' : ''} from your collection.`);
    // Auto-dismiss success message after 5 seconds
    setTimeout(() => setSuccessMessage(null), 5000);
  };

  const handleThemeSaveSuccess = (theme: ThemeName) => {
    setSuccessMessage(`Theme changed to "${theme}".`);
    setTimeout(() => setSuccessMessage(null), 5000);
  };

  const handleThemeSaveError = (error: string) => {
    setSuccessMessage(`Failed to save theme: ${error}`);
    setTimeout(() => setSuccessMessage(null), 5000);
  };

  return (
    <div className="min-h-screen bg-transparent">
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Success message */}
        {successMessage && (
          <div className="mb-6 bg-emerald-500/10 border border-emerald-500/20 rounded-xl p-4">
            <div className="flex items-start gap-3">
              <svg
                className="w-5 h-5 text-emerald-400 flex-shrink-0 mt-0.5"
                fill="currentColor"
                viewBox="0 0 20 20"
                aria-hidden="true"
              >
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                  clipRule="evenodd"
                />
              </svg>
              <div className="flex-1">
                <h3 className="text-sm font-medium text-emerald-300">
                  Success
                </h3>
                <p className="text-sm text-emerald-400 mt-1">{successMessage}</p>
              </div>
              <button
                type="button"
                onClick={() => setSuccessMessage(null)}
                className="text-emerald-400 hover:text-emerald-300"
                aria-label="Dismiss message"
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

        {/* Theme Settings Section */}
        <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-8 mb-6">
          <div className="mb-6">
            <h2 className="text-2xl font-bold text-white mb-2">
              Appearance
            </h2>
            <p className="text-gray-400">
              Choose your preferred UI theme. The change is applied immediately.
            </p>
          </div>
          <ThemeSwitcher
            onSaveSuccess={handleThemeSaveSuccess}
            onSaveError={handleThemeSaveError}
          />
        </div>

        {/* Delete Collection Section */}
        <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-8">
          <div className="mb-6">
            <h2 className="text-2xl font-bold text-white mb-2">
              Collection Management
            </h2>
            <p className="text-gray-400">
              Manage your music collection settings and data.
            </p>
          </div>

          <div className="border-t border-[#1C1C28] pt-6">
            <div className="flex items-start justify-between">
              <div className="flex-1 pr-8">
                <h3 className="text-lg font-semibold text-white mb-2">
                  Delete Collection
                </h3>
                <p className="text-gray-400 mb-4">
                  Permanently delete all albums from your collection. This action cannot be undone.
                  This is useful for testing Discogs import or starting fresh with a new collection.
                </p>
                <div className="bg-yellow-500/10 border border-yellow-500/20 rounded-xl p-4 mb-4">
                  <div className="flex items-start gap-2">
                    <svg
                      className="w-5 h-5 text-yellow-400 flex-shrink-0 mt-0.5"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                      aria-hidden="true"
                    >
                      <path
                        fillRule="evenodd"
                        d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                        clipRule="evenodd"
                      />
                    </svg>
                    <div className="flex-1">
                      <h4 className="text-sm font-medium text-yellow-300">
                        Warning: This action is permanent
                      </h4>
                      <p className="text-sm text-yellow-400 mt-1">
                        All albums and their associated cover images will be permanently removed from your collection. 
                        You will need to re-import or re-add them if you change your mind.
                      </p>
                    </div>
                  </div>
                </div>
              </div>
              <div className="flex-shrink-0">
                <DeleteCollectionButton
                  onDeleteSuccess={handleDeleteSuccess}
                  className="whitespace-nowrap"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
