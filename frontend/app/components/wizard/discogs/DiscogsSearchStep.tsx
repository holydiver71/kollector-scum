"use client";

/**
 * DiscogsSearchStep
 *
 * Step 1 of the Discogs add-release wizard.  Renders a search form that
 * accepts a catalogue number plus optional filters, then calls the Discogs
 * search API.  On success it invokes `onSearchSuccess` with the results and
 * the submitted request so the parent can advance to the results step while
 * preserving the search criteria for Back navigation.
 *
 * The component accepts `initialValues` so the form is pre-populated when
 * the user navigates back from the results step.
 */

import { useState } from "react";
import { searchDiscogs } from "../../../lib/api";
import type { DiscogsSearchRequest, DiscogsSearchResult } from "../../../lib/discogs-types";

export interface DiscogsSearchStepProps {
  /** Pre-populate the form fields when the user navigates back. */
  initialValues?: DiscogsSearchRequest;
  /** Called with results and the submitted request on a successful search. */
  onSearchSuccess: (results: DiscogsSearchResult[], request: DiscogsSearchRequest) => void;
  /** Called when the search returns an error or zero results. */
  onSearchError: (message: string) => void;
  /** Called when the user clicks the Back button to return to method selection. */
  onBack?: () => void;
}

/**
 * Step 1 of the Discogs import wizard – catalogue-number search form.
 */
