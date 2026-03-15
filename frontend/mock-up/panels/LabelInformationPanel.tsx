"use client";

import type { MockFormData, ValidationErrors } from "../types";
import { LABELS } from "../fixtures";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/**
 * Panel 4 – Label & Dates.
 * Collects the release year, original release year, record label,
 * catalogue number and UPC/barcode.
 */
export default function LabelInformationPanel({ data, onChange, errors }: Props) {
  const copyYearForward = () => {
    if (data.releaseYear) {
      onChange({ origReleaseYear: data.releaseYear });
    }
  };

  return (
    <div className="space-y-5">
      {/* Release Years */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <p className="text-xs text-gray-600 mb-3">
          Enter the year this specific edition was released. If it is a reissue
          or remaster, also fill in the original release year.
        </p>
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

          {/* Copy arrow (desktop) */}
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

        {/* Copy button (mobile) */}
        <div className="md:hidden mt-3">
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

      {/* Label */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <label
          htmlFor="mock-label"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Record Label{" "}
          <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
        </label>
        <select
          id="mock-label"
          value={data.labelName}
          onChange={(e) => onChange({ labelName: e.target.value })}
          className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
        >
          <option value="">Select or search a label…</option>
          {LABELS.map((l) => (
            <option key={l.id} value={l.name}>
              {l.name}
            </option>
          ))}
        </select>
        <p className="mt-1.5 text-xs text-gray-600">
          Can&apos;t find the label? Type it directly in the Catalogue Number
          field notes, or ask an admin to add it.
        </p>
      </div>

      {/* Catalogue number + UPC side by side */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label
            htmlFor="mock-labelNumber"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Catalogue Number{" "}
            <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
          </label>
          <input
            id="mock-labelNumber"
            type="text"
            value={data.labelNumber}
            onChange={(e) => onChange({ labelNumber: e.target.value })}
            placeholder="e.g. EMC 3400"
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              errors.labelNumber
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />
          {errors.labelNumber && (
            <p className="mt-1.5 text-sm text-red-400" role="alert">
              {errors.labelNumber}
            </p>
          )}
        </div>

        <div>
          <label
            htmlFor="mock-upc"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            UPC / Barcode{" "}
            <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
          </label>
          <input
            id="mock-upc"
            type="text"
            value={data.upc}
            onChange={(e) => onChange({ upc: e.target.value })}
            placeholder="e.g. 077774681116"
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              errors.upc
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />
          {errors.upc && (
            <p className="mt-1.5 text-sm text-red-400" role="alert">
              {errors.upc}
            </p>
          )}
        </div>
      </div>
      </div>
    </div>
  );
}
