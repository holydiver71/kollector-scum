"use client";

/**
 * DiscogsAddReleaseWizard – orchestrates the three-step Discogs import flow.
 *
 * Steps:
 *   1. `search`  – user enters a catalogue number and optional filters.
 *   2. `results` – user selects one result from the Discogs list.
 *   3. `details` – user reviews the full release and chooses to add it directly
 *                  or open it in the manual edit wizard.
 *
 * When the user chooses "Add to Collection" the wizard:
 *   1. Posts the mapped DTO to `POST /api/musicreleases`.
 *   2. Triggers background image downloads in parallel.
 *   3. Calls `onSuccess(releaseId)` to redirect the page.
 *
 * When the user chooses "Edit Release" the wizard calls `onEditRelease` with
 * the mapped DTO and image metadata so the page can switch to `AddReleaseWizard`.
 */

import { useState } from "react";
import { fetchJson } from "../../../lib/api";
import DiscogsSearchStep from "./DiscogsSearchStep";
import DiscogsResultsStep from "./DiscogsResultsStep";
import DiscogsDetailsStep from "./DiscogsDetailsStep";
import {
  type DiscogsWizardState,
  type DiscogsWizardStep,
  EMPTY_DISCOGS_STATE,
} from "./types";
import type { DiscogsSearchRequest, DiscogsSearchResult } from "../../../lib/discogs-types";
import { downloadDiscogsImages, type MappedDiscogsRelease } from "./mapDiscogsRelease";
import type { CreateMusicReleaseDto } from "../../AddReleaseForm";
import type { MusicReleaseDto } from "../../../lib/types";
import { useReleaseLookups } from "../useReleaseLookups";

interface CreateMusicReleaseResponseDto {
  release: MusicReleaseDto;
}

/** Props for the Discogs add-release wizard container */
interface DiscogsAddReleaseWizardProps {
  /** Called with the new release ID after a successful add */
  onSuccess: (releaseId: number) => void;
  /**
   * Called when the user chooses to edit the Discogs data before saving.
   * The page should switch to `AddReleaseWizard` with the provided initial data.
   */
  onEditRelease: (
    initialData: Partial<CreateMusicReleaseDto>,
    sourceImages: { cover: string | null; thumbnail: string | null }
  ) => void;
  /** Called when the user wants to return to the source-selection screen */
  onCancel?: () => void;
}

/** Step indicator label map */
const STEP_LABELS: Record<DiscogsWizardStep, string> = {
  search: "Search",
  results: "Results",
  details: "Details",
};

const STEP_ORDER: DiscogsWizardStep[] = ["search", "results", "details"];

