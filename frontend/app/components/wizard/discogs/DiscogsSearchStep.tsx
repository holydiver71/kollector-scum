"use client";

/**
 * DiscogsSearchStep – Step 1 of the Discogs add-release wizard.
 *
 * Renders a catalogue-number search form with optional filters.
 * On successful search the parent wizard advances to the results step
 * automatically via `onSearchSuccess`.
 *
 * Validation:
 *  - Catalogue number is required
 *  - Year must be a four-digit number between 1900 and the current year + 1
 */

import { useState } from "react";
import { searchDiscogs } from "../../../lib/api";
import type { DiscogsSearchRequest, DiscogsSearchResult } from "../../../lib/discogs-types";

interface DiscogsSearchStepProps {
  /** Initial field values, e.g. when the user navigates back to refine criteria */
  initialValues?: DiscogsSearchRequest;
  /** Called with the results and the submitted request when the search succeeds */
  onSearchSuccess: (results: DiscogsSearchResult[], request: DiscogsSearchRequest) => void;
  /** Called with an error message when the search fails or returns no results */
  onSearchError: (message: string) => void;
  /** Optional: navigate to the manual entry flow */
  onSwitchToManual?: () => void;
}

/**
 * Validates the search form and returns an object of field-level error messages.
 * An empty object means the form is valid.
 */
function validateSearchForm(
  catalogNumber: string,
  year: string
): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!catalogNumber.trim()) {
    errors.catalogNumber = "Catalogue number is required";
  }

  if (year.trim()) {
    const yearNum = parseInt(year, 10);
    const currentYear = new Date().getFullYear();
    if (isNaN(yearNum) || yearNum < 1900 || yearNum > currentYear + 1) {
      errors.year = `Year must be between 1900 and ${currentYear + 1}`;
    }
  }

  return errors;
}

