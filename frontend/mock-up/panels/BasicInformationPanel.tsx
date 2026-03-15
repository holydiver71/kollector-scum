"use client";

import { useState } from "react";
import type { MockFormData, ValidationErrors } from "../types";
import { ARTISTS } from "../fixtures";

interface Props {
  /** Current form data */
  data: MockFormData;
  /** Callback when any field in this panel changes */
  onChange: (updates: Partial<MockFormData>) => void;
  /** Per-field validation errors to display */
  errors: ValidationErrors;
}

/**
 * Panel 1 – Basic Information.
 * Collects the release title and one or more artist names.
 * This is the only required panel; Next is blocked until both fields are filled.
 */
export default function BasicInformationPanel({ data, onChange, errors }: Props) {
  const [artistInput, setArtistInput] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);

  const filteredSuggestions = artistInput.trim()
    ? ARTISTS.filter(
        (a) =>
          a.name.toLowerCase().includes(artistInput.toLowerCase()) &&
          !data.artistNames.includes(a.name)
      ).slice(0, 8)
    : [];

  const addArtist = (name: string) => {
    const trimmed = name.trim();
    if (!trimmed || data.artistNames.includes(trimmed)) return;
    onChange({ artistNames: [...data.artistNames, trimmed] });
    setArtistInput("");
    setShowSuggestions(false);
  };

  const removeArtist = (name: string) => {
    onChange({ artistNames: data.artistNames.filter((a) => a !== name) });
  };

  const handleArtistKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      if (filteredSuggestions.length > 0 && artistInput.trim()) {
        addArtist(filteredSuggestions[0].name);
      } else if (artistInput.trim()) {
        addArtist(artistInput);
      }
    }
    if (e.key === "Escape") {
      setShowSuggestions(false);
    }
  };

  return (
    <div className="space-y-5">
      {/* Title */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <label
          htmlFor="mock-title"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Title <span className="text-red-400">*</span>
        </label>
        <input
          id="mock-title"
          type="text"
          value={data.title}
          onChange={(e) => onChange({ title: e.target.value })}
          placeholder="Enter the album or release title"
          className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
            errors.title
              ? "border-red-500 focus:ring-red-500"
              : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
          }`}
        />
        {errors.title && (
          <p className="mt-1.5 text-sm text-red-400" role="alert">
            {errors.title}
          </p>
        )}
      </div>

      {/* Artists */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <label className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
          Artists <span className="text-red-400">*</span>
        </label>

        {/* Selected artist tags */}
        {data.artistNames.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-3">
            {data.artistNames.map((name) => (
              <span
                key={name}
                className="inline-flex items-center gap-1.5 bg-[#8B5CF6]/20 text-[#A78BFA] text-sm px-3 py-1.5 rounded-full border border-[#8B5CF6]/30"
              >
                {name}
                <button
                  type="button"
                  onClick={() => removeArtist(name)}
                  className="text-[#8B5CF6] hover:text-white transition-colors leading-none"
                  aria-label={`Remove artist ${name}`}
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        )}

        {/* Input + autocomplete */}
        <div className="relative">
          <input
            type="text"
            value={artistInput}
            onChange={(e) => {
              setArtistInput(e.target.value);
              setShowSuggestions(true);
            }}
            onFocus={() => setShowSuggestions(true)}
            onBlur={() => setTimeout(() => setShowSuggestions(false), 150)}
            onKeyDown={handleArtistKeyDown}
            placeholder="Search or type a new artist name, then press Enter…"
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              errors.artists
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />

          {/* Suggestions dropdown */}
          {showSuggestions && filteredSuggestions.length > 0 && (
            <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
              {filteredSuggestions.map((a) => (
                <li key={a.id}>
                  <button
                    type="button"
                    onMouseDown={() => addArtist(a.name)}
                    className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                  >
                    {a.name}
                  </button>
                </li>
              ))}
              {artistInput.trim() &&
                !ARTISTS.some(
                  (a) => a.name.toLowerCase() === artistInput.toLowerCase()
                ) && (
                  <li>
                    <button
                      type="button"
                      onMouseDown={() => addArtist(artistInput)}
                      className="w-full text-left px-4 py-2.5 text-sm text-[#A78BFA] hover:bg-[#8B5CF6]/20 transition-colors border-t border-[#1C1C28]"
                    >
                      + Add &quot;{artistInput}&quot; as new artist
                    </button>
                  </li>
                )}
            </ul>
          )}
        </div>

        <p className="mt-1.5 text-xs text-gray-600">
          Search existing artists or type a new name and press Enter
        </p>
        {errors.artists && (
          <p className="mt-1 text-sm text-red-400" role="alert">
            {errors.artists}
          </p>
        )}
      </div>
    </div>
  );
}
