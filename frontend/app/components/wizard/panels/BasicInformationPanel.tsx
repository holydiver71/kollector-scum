"use client";

import { useState } from "react";
import type { WizardFormData, ValidationErrors, LookupItem } from "../types";
import type { ReleaseLookups } from "../useReleaseLookups";

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field in this panel changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors to display */
  errors: ValidationErrors;
  /** Real lookup data from the API */
  lookups: ReleaseLookups;
}

/**
 * A combined display item representing either an existing artist (with id) or
 * a new artist name to be created (without id).
 */
interface SelectedArtist {
  id?: number;
  name: string;
}

/**
 * Build the initial selected-artist list from form data and lookup items.
 * Existing artists (those with IDs) are resolved to their full LookupItem.
 * New artists (names only) are included as-is.
 */
function buildInitialArtists(
  artistIds: number[],
  artistNames: string[],
  allArtists: LookupItem[]
): SelectedArtist[] {
  const byId = artistIds
    .map((id) => allArtists.find((a) => a.id === id))
    .filter((a): a is LookupItem => a !== undefined)
    .map((a) => ({ id: a.id, name: a.name }));
  const byName = artistNames.map((name) => ({ name }));
  return [...byId, ...byName];
}

/**
 * Panel 1 – Basic Information.
 * Collects the release title and one or more contributing artists.
 * This is the only required panel; Next is blocked until both fields are filled.
 * Artists can be selected from the real database via autocomplete or entered
 * as free text to create a new artist on submission.
 */
export default function BasicInformationPanel({ data, onChange, errors, lookups }: Props) {
  const [artistInput, setArtistInput] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);

  // Combined display list of selected artists (existing by ID + new by name)
  const [selectedArtists, setSelectedArtists] = useState<SelectedArtist[]>(() =>
    buildInitialArtists(data.artistIds, data.artistNames, lookups.artists)
  );

  const filteredSuggestions = artistInput.trim()
    ? lookups.artists
        .filter(
          (a) =>
            a.name.toLowerCase().includes(artistInput.toLowerCase()) &&
            !selectedArtists.some((s) => s.name === a.name)
        )
        .slice(0, 8)
    : [];

  /** Sync the combined display list back to the form data */
  const syncToForm = (items: SelectedArtist[]) => {
    onChange({
      artistIds: items.filter((i) => i.id !== undefined).map((i) => i.id!),
      artistNames: items.filter((i) => i.id === undefined).map((i) => i.name),
    });
  };

  const addArtist = (item: SelectedArtist) => {
    if (selectedArtists.some((s) => s.name === item.name)) return;
    const next = [...selectedArtists, item];
    setSelectedArtists(next);
    setArtistInput("");
    setShowSuggestions(false);
    syncToForm(next);
  };

  const removeArtist = (name: string) => {
    const next = selectedArtists.filter((a) => a.name !== name);
    setSelectedArtists(next);
    syncToForm(next);
  };

  const handleArtistKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const trimmed = artistInput.trim();
      if (!trimmed) return;
      // Prefer an exact match from the DB lookup
      const match = lookups.artists.find(
        (a) => a.name.toLowerCase() === trimmed.toLowerCase()
      );
      if (match) {
        addArtist({ id: match.id, name: match.name });
      } else if (filteredSuggestions.length > 0) {
        // Accept first suggestion
        addArtist({ id: filteredSuggestions[0].id, name: filteredSuggestions[0].name });
      } else {
        // New artist
        addArtist({ name: trimmed });
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
          htmlFor="wiz-title"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Title <span className="text-red-400">*</span>
        </label>
        <input
          id="wiz-title"
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
        {selectedArtists.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-3">
            {selectedArtists.map((artist) => (
              <span
                key={artist.name}
                className="inline-flex items-center gap-1.5 bg-[#8B5CF6]/20 text-[#A78BFA] text-sm px-3 py-1.5 rounded-full border border-[#8B5CF6]/30"
              >
                {artist.name}
                {!artist.id && (
                  <span className="text-[10px] text-[#8B5CF6]/60 ml-0.5">(new)</span>
                )}
                <button
                  type="button"
                  onClick={() => removeArtist(artist.name)}
                  className="text-[#8B5CF6] hover:text-white transition-colors leading-none"
                  aria-label={`Remove artist ${artist.name}`}
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
          {showSuggestions && (filteredSuggestions.length > 0 || (artistInput.trim() && !lookups.artists.some((a) => a.name.toLowerCase() === artistInput.toLowerCase()))) && (
            <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
              {filteredSuggestions.map((a) => (
                <li key={a.id}>
                  <button
                    type="button"
                    onMouseDown={() => addArtist({ id: a.id, name: a.name })}
                    className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                  >
                    {a.name}
                  </button>
                </li>
              ))}
              {artistInput.trim() &&
                !lookups.artists.some(
                  (a) => a.name.toLowerCase() === artistInput.toLowerCase()
                ) && (
                  <li>
                    <button
                      type="button"
                      onMouseDown={() => addArtist({ name: artistInput.trim() })}
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
