"use client";

/**
 * DiscogsResultsStep – Step 2 of the Discogs add-release wizard.
 *
 * Displays a list of Discogs search results and requires the user to select
 * one before continuing to the details step.
 *
 * Validation:
 *  - The Continue button is disabled until a result is selected.
 *  - If the user attempts to continue without selecting, an inline error is shown.
 */

import { useState } from "react";
import Image from "next/image";
import type { DiscogsSearchResult } from "../../../lib/discogs-types";

interface DiscogsResultsStepProps {
  /** The results returned from the Discogs search */
  results: DiscogsSearchResult[];
  /** Initially highlighted result, e.g. when navigating back */
  initialSelection?: DiscogsSearchResult | null;
  /** Called when the user confirms their selection and clicks Continue */
  onContinue: (result: DiscogsSearchResult) => void;
  /** Called when the user wants to go back to the search step */
  onBack: () => void;
}

/** Formats a Discogs result into a short description line */
function resultSubtitle(result: DiscogsSearchResult): string {
  const parts: string[] = [];
  if (result.format) parts.push(result.format);
  if (result.country) parts.push(result.country);
  if (result.year) parts.push(result.year);
  return parts.join(" · ");
}

/** Results step component */
export default function DiscogsResultsStep({
  results,
  initialSelection = null,
  onContinue,
  onBack,
}: DiscogsResultsStepProps) {
  const [selected, setSelected] = useState<DiscogsSearchResult | null>(
    initialSelection
  );
  const [selectionError, setSelectionError] = useState<string | null>(null);

  const handleContinue = () => {
    if (!selected) {
      setSelectionError("Please select a release before continuing");
      return;
    }
    setSelectionError(null);
    onContinue(selected);
  };

  const handleSelect = (result: DiscogsSearchResult) => {
    setSelected(result);
    setSelectionError(null);
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div>
        <h2 className="text-xl font-black text-[var(--theme-foreground)]">
          Search Results
        </h2>
        <p className="text-sm text-gray-400 mt-1">
          {results.length} {results.length === 1 ? "match" : "matches"} found
          – select a release to view full details.
        </p>
      </div>

      {/* Result list */}
      <div className="space-y-3">
        {results.map((result) => {
          const isSelected = selected?.id === result.id;
          return (
            <div
              key={result.id}
              role="radio"
              aria-checked={isSelected}
              tabIndex={0}
              onClick={() => handleSelect(result)}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  handleSelect(result);
                }
              }}
              className={`flex gap-4 p-4 rounded-xl border cursor-pointer transition-all outline-none focus-visible:ring-2 focus-visible:ring-[var(--theme-accent)] ${
                isSelected
                  ? "border-[var(--theme-accent)] bg-[var(--theme-accent)]/10 shadow-[0_0_0_1px_var(--theme-accent)]"
                  : "border-[var(--theme-card-border)] bg-[var(--theme-card-bg)] hover:border-[var(--theme-accent)]/50"
              }`}
            >
              {/* Thumbnail */}
              <div className="flex-shrink-0">
                {result.thumbUrl || result.coverImageUrl ? (
                  <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-[#0F0F1A]">
                    <Image
                      src={result.thumbUrl || result.coverImageUrl || ""}
                      alt={`${result.title} cover`}
                      fill
                      sizes="64px"
                      className="object-cover"
                      onError={(e) => {
                        e.currentTarget.style.display = "none";
                      }}
                    />
                  </div>
                ) : (
                  <div className="w-16 h-16 rounded-lg bg-[#0F0F1A] flex items-center justify-center">
                    <svg
                      className="w-8 h-8 text-gray-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3"
                      />
                    </svg>
                  </div>
                )}
              </div>

              {/* Details */}
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-[var(--theme-foreground)] truncate">
                  {result.title}
                </p>
                {result.artist && (
                  <p className="text-sm text-gray-400 truncate">{result.artist}</p>
                )}
                <p className="text-xs text-gray-500 mt-1">
                  {resultSubtitle(result)}
                </p>
                {result.catalogNumber && (
                  <p className="text-xs text-gray-600 font-mono mt-0.5">
                    Cat# {result.catalogNumber}
                  </p>
                )}
                {result.label && (
                  <p className="text-xs text-gray-600 truncate">
                    {result.label}
                  </p>
                )}
              </div>

              {/* Selection indicator */}
              <div className="flex-shrink-0 flex items-center">
                <div
                  className={`w-5 h-5 rounded-full border-2 flex items-center justify-center transition-colors ${
                    isSelected
                      ? "border-[var(--theme-accent)] bg-[var(--theme-accent)]"
                      : "border-gray-600"
                  }`}
                >
                  {isSelected && (
                    <svg
                      className="w-3 h-3 text-white"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Selection validation error */}
      {selectionError && (
        <p role="alert" className="text-sm text-red-400">
          {selectionError}
        </p>
      )}

      {/* Footer actions */}
      <div className="flex items-center justify-between gap-4 pt-2">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[var(--theme-card-border)] text-gray-300 hover:text-white hover:border-[var(--theme-accent)]/50 transition-colors"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M15.75 19.5L8.25 12l7.5-7.5"
            />
          </svg>
          Back
        </button>

        <button
          type="button"
          onClick={handleContinue}
          className={`flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold transition-all ${
            !selected
              ? "bg-[var(--theme-accent)]/30 text-[var(--theme-accent)]/60 cursor-not-allowed"
              : "bg-[var(--theme-accent)] hover:opacity-90 text-white shadow-lg shadow-[var(--theme-accent)]/20"
          }`}
        >
          View Details
          <svg
            className="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M8.25 4.5l7.5 7.5-7.5 7.5"
            />
          </svg>
        </button>
      </div>
    </div>
  );
}
