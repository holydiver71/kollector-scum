"use client";

import { useState, useRef, useEffect, useMemo } from "react";
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
 * A styled single-value combobox with creatable support.
 * Allows selecting from existing lookup items or typing a free-text new value,
 * matching the behaviour of the Artist and Genre fields.
 */
function CreatableLookupInput({
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
  const [inputValue, setInputValue] = useState(value);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const confirmedValue = useRef(value);

  /** Sync local display when the external value changes (e.g. editing an existing release). */
  useEffect(() => {
    setInputValue(value);
    confirmedValue.current = value;
  }, [value]);

  const filteredItems = inputValue.trim()
    ? items
        .filter((i) => i.name.toLowerCase().includes(inputValue.toLowerCase()))
        .slice(0, 8)
    : items.slice(0, 8);

  /** Whether the current typed value is not an exact match for any existing item. */
  const isNewValue =
    inputValue.trim() !== "" &&
    !items.some((i) => i.name.toLowerCase() === inputValue.trim().toLowerCase());

  const commit = (itemId: number | undefined, name: string) => {
    confirmedValue.current = name;
    setInputValue(name);
    setShowSuggestions(false);
    onSelect(itemId, name);
  };

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
      <div className="relative">
        <input
          id={id}
          type="text"
          value={inputValue}
          autoComplete="off"
          onChange={(e) => {
            const val = e.target.value;
            setInputValue(val);
            setShowSuggestions(true);
            if (!val) {
              confirmedValue.current = "";
              onSelect(undefined, "");
            }
          }}
          onFocus={() => setShowSuggestions(true)}
          onBlur={() =>
            setTimeout(() => {
              setShowSuggestions(false);
              // Revert to last committed value if the user typed but didn't confirm.
              setInputValue(confirmedValue.current);
            }, 150)
          }
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              setShowSuggestions(false);
              setInputValue(confirmedValue.current);
              return;
            }
            if (e.key === "Enter") {
              e.preventDefault();
              const trimmed = inputValue.trim();
              if (!trimmed) {
                commit(undefined, "");
                return;
              }
              const exact = items.find(
                (i) => i.name.toLowerCase() === trimmed.toLowerCase()
              );
              if (exact) {
                commit(exact.id, exact.name);
              } else if (filteredItems.length > 0) {
                // Auto-select the top suggestion on Enter (consistent with Genre field).
                commit(filteredItems[0].id, filteredItems[0].name);
              } else {
                // No existing match — treat as a new entry.
                commit(undefined, trimmed);
              }
            }
          }}
          placeholder={placeholder}
          className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
        />
        {showSuggestions && (filteredItems.length > 0 || isNewValue) && (
          <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden max-h-60 overflow-y-auto">
            {filteredItems.map((item) => (
              <li key={item.id}>
                <button
                  type="button"
                  onMouseDown={() => commit(item.id, item.name)}
                  className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                >
                  {item.name}
                </button>
              </li>
            ))}
            {isNewValue && (
              <li>
                <button
                  type="button"
                  onMouseDown={() => commit(undefined, inputValue.trim())}
                  className="w-full text-left px-4 py-2.5 text-sm text-[#A78BFA] hover:bg-[#8B5CF6]/20 transition-colors border-t border-[#1C1C28]"
                >
                  + Add &quot;{inputValue.trim()}&quot; as new {label.toLowerCase()}
                </button>
              </li>
            )}
          </ul>
        )}
      </div>
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
  const genresById = new Map(allGenres.map((g) => [g.id, g]));
  const byId = genreIds
    .map((id) => genresById.get(id))
    .filter((g): g is LookupItem => g !== undefined)
    .map((g) => ({ id: g.id, name: g.name }));
  const byName = genreNames.map((name) => ({ name }));
  return [...byId, ...byName];
}

/**
 * Resolve the display name for a lookup selection.
 * Prefers looking up by ID (reliable) when the map is populated; falls back to
 * the stored name string for free-text / custom entries.
 */
function resolveDisplayName(
  id: number | undefined,
  storedName: string,
  itemsById: Map<number, LookupItem>
): string {
  if (id !== undefined && itemsById.size > 0) {
    const match = itemsById.get(id);
    if (match) return match.name;
  }
  return storedName;
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

  // Build O(1) lookup Maps once per lookup list change instead of scanning
  // arrays on every render (resolveDisplayName previously called .find() 3×).
  const formatsById = useMemo(
    () => new Map(lookups.formats.map((f) => [f.id, f])),
    [lookups.formats]
  );
  const packagingsById = useMemo(
    () => new Map(lookups.packagings.map((p) => [p.id, p])),
    [lookups.packagings]
  );
  const countriesById = useMemo(
    () => new Map(lookups.countries.map((c) => [c.id, c])),
    [lookups.countries]
  );

  // Resolve display values from lookup IDs so pre-populated edits show
  // correctly even if the stored name string diverges from the lookup list.
  const displayFormatName = resolveDisplayName(data.formatId, data.formatName, formatsById);
  const displayPackagingName = resolveDisplayName(data.packagingId, data.packagingName, packagingsById);
  const displayCountryName = resolveDisplayName(data.countryId, data.countryName, countriesById);

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
          <CreatableLookupInput
            id="wiz-format"
            label="Format"
            value={displayFormatName}
            items={lookups.formats}
            placeholder="Search or add format…"
            onSelect={(id, name) => onChange({ formatId: id, formatName: name })}
          />
          <CreatableLookupInput
            id="wiz-packaging"
            label="Packaging"
            value={displayPackagingName}
            items={lookups.packagings}
            placeholder="Search or add packaging…"
            onSelect={(id, name) => onChange({ packagingId: id, packagingName: name })}
          />
          <CreatableLookupInput
            id="wiz-country"
            label="Country"
            value={displayCountryName}
            items={lookups.countries}
            placeholder="Search or add country…"
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

            {showGenreSuggestions && (filteredGenres.length > 0 || (genreInput.trim() && !lookups.genres.some((g) => g.name.toLowerCase() === genreInput.trim().toLowerCase()))) && (
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
                    (g) => g.name.toLowerCase() === genreInput.trim().toLowerCase()
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
