"use client";

import { useState } from "react";
import type { WizardFormData, ValidationErrors, LookupItem } from "../types";
import type { ReleaseLookups } from "../useReleaseLookups";

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors */
  errors: ValidationErrors;
  /** Real lookup data from the API */
  lookups: ReleaseLookups;
}

/**
 * Panel 3 – Label & Dates.
 * Collects the release year, original release year, record label,
 * catalogue number and UPC/barcode.
 *
 * The label field uses a custom autocomplete so users can either select an
 * existing label (sets labelId + labelName) or type a new one (sets labelName
 * only; the backend will create the label on submission).
 */
export default function LabelInformationPanel({ data, onChange, errors, lookups }: Props) {
  const [labelInput, setLabelInput] = useState(data.labelName);
  const [showLabelSuggestions, setShowLabelSuggestions] = useState(false);

  const filteredLabels: LookupItem[] = labelInput.trim()
    ? lookups.labels
        .filter((l) => l.name.toLowerCase().includes(labelInput.toLowerCase()))
        .slice(0, 8)
    : [];

  const selectLabel = (label: LookupItem) => {
    setLabelInput(label.name);
    setShowLabelSuggestions(false);
    onChange({ labelId: label.id, labelName: label.name });
  };

  const handleLabelInputChange = (value: string) => {
    setLabelInput(value);
    setShowLabelSuggestions(true);
    // If user clears or types a new value, clear the stored ID
    const exact = lookups.labels.find(
      (l) => l.name.toLowerCase() === value.toLowerCase()
    );
    onChange({ labelId: exact?.id, labelName: value });
  };

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
              htmlFor="wiz-releaseYear"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Release Year
            </label>
            <input
              id="wiz-releaseYear"
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
              htmlFor="wiz-origReleaseYear"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Original Release Year{" "}
              <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
                (optional)
              </span>
            </label>
            <input
              id="wiz-origReleaseYear"
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

      {/* Label – custom autocomplete */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <label
          htmlFor="wiz-label"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Record Label{" "}
          <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
            (optional)
          </span>
        </label>
        <div className="relative">
          <input
            id="wiz-label"
            type="text"
            value={labelInput}
            onChange={(e) => handleLabelInputChange(e.target.value)}
            onFocus={() => setShowLabelSuggestions(true)}
            onBlur={() => setTimeout(() => setShowLabelSuggestions(false), 150)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                if (filteredLabels.length > 0) {
                  selectLabel(filteredLabels[0]);
                }
              }
              if (e.key === "Escape") setShowLabelSuggestions(false);
            }}
            placeholder="Search or type a new label name…"
            className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
          />

          {showLabelSuggestions && filteredLabels.length > 0 && (
            <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
              {filteredLabels.map((l) => (
                <li key={l.id}>
                  <button
                    type="button"
                    onMouseDown={() => selectLabel(l)}
                    className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                  >
                    {l.name}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
        <p className="mt-1.5 text-xs text-gray-600">
          Search existing labels or type a new name to create one.
        </p>
      </div>

      {/* Catalogue number + UPC */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="wiz-labelNumber"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Catalogue Number{" "}
              <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
                (optional)
              </span>
            </label>
            <input
              id="wiz-labelNumber"
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
              htmlFor="wiz-upc"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              UPC / Barcode{" "}
              <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
                (optional)
              </span>
            </label>
            <input
              id="wiz-upc"
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
