"use client";

import type { MockFormData, ValidationErrors } from "../types";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/**
 * Panel 2 – Release Dates.
 * Collects the release year and, optionally, the original release year.
 * Both fields accept four-digit years. An arrow button copies the release
 * year across to the original release year, matching the existing form UI.
 */
export default function ReleaseDatesPanel({ data, onChange, errors }: Props) {
  const copyYearForward = () => {
    if (data.releaseYear) {
      onChange({ origReleaseYear: data.releaseYear });
    }
  };

  return (
    <div className="space-y-6">
      {/* Helper copy text */}
      <p className="text-sm text-gray-500">
        Enter the year this specific edition was released. If it is a reissue
        or remaster, also fill in the original release year below.
      </p>

      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <div className="flex flex-col md:flex-row md:items-start gap-4 md:gap-2">
        {/* Release Year */}
        <div className="flex-1">
          <label
            htmlFor="mock-releaseYear"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Release Year
          </label>
          <input
            id="mock-releaseYear"
            type="number"
            min={1900}
            max={2030}
            value={data.releaseYear}
            onChange={(e) => onChange({ releaseYear: e.target.value })}
            placeholder="e.g. 1982"
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              errors.releaseYear
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />
          {errors.releaseYear && (
            <p className="mt-1.5 text-sm text-red-400" role="alert">
              {errors.releaseYear}
            </p>
          )}
        </div>

        {/* Copy arrow */}
        <div className="hidden md:flex items-end pb-3">
          <button
            type="button"
            onClick={copyYearForward}
            title="Copy release year to original release year"
            className="p-2 text-gray-500 hover:text-[#A78BFA] hover:bg-[#8B5CF6]/10 rounded-full transition-colors"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              className="h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M13 7l5 5m0 0l-5 5m5-5H6"
              />
            </svg>
          </button>
        </div>

        {/* Original Release Year */}
        <div className="flex-1">
          <label
            htmlFor="mock-origReleaseYear"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Original Release Year{" "}
            <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
          </label>
          <input
            id="mock-origReleaseYear"
            type="number"
            min={1900}
            max={2030}
            value={data.origReleaseYear}
            onChange={(e) => onChange({ origReleaseYear: e.target.value })}
            placeholder="e.g. 1982"
            className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
          />
        </div>
      </div>
      </div>
      <div className="md:hidden">
        <button
          type="button"
          onClick={copyYearForward}
          className="flex items-center gap-2 text-sm text-[#A78BFA] hover:text-[#8B5CF6] transition-colors"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-4 w-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M13 7l5 5m0 0l-5 5m5-5H6"
            />
          </svg>
          Copy release year to original
        </button>
      </div>
    </div>
  );
}