/** Search step component */
export default function DiscogsSearchStep({
  initialValues,
  onSearchSuccess,
  onSearchError,
  onSwitchToManual,
}: DiscogsSearchStepProps) {
  const [catalogNumber, setCatalogNumber] = useState(
    initialValues?.catalogNumber ?? ""
  );
  const [format, setFormat] = useState(initialValues?.format ?? "");
  const [country, setCountry] = useState(initialValues?.country ?? "");
  const [year, setYear] = useState(
    initialValues?.year !== undefined ? String(initialValues.year) : ""
  );
  const [isSearching, setIsSearching] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();

    const errors = validateSearchForm(catalogNumber, year);
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }
    setFieldErrors({});
    setIsSearching(true);

    try {
      const request: DiscogsSearchRequest = {
        catalogNumber: catalogNumber.trim(),
        ...(format.trim() && { format: format.trim() }),
        ...(country.trim() && { country: country.trim() }),
        ...(year.trim() && { year: parseInt(year, 10) }),
      };

      const results = await searchDiscogs(request);

      if (results.length === 0) {
        onSearchError(
          `No results found for catalogue number "${catalogNumber.trim()}"`
        );
      } else {
        onSearchSuccess(results, request);
      }
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to search Discogs";
      onSearchError(message);
    } finally {
      setIsSearching(false);
    }
  };

  const handleClear = () => {
    setCatalogNumber("");
    setFormat("");
    setCountry("");
    setYear("");
    setFieldErrors({});
  };

  return (
    <div className="space-y-6">
      {/* Intro */}
      <div>
        <h2 className="text-xl font-black text-[var(--theme-foreground)]">
          Search Discogs
        </h2>
        <p className="text-sm text-gray-400 mt-1">
          Enter the catalogue number from the release (usually found on the
          spine or back cover).
        </p>
      </div>

      <form onSubmit={handleSearch} className="space-y-4" noValidate>
        {/* Catalogue number */}
        <div>
          <label
            htmlFor="discogs-cat-number"
            className="block text-sm font-medium text-[var(--theme-foreground)] mb-1"
          >
            Catalogue Number <span className="text-red-400">*</span>
          </label>
          <input
            type="text"
            id="discogs-cat-number"
            value={catalogNumber}
            onChange={(e) => {
              setCatalogNumber(e.target.value);
              if (fieldErrors.catalogNumber) {
                setFieldErrors((prev) => {
                  const { catalogNumber: _c, ...rest } = prev;
                  return rest;
                });
              }
            }}
            placeholder="e.g. MOVLP001, ABC-12345"
            className={`w-full px-3 py-2 bg-[var(--theme-input-bg,#0F0F1A)] border rounded-lg text-[var(--theme-foreground)] placeholder-gray-600 focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)] transition ${
              fieldErrors.catalogNumber
                ? "border-red-500"
                : "border-[var(--theme-card-border)]"
            }`}
            disabled={isSearching}
            aria-describedby={
              fieldErrors.catalogNumber ? "cat-number-error" : undefined
            }
            aria-invalid={!!fieldErrors.catalogNumber}
          />
          {fieldErrors.catalogNumber && (
            <p
              id="cat-number-error"
              role="alert"
              className="mt-1 text-sm text-red-400"
            >
              {fieldErrors.catalogNumber}
            </p>
          )}
        </div>

        {/* Optional filters */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label
              htmlFor="discogs-format"
              className="block text-sm font-medium text-[var(--theme-foreground)] mb-1"
            >
              Format{" "}
              <span className="text-gray-500 font-normal">(Optional)</span>
            </label>
            <input
              type="text"
              id="discogs-format"
              value={format}
              onChange={(e) => setFormat(e.target.value)}
              placeholder="e.g. Vinyl, CD"
              className="w-full px-3 py-2 bg-[var(--theme-input-bg,#0F0F1A)] border border-[var(--theme-card-border)] rounded-lg text-[var(--theme-foreground)] placeholder-gray-600 focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)] transition"
              disabled={isSearching}
            />
          </div>

          <div>
            <label
              htmlFor="discogs-country"
              className="block text-sm font-medium text-[var(--theme-foreground)] mb-1"
            >
              Country{" "}
              <span className="text-gray-500 font-normal">(Optional)</span>
            </label>
            <input
              type="text"
              id="discogs-country"
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              placeholder="e.g. US, UK"
              className="w-full px-3 py-2 bg-[var(--theme-input-bg,#0F0F1A)] border border-[var(--theme-card-border)] rounded-lg text-[var(--theme-foreground)] placeholder-gray-600 focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)] transition"
              disabled={isSearching}
            />
          </div>

          <div>
            <label
              htmlFor="discogs-year"
              className="block text-sm font-medium text-[var(--theme-foreground)] mb-1"
            >
              Year <span className="text-gray-500 font-normal">(Optional)</span>
            </label>
            <input
              type="number"
              id="discogs-year"
              value={year}
              onChange={(e) => {
                setYear(e.target.value);
                if (fieldErrors.year) {
                  setFieldErrors((prev) => {
                    const { year: _y, ...rest } = prev;
                    return rest;
                  });
                }
              }}
              placeholder="e.g. 2020"
              min="1900"
              max={new Date().getFullYear() + 1}
              className={`w-full px-3 py-2 bg-[var(--theme-input-bg,#0F0F1A)] border rounded-lg text-[var(--theme-foreground)] placeholder-gray-600 focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)] transition ${
                fieldErrors.year
                  ? "border-red-500"
                  : "border-[var(--theme-card-border)]"
              }`}
              disabled={isSearching}
              aria-describedby={fieldErrors.year ? "year-error" : undefined}
              aria-invalid={!!fieldErrors.year}
            />
            {fieldErrors.year && (
              <p
                id="year-error"
                role="alert"
                className="mt-1 text-sm text-red-400"
              >
                {fieldErrors.year}
              </p>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex gap-3 flex-wrap">
          <button
            type="submit"
            disabled={isSearching || !catalogNumber.trim()}
            className="flex-1 min-w-[140px] bg-[var(--theme-accent)] text-white px-6 py-2.5 rounded-xl font-semibold hover:opacity-90 disabled:opacity-40 disabled:cursor-not-allowed transition-opacity flex items-center justify-center gap-2"
          >
            {isSearching ? (
              <>
                <svg
                  className="animate-spin h-4 w-4"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                  />
                </svg>
                Searching…
              </>
            ) : (
              "Search Discogs"
            )}
          </button>

          <button
            type="button"
            onClick={handleClear}
            disabled={isSearching}
            className="px-5 py-2.5 border border-[var(--theme-card-border)] text-[var(--theme-foreground)] rounded-xl font-medium hover:bg-[var(--theme-sidebar-hover)] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Clear
          </button>
        </div>
      </form>

      {/* Manual entry escape hatch */}
      {onSwitchToManual && (
        <p className="text-sm text-gray-500">
          Prefer to enter data yourself?{" "}
          <button
            type="button"
            onClick={onSwitchToManual}
            className="text-[var(--theme-accent)] hover:underline"
          >
            Switch to manual entry
          </button>
        </p>
      )}
    </div>
  );
}