export default function DiscogsSearchStep({
  initialValues,
  onSearchSuccess,
  onSearchError,
  onBack,
}: DiscogsSearchStepProps) {
  const [catalogNumber, setCatalogNumber] = useState(
    initialValues?.catalogNumber ?? ""
  );
  const [format, setFormat] = useState(initialValues?.format ?? "");
  const [country, setCountry] = useState(initialValues?.country ?? "");
  const [year, setYear] = useState(
    initialValues?.year ? String(initialValues.year) : ""
  );
  const [isSearching, setIsSearching] = useState(false);
  const [localError, setLocalError] = useState("");

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!catalogNumber.trim()) {
      const msg = "Please enter a catalogue number";
      setLocalError(msg);
      onSearchError(msg);
      return;
    }

    setLocalError("");
    setIsSearching(true);

    try {
      const request: DiscogsSearchRequest = {
        catalogNumber: catalogNumber.trim(),
        ...(format && { format }),
        ...(country && { country }),
        ...(year && { year: parseInt(year, 10) }),
      };

      const results = await searchDiscogs(request);

      if (results.length === 0) {
        const msg = `No results found for catalogue number "${catalogNumber}"`;
        setLocalError(msg);
        onSearchError(msg);
      } else {
        onSearchSuccess(results, request);
      }
    } catch (err) {
      const msg =
        err instanceof Error ? err.message : "Failed to search Discogs";
      setLocalError(msg);
      onSearchError(msg);
    } finally {
      setIsSearching(false);
    }
  };

  const handleClear = () => {
    setCatalogNumber("");
    setFormat("");
    setCountry("");
    setYear("");
    setLocalError("");
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-black text-white mb-1">Search Discogs</h2>
        <p className="text-sm text-gray-500">
          Enter a catalogue number to find matching releases on Discogs.
        </p>
      </div>

      <form onSubmit={handleSearch} className="space-y-4">
        {/* Catalogue Number – required */}
        <div>
          <label
            htmlFor="discogs-catalog-number"
            className="block text-sm font-medium text-gray-300 mb-1"
          >
            Catalogue Number <span className="text-red-400">*</span>
          </label>
          <input
            type="text"
            id="discogs-catalog-number"
            value={catalogNumber}
            onChange={(e) => {
              setCatalogNumber(e.target.value);
              if (localError) setLocalError("");
            }}
            placeholder="e.g., MOVLP001, ABC-12345"
            className={`w-full px-3 py-2 bg-[#0F0F1A] border rounded-lg text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              localError
                ? "border-red-500 focus:ring-red-500"
                : "border-[#1C1C28] focus:ring-[#8B5CF6] focus:border-[#8B5CF6]"
            }`}
            disabled={isSearching}
            aria-required="true"
            aria-describedby={localError ? "catalog-error" : "catalog-hint"}
          />
          {localError ? (
            <p id="catalog-error" role="alert" className="mt-1 text-sm text-red-400">
              {localError}
            </p>
          ) : (
            <p id="catalog-hint" className="mt-1 text-xs text-gray-600">
              Usually found on the spine or back cover of the release.
            </p>
          )}
        </div>

        {/* Optional filters */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div>
            <label
              htmlFor="discogs-format"
              className="block text-sm font-medium text-gray-300 mb-1"
            >
              Format
              <span className="ml-1 text-xs text-gray-600">(optional)</span>
            </label>
            <input
              type="text"
              id="discogs-format"
              value={format}
              onChange={(e) => setFormat(e.target.value)}
              placeholder="e.g., Vinyl, CD"
              className="w-full px-3 py-2 bg-[#0F0F1A] border border-[#1C1C28] rounded-lg text-white placeholder-gray-600 focus:outline-none focus:ring-1 focus:ring-[#8B5CF6] focus:border-[#8B5CF6] transition-colors"
              disabled={isSearching}
            />
          </div>

          <div>
            <label
              htmlFor="discogs-country"
              className="block text-sm font-medium text-gray-300 mb-1"
            >
              Country
              <span className="ml-1 text-xs text-gray-600">(optional)</span>
            </label>
            <input
              type="text"
              id="discogs-country"
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              placeholder="e.g., US, UK"
              className="w-full px-3 py-2 bg-[#0F0F1A] border border-[#1C1C28] rounded-lg text-white placeholder-gray-600 focus:outline-none focus:ring-1 focus:ring-[#8B5CF6] focus:border-[#8B5CF6] transition-colors"
              disabled={isSearching}
            />
          </div>

          <div>
            <label
              htmlFor="discogs-year"
              className="block text-sm font-medium text-gray-300 mb-1"
            >
              Year
              <span className="ml-1 text-xs text-gray-600">(optional)</span>
            </label>
            <input
              type="number"
              id="discogs-year"
              value={year}
              onChange={(e) => setYear(e.target.value)}
              placeholder="e.g., 2020"
              min="1900"
              max={new Date().getFullYear() + 1}
              className="w-full px-3 py-2 bg-[#0F0F1A] border border-[#1C1C28] rounded-lg text-white placeholder-gray-600 focus:outline-none focus:ring-1 focus:ring-[#8B5CF6] focus:border-[#8B5CF6] transition-colors"
              disabled={isSearching}
            />
          </div>
        </div>

        {/* Clear – secondary form action, not a navigation button */}
        <div className="flex justify-start pt-1">
          <button
            type="button"
            onClick={handleClear}
            disabled={isSearching}
            className="text-xs text-gray-500 hover:text-gray-300 underline underline-offset-2 cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Clear fields
          </button>
        </div>

        {/* Navigation footer */}
        <div className="flex items-center justify-between pt-4 mt-2 border-t border-[#1C1C28]">
          {onBack ? (
            <button
              type="button"
              onClick={onBack}
              disabled={isSearching}
              className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl text-sm font-semibold text-gray-300 hover:text-white border border-[#1C1C28] hover:border-[#8B5CF6]/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              Cancel
            </button>
          ) : (
            <span />
          )}

          <button
            type="submit"
            disabled={isSearching || !catalogNumber.trim()}
            className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-[#8B5CF6] hover:bg-[#7C3AED] text-white shadow-lg shadow-[#8B5CF6]/20 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
          >
            {isSearching ? (
              <>
                <svg
                  className="animate-spin h-4 w-4"
                  xmlns="http://www.w3.org/2000/svg"
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
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Searching…
              </>
            ) : (
              <>
                Search Discogs
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
