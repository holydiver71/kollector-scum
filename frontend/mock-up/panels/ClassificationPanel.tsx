"use client";

import { useState } from "react";
import type { MockFormData, ValidationErrors } from "../types";
import { GENRES, FORMATS, PACKAGINGS, COUNTRIES } from "../fixtures";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/** A simple styled select for single-value lookups */
function LookupSelect({
  id,
  label,
  value,
  items,
  placeholder,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  items: { id: number; name: string }[];
  placeholder: string;
  onChange: (name: string) => void;
}) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
        {label}{" "}
        <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
      </label>
      <select
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
      >
        <option value="">{placeholder}</option>
        {items.map((item) => (
          <option key={item.id} value={item.name}>
            {item.name}
          </option>
        ))}
      </select>
    </div>
  );
}

/**
 * Panel 2 – Release Information.
 * Collects format, packaging, country, genres and a live recording toggle.
 */
export default function ClassificationPanel({ data, onChange, errors }: Props) {
  const [genreInput, setGenreInput] = useState("");
  const [showGenreSuggestions, setShowGenreSuggestions] = useState(false);

  const filteredGenres = genreInput.trim()
    ? GENRES.filter(
        (g) =>
          g.name.toLowerCase().includes(genreInput.toLowerCase()) &&
          !data.genreNames.includes(g.name)
      ).slice(0, 8)
    : GENRES.filter((g) => !data.genreNames.includes(g.name)).slice(0, 8);

  const addGenre = (name: string) => {
    const trimmed = name.trim();
    if (!trimmed || data.genreNames.includes(trimmed)) return;
    onChange({ genreNames: [...data.genreNames, trimmed] });
    setGenreInput("");
    setShowGenreSuggestions(false);
  };

  const removeGenre = (name: string) => {
    onChange({ genreNames: data.genreNames.filter((g) => g !== name) });
  };

  return (
    <div className="space-y-6">
      {/* Format, Packaging, Country */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <LookupSelect
          id="mock-format"
          label="Format"
          value={data.formatName}
          items={FORMATS}
          placeholder="Select format…"
          onChange={(name) => onChange({ formatName: name })}
        />
        <LookupSelect
          id="mock-packaging"
          label="Packaging"
          value={data.packagingName}
          items={PACKAGINGS}
          placeholder="Select packaging…"
          onChange={(name) => onChange({ packagingName: name })}
        />
        <LookupSelect
          id="mock-country"
          label="Country"
          value={data.countryName}
          items={COUNTRIES}
          placeholder="Select country…"
          onChange={(name) => onChange({ countryName: name })}
        />
      </div>
      </div>

      {/* Genres */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div>
        <label className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
          Genres{" "}
          <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">(optional)</span>
        </label>

        {data.genreNames.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-3">
            {data.genreNames.map((name) => (
              <span
                key={name}
                className="inline-flex items-center gap-1.5 bg-[#8B5CF6]/20 text-[#A78BFA] text-sm px-3 py-1.5 rounded-full border border-[#8B5CF6]/30"
              >
                {name}
                <button
                  type="button"
                  onClick={() => removeGenre(name)}
                  className="text-[#8B5CF6] hover:text-white transition-colors leading-none"
                  aria-label={`Remove genre ${name}`}
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        )}

        <div className="relative">
          <input
            type="text"
            value={genreInput}
            onChange={(e) => {
              setGenreInput(e.target.value);
              setShowGenreSuggestions(true);
            }}
            onFocus={() => setShowGenreSuggestions(true)}
            onBlur={() => setTimeout(() => setShowGenreSuggestions(false), 150)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                if (filteredGenres.length > 0) addGenre(filteredGenres[0].name);
                else if (genreInput.trim()) addGenre(genreInput);
              }
            }}
            placeholder="Search or add genres, then press Enter…"
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
              errors.genres
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />

          {showGenreSuggestions && filteredGenres.length > 0 && (
            <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
              {filteredGenres.map((g) => (
                <li key={g.id}>
                  <button
                    type="button"
                    onMouseDown={() => addGenre(g.name)}
                    className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                  >
                    {g.name}
                  </button>
                </li>
              ))}
              {genreInput.trim() &&
                !GENRES.some(
                  (g) => g.name.toLowerCase() === genreInput.toLowerCase()
                ) && (
                  <li>
                    <button
                      type="button"
                      onMouseDown={() => addGenre(genreInput)}
                      className="w-full text-left px-4 py-2.5 text-sm text-[#A78BFA] hover:bg-[#8B5CF6]/20 transition-colors border-t border-[#1C1C28]"
                    >
                      + Add &quot;{genreInput}&quot; as new genre
                    </button>
                  </li>
                )}
            </ul>
          )}
        </div>
      </div>
      </div>

      {/* Recording type segmented control */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <p className="text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-3">
          Recording Type
        </p>
        <div
          role="group"
          aria-label="Recording type"
          className="inline-flex w-full rounded-lg border border-[#2A2A3C] overflow-hidden"
        >
          <button
            type="button"
            onClick={() => onChange({ live: false })}
            className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-[#8B5CF6]/50 ${
              !data.live
                ? "bg-[#8B5CF6] text-white"
                : "bg-transparent text-gray-400 hover:text-white hover:bg-[#8B5CF6]/10"
            }`}
            aria-pressed={!data.live}
          >
            <svg className="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.75}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 9V4.5M9 9H4.5M9 9 3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5 5.25 5.25" />
            </svg>
            Studio Recording
          </button>
          <div className="w-px bg-[#2A2A3C]" />
          <button
            type="button"
            onClick={() => onChange({ live: true })}
            className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-[#8B5CF6]/50 ${
              data.live
                ? "bg-[#8B5CF6] text-white"
                : "bg-transparent text-gray-400 hover:text-white hover:bg-[#8B5CF6]/10"
            }`}
            aria-pressed={data.live}
          >
            <svg className="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.75}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 10.5l4.72-4.72a.75.75 0 0 1 1.28.53v11.38a.75.75 0 0 1-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 0 0 2.25-2.25v-9a2.25 2.25 0 0 0-2.25-2.25h-9A2.25 2.25 0 0 0 2.25 7.5v9a2.25 2.25 0 0 0 2.25 2.25z" />
            </svg>
            Live Recording
          </button>
        </div>
      </div>
    </div>
  );
}
