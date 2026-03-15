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
 * A simple styled select for single-value lookups that captures both the
 * lookup item ID and name when the selection changes.
 */
function LookupSelect({
  id,
  label,
  value,
  items,
  placeholder,
  onSelect,
}: {
  id: string;
  label: string;
  value: string;
  items: LookupItem[];
  placeholder: string;
  onSelect: (id: number | undefined, name: string) => void;
}) {
  return (
    <div>
      <label
        htmlFor={id}
        className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
      >
        {label}{" "}
        <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
          (optional)
        </span>
      </label>
      <select
        id={id}
        value={value}
        onChange={(e) => {
          const name = e.target.value;
          const item = items.find((i) => i.name === name);
          onSelect(item?.id, name);
        }}
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
 * A combined display item for multi-select genre fields.
 * Represents either an existing genre (with id) or a new genre name (without id).
 */
interface SelectedGenre {
  id?: number;
  name: string;
}

/** Build initial selected-genre list from form data and lookup items. */
function buildInitialGenres(
  genreIds: number[],
  genreNames: string[],
  allGenres: LookupItem[]
): SelectedGenre[] {
  const byId = genreIds
    .map((id) => allGenres.find((g) => g.id === id))
    .filter((g): g is LookupItem => g !== undefined)
    .map((g) => ({ id: g.id, name: g.name }));
  const byName = genreNames.map((name) => ({ name }));
  return [...byId, ...byName];
}

/**
 * Panel 2 – Release Information.
 * Collects format, packaging, country, genres and a live recording toggle.
 * All fields are optional; genre supports both existing DB entries and free text.
 */
export default function ClassificationPanel({ data, onChange, errors, lookups }: Props) {
  const [genreInput, setGenreInput] = useState("");
  const [showGenreSuggestions, setShowGenreSuggestions] = useState(false);

  const [selectedGenres, setSelectedGenres] = useState<SelectedGenre[]>(() =>
    buildInitialGenres(data.genreIds, data.genreNames, lookups.genres)
  );

  const filteredGenres = genreInput.trim()
    ? lookups.genres
        .filter(
          (g) =>
            g.name.toLowerCase().includes(genreInput.toLowerCase()) &&
            !selectedGenres.some((s) => s.name === g.name)
        )
        .slice(0, 8)
    : lookups.genres.filter((g) => !selectedGenres.some((s) => s.name === g.name)).slice(0, 8);

  /** Sync the combined genre display list back to form data */
  const syncGenresToForm = (items: SelectedGenre[]) => {
    onChange({
      genreIds: items.filter((i) => i.id !== undefined).map((i) => i.id!),
      genreNames: items.filter((i) => i.id === undefined).map((i) => i.name),
    });
  };

  const addGenre = (item: SelectedGenre) => {
    if (selectedGenres.some((s) => s.name === item.name)) return;
    const next = [...selectedGenres, item];
    setSelectedGenres(next);
    setGenreInput("");
    setShowGenreSuggestions(false);
    syncGenresToForm(next);
  };

  const removeGenre = (name: string) => {
    const next = selectedGenres.filter((g) => g.name !== name);
    setSelectedGenres(next);
    syncGenresToForm(next);
  };

  return (
    <div className="space-y-6">
      {/* Format, Packaging, Country */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <LookupSelect
            id="wiz-format"
            label="Format"
            value={data.formatName}
            items={lookups.formats}
            placeholder="Select format…"
            onSelect={(id, name) => onChange({ formatId: id, formatName: name })}
          />
          <LookupSelect
            id="wiz-packaging"
            label="Packaging"
            value={data.packagingName}
            items={lookups.packagings}
            placeholder="Select packaging…"
            onSelect={(id, name) => onChange({ packagingId: id, packagingName: name })}
          />
          <LookupSelect
            id="wiz-country"
            label="Country"
            value={data.countryName}
            items={lookups.countries}
            placeholder="Select country…"
            onSelect={(id, name) => onChange({ countryId: id, countryName: name })}
          />
        </div>
      </div>

      {/* Genres */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div>
          <label className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
            Genres{" "}
            <span className="ml-1 text-[10px] font-normal text-gray-600 normal-case tracking-normal">
              (optional)
            </span>
          </label>

          {selectedGenres.length > 0 && (
            <div className="flex flex-wrap gap-2 mb-3">
              {selectedGenres.map((genre) => (
                <span
                  key={genre.name}
                  className="inline-flex items-center gap-1.5 bg-[#8B5CF6]/20 text-[#A78BFA] text-sm px-3 py-1.5 rounded-full border border-[#8B5CF6]/30"
                >
                  {genre.name}
                  {!genre.id && (
                    <span className="text-[10px] text-[#8B5CF6]/60 ml-0.5">(new)</span>
                  )}
                  <button
                    type="button"
                    onClick={() => removeGenre(genre.name)}
                    className="text-[#8B5CF6] hover:text-white transition-colors leading-none"
                    aria-label={`Remove genre ${genre.name}`}
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
                  const trimmed = genreInput.trim();
                  if (!trimmed) return;
                  const match = lookups.genres.find(
                    (g) => g.name.toLowerCase() === trimmed.toLowerCase()
                  );
                  if (match) {
                    addGenre({ id: match.id, name: match.name });
                  } else if (filteredGenres.length > 0) {
                    addGenre({ id: filteredGenres[0].id, name: filteredGenres[0].name });
                  } else {
                    addGenre({ name: trimmed });
                  }
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
                      onMouseDown={() => addGenre({ id: g.id, name: g.name })}
                      className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                    >
                      {g.name}
                    </button>
                  </li>
                ))}
                {genreInput.trim() &&
                  !lookups.genres.some(
                    (g) => g.name.toLowerCase() === genreInput.toLowerCase()
                  ) && (
                    <li>
                      <button
                        type="button"
                        onMouseDown={() => addGenre({ name: genreInput.trim() })}
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
            <svg
              className="w-4 h-4 flex-shrink-0"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.75}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M9 9V4.5M9 9H4.5M9 9 3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5 5.25 5.25"
              />
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
            <svg
              className="w-4 h-4 flex-shrink-0"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.75}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M15.75 10.5l4.72-4.72a.75.75 0 0 1 1.28.53v11.38a.75.75 0 0 1-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 0 0 2.25-2.25v-9a2.25 2.25 0 0 0-2.25-2.25h-9A2.25 2.25 0 0 0 2.25 7.5v9a2.25 2.25 0 0 0 2.25 2.25z"
              />
            </svg>
            Live Recording
          </button>
        </div>
      </div>
    </div>
  );
}