/** Discogs add-release wizard */
export default function DiscogsAddReleaseWizard({
  onSuccess,
  onEditRelease,
  onCancel,
}: DiscogsAddReleaseWizardProps) {
  const [step, setStep] = useState<DiscogsWizardStep>("search");
  const [state, setState] = useState<DiscogsWizardState>(EMPTY_DISCOGS_STATE);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [searchError, setSearchError] = useState<string | null>(null);

  // Fetch all lookup data upfront (needed for new-entity detection in step 3)
  const lookups = useReleaseLookups(3);

  // ── Step handlers ─────────────────────────────────────────────────────────

  const handleSearchSuccess = (
    results: DiscogsSearchResult[],
    request: DiscogsSearchRequest
  ) => {
    setState((prev) => ({
      ...prev,
      search: request,
      results,
      selectedResult: null,
    }));
    setSearchError(null);
    setStep("results");
  };

  const handleSearchError = (message: string) => {
    setSearchError(message);
  };

  const handleResultContinue = (result: DiscogsSearchResult) => {
    setState((prev) => ({ ...prev, selectedResult: result }));
    setStep("details");
  };

  const handleBackToSearch = () => {
    setSearchError(null);
    setStep("search");
  };

  const handleBackToResults = () => {
    setStep("results");
  };

  // ── Add to collection ─────────────────────────────────────────────────────

  const handleAddToCollection = async (mapped: MappedDiscogsRelease) => {
    setSubmitError(null);

    const { dto, sourceImages } = mapped;

    // Build the POST body – normalise year strings to ISO dates
    const cleanedData: Partial<CreateMusicReleaseDto> = {
      ...dto,
      releaseYear: dto.releaseYear
        ? new Date(parseInt(dto.releaseYear, 10), 0, 1).toISOString()
        : undefined,
      origReleaseYear: dto.origReleaseYear
        ? new Date(parseInt(dto.origReleaseYear, 10), 0, 1).toISOString()
        : undefined,
      artistIds: dto.artistIds?.length ? dto.artistIds : undefined,
      artistNames: dto.artistNames?.length ? dto.artistNames : undefined,
      genreIds: dto.genreIds?.length ? dto.genreIds : undefined,
      genreNames: dto.genreNames?.length ? dto.genreNames : undefined,
      links: dto.links?.length ? dto.links : undefined,
      media: dto.media?.length ? dto.media : undefined,
    };

    try {
      const result = await fetchJson<CreateMusicReleaseResponseDto>(
        "/api/musicreleases",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(cleanedData),
        }
      );

      // Trigger image downloads in background (errors are swallowed)
      await downloadDiscogsImages(sourceImages, cleanedData);

      onSuccess(result.release.id);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to add release";
      setSubmitError(message);
    }
  };

  // ── Edit release ──────────────────────────────────────────────────────────

  const handleEditRelease = (mapped: MappedDiscogsRelease) => {
    onEditRelease(mapped.dto, mapped.sourceImages);
  };

  // ── Derived ───────────────────────────────────────────────────────────────

  const currentStepIndex = STEP_ORDER.indexOf(step);

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-4">
      {/* Step indicator */}
      <div className="flex items-center gap-2">
        {STEP_ORDER.map((s, idx) => {
          const isCurrent = s === step;
          const isPast = idx < currentStepIndex;
          return (
            <div key={s} className="flex items-center gap-2">
              <div className="flex items-center gap-1.5">
                <div
                  className={`w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold transition-colors ${
                    isCurrent
                      ? "bg-[var(--theme-accent)] text-white"
                      : isPast
                      ? "bg-[var(--theme-accent)]/40 text-white"
                      : "bg-[#1C1C28] text-gray-600"
                  }`}
                >
                  {isPast ? (
                    <svg
                      className="w-3 h-3"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  ) : (
                    idx + 1
                  )}
                </div>
                <span
                  className={`text-xs font-semibold transition-colors ${
                    isCurrent ? "text-[var(--theme-foreground)]" : "text-gray-600"
                  }`}
                >
                  {STEP_LABELS[s]}
                </span>
              </div>
              {idx < STEP_ORDER.length - 1 && (
                <div
                  className={`h-px w-8 transition-colors ${
                    idx < currentStepIndex
                      ? "bg-[var(--theme-accent)]/40"
                      : "bg-[#1C1C28]"
                  }`}
                />
              )}
            </div>
          );
        })}
      </div>

      {/* Submit error */}
      {submitError && (
        <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4">
          <p className="text-sm text-red-400">{submitError}</p>
        </div>
      )}

      {/* Panel card */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl p-6">
        {step === "search" && (
          <>
            <DiscogsSearchStep
              initialValues={state.search}
              onSearchSuccess={handleSearchSuccess}
              onSearchError={handleSearchError}
              onSwitchToManual={onCancel}
            />
            {/* Show search error below the form */}
            {searchError && (
              <div className="mt-4 bg-yellow-600/10 border border-yellow-600/20 rounded-xl p-4">
                <div className="flex gap-3">
                  <svg
                    className="h-5 w-5 text-yellow-400 flex-shrink-0"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <p className="text-sm text-yellow-300">{searchError}</p>
                </div>
              </div>
            )}
          </>
        )}

        {step === "results" && (
          <DiscogsResultsStep
            results={state.results}
            initialSelection={state.selectedResult}
            onContinue={handleResultContinue}
            onBack={handleBackToSearch}
          />
        )}

        {step === "details" && state.selectedResult && (
          <DiscogsDetailsStep
            searchResult={state.selectedResult}
            onAddToCollection={handleAddToCollection}
            onEditRelease={handleEditRelease}
            onBack={handleBackToResults}
            existingArtists={lookups.artists.map((a) => a.name)}
            existingLabels={lookups.labels.map((l) => l.name)}
            existingGenres={lookups.genres.map((g) => g.name)}
            existingCountries={lookups.countries.map((c) => c.name)}
            existingFormats={lookups.formats.map((f) => f.name)}
          />
        )}
      </div>
    </div>
  );
}
