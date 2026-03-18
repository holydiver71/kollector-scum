"use client";

/**
 * DiscogsAddReleaseWizard
 *
 * Container component for the three-step Discogs import flow:
 *
 *  1. Search  – user enters catalogue number and optional filters
 *  2. Results – user selects one result from the list
 *  3. Details – full release preview; user can add to collection or hand off
 *               to the manual AddReleaseWizard for further editing
 *
 * The component manages the step state machine, enforcing the guards described
 * in `types.ts`.  When the user chooses "Edit Release" at step 3, the wizard
 * switches to the manual `AddReleaseWizard` with the Discogs data prefilled.
 */

import { useState } from "react";
import { fetchJson } from "../../../lib/api";
import type { DiscogsRelease, DiscogsSearchRequest, DiscogsSearchResult } from "../../../lib/discogs-types";
import { mapDiscogsRelease, extractFilenameFromUrl } from "./mapDiscogsRelease";
import type { DiscogsWizardState } from "./types";
import type { WizardStep } from "../types";
import StepIndicator from "../StepIndicator";
import DiscogsSearchStep from "./DiscogsSearchStep";
import DiscogsResultsStep from "./DiscogsResultsStep";
import DiscogsDetailsStep from "./DiscogsDetailsStep";
import AddReleaseWizard from "../AddReleaseWizard";

// ─── Types ─────────────────────────────────────────────────────────────────────

interface CreateMusicReleaseResponseDto {
  release: { id: number };
}

export interface DiscogsAddReleaseWizardProps {
  /** Called with the new release ID after a successful add or manual save. */
  onSuccess: (releaseId: number) => void;
  /** Called when the user cancels the entire flow. */
  onCancel: () => void;
}

// ─── Step definitions ──────────────────────────────────────────────────────────

const DISCOGS_STEPS: WizardStep[] = [
  { id: 0, title: "Search",  description: "Search Discogs by catalogue number",   required: false },
  { id: 1, title: "Results", description: "Select a release from search results", required: false },
  { id: 2, title: "Details", description: "Preview and add the release",          required: false },
];

const STEP_NAMES: DiscogsWizardState["step"][] = ["search", "results", "details"];
const STEP_INDEX: Record<DiscogsWizardState["step"], number> = {
  search: 0,
  results: 1,
  details: 2,
};

// ─── Initial state ─────────────────────────────────────────────────────────────

const INITIAL_STATE: DiscogsWizardState = {
  step: "search",
  searchRequest: null,
  searchResults: [],
  selectedResult: null,
  selectedRelease: null,
  mappedDraft: null,
  sourceImages: { cover: null, thumbnail: null },
};

// ─── Component ─────────────────────────────────────────────────────────────────

/**
 * Three-step Discogs import wizard that can hand off into the manual
 * AddReleaseWizard for additional editing.
 */
export default function DiscogsAddReleaseWizard({
  onSuccess,
  onCancel,
}: DiscogsAddReleaseWizardProps) {
  const [state, setState] = useState<DiscogsWizardState>(INITIAL_STATE);
  const [isAdding, setIsAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  // When true, renders the manual wizard instead of this wizard shell.
  const [editMode, setEditMode] = useState(false);

  // ── Search step handlers ────────────────────────────────────────────────────

  const handleSearchSuccess = (
    results: DiscogsSearchResult[],
    request: DiscogsSearchRequest
  ) => {
    setState((prev) => ({
      ...prev,
      step: "results",
      searchRequest: request,
      searchResults: results,
      selectedResult: null,
    }));
  };

  const handleSearchError = (_message: string) => {
    // Error is displayed inside DiscogsSearchStep; no state change needed here.
  };

  // ── Results step handlers ───────────────────────────────────────────────────

  const handleSelectResult = (result: DiscogsSearchResult) => {
    setState((prev) => ({ ...prev, selectedResult: result }));
  };

  const handleContinueToDetails = (result: DiscogsSearchResult) => {
    setState((prev) => ({
      ...prev,
      step: "details",
      selectedResult: result,
      selectedRelease: null, // will be fetched by DiscogsDetailsStep
    }));
  };

  const handleBackToSearch = () => {
    setState((prev) => ({ ...prev, step: "search" }));
  };

  const handleBackToResults = () => {
    setState((prev) => ({ ...prev, step: "results" }));
  };

  // ── Details step handlers ───────────────────────────────────────────────────

  /**
   * Maps the release, POSTs to the API, downloads images, then redirects.
   */
  const handleAddToCollection = async (release: DiscogsRelease) => {
    setIsAdding(true);
    setAddError(null);

    const { formData, sourceImages } = mapDiscogsRelease(release);

    // Convert year strings to ISO DateTime
    const cleanedData = {
      ...formData,
      releaseYear: formData.releaseYear
        ? new Date(parseInt(formData.releaseYear, 10), 0, 1).toISOString()
        : undefined,
      origReleaseYear: formData.origReleaseYear
        ? new Date(parseInt(formData.origReleaseYear, 10), 0, 1).toISOString()
        : undefined,
      artistIds: formData.artistIds?.length ? formData.artistIds : undefined,
      artistNames: formData.artistNames?.length ? formData.artistNames : undefined,
      genreIds: formData.genreIds?.length ? formData.genreIds : undefined,
      genreNames: formData.genreNames?.length ? formData.genreNames : undefined,
      links: formData.links?.length ? formData.links : undefined,
      media: formData.media?.length ? formData.media : undefined,
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

      // Download images in background (non-blocking)
      const downloads: Promise<void>[] = [];

      if (sourceImages.cover && cleanedData.images?.coverFront) {
        downloads.push(
          (async () => {
            try {
              await fetchJson("/api/images/download", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                  url: sourceImages.cover,
                  filename: extractFilenameFromUrl(cleanedData.images!.coverFront!),
                }),
                swallowErrors: true,
              });
            } catch {
              // Image download failures are non-fatal
            }
          })()
        );
      }

      if (sourceImages.thumbnail && cleanedData.images?.thumbnail) {
        downloads.push(
          (async () => {
            try {
              await fetchJson("/api/images/download", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                  url: sourceImages.thumbnail,
                  filename: extractFilenameFromUrl(cleanedData.images!.thumbnail!),
                }),
                swallowErrors: true,
              });
            } catch {
              // Image download failures are non-fatal
            }
          })()
        );
      }

      await Promise.allSettled(downloads);
      onSuccess(result.release.id);
    } catch (err) {
      const msg =
        err instanceof Error ? err.message : "Failed to add release";
      setAddError(msg);
    } finally {
      setIsAdding(false);
    }
  };

  /**
   * Maps the release data and switches into the manual AddReleaseWizard.
   */
  const handleEditRelease = (release: DiscogsRelease) => {
    const { formData, sourceImages } = mapDiscogsRelease(release);
    setState((prev) => ({
      ...prev,
      selectedRelease: release,
      mappedDraft: formData,
      sourceImages,
    }));
    setEditMode(true);
  };

  /**
   * Called by the manual wizard on successful save.  Downloads images stored
   * in state (if any) then delegates to the page-level onSuccess handler.
   */
  const handleManualSuccess = async (releaseId: number) => {
    const { sourceImages, mappedDraft } = state;
    const downloads: Promise<void>[] = [];

    if (sourceImages.cover && mappedDraft?.images?.coverFront) {
      downloads.push(
        (async () => {
          try {
            await fetchJson("/api/images/download", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                url: sourceImages.cover,
                filename: extractFilenameFromUrl(mappedDraft.images!.coverFront!),
              }),
              swallowErrors: true,
            });
          } catch {
            // non-fatal
          }
        })()
      );
    }

    if (sourceImages.thumbnail && mappedDraft?.images?.thumbnail) {
      downloads.push(
        (async () => {
          try {
            await fetchJson("/api/images/download", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                url: sourceImages.thumbnail,
                filename: extractFilenameFromUrl(mappedDraft.images!.thumbnail!),
              }),
              swallowErrors: true,
            });
          } catch {
            // non-fatal
          }
        })()
      );
    }

    await Promise.allSettled(downloads);
    onSuccess(releaseId);
  };

  // ── Edit mode ───────────────────────────────────────────────────────────────

  if (editMode && state.mappedDraft) {
    return (
      <AddReleaseWizard
        initialData={state.mappedDraft}
        onSuccess={handleManualSuccess}
        onCancel={() => setEditMode(false)}
      />
    );
  }

  // ── Step indicator helpers ──────────────────────────────────────────────────

  const currentStepIndex = STEP_INDEX[state.step];
  const visitedSteps = STEP_NAMES.slice(0, currentStepIndex + 1).map((_, i) => i);

  const handleDiscogsStepClick = (stepId: number) => {
    if (stepId < currentStepIndex) {
      setState((prev) => ({ ...prev, step: STEP_NAMES[stepId] }));
    }
  };

  // ── Step indicator label ────────────────────────────────────────────────────

  const stepLabels: Record<DiscogsWizardState["step"], string> = {
    search: "Step 1 of 3 – Search",
    results: "Step 2 of 3 – Select a Release",
    details: "Step 3 of 3 – View Details",
  };

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-4">
      {/* Step indicator – same component and wrapper as manual wizard */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl p-3">
        <StepIndicator
          steps={DISCOGS_STEPS}
          currentStep={currentStepIndex}
          visitedSteps={visitedSteps}
          onStepClick={handleDiscogsStepClick}
        />
      </div>

      {/* Panel card */}
      <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl overflow-hidden">
        {/* Card header */}
        <div className="px-6 py-3 border-b border-[#1C1C28]">
          <span className="text-[10px] font-bold uppercase tracking-widest text-gray-600">
            {stepLabels[state.step]}
          </span>
        </div>

        {/* Card body */}
        <div className="px-6 py-5">
          {/* Add-to-collection error */}
          {addError && (
            <div
              role="alert"
              className="mb-4 flex gap-3 bg-red-500/10 border border-red-500/30 rounded-xl p-4"
            >
              <p className="text-sm text-red-400">{addError}</p>
            </div>
          )}

          {state.step === "search" && (
            <DiscogsSearchStep
              initialValues={state.searchRequest ?? undefined}
              onSearchSuccess={handleSearchSuccess}
              onSearchError={handleSearchError}
              onBack={onCancel}
            />
          )}

          {state.step === "results" && (
            <DiscogsResultsStep
              results={state.searchResults}
              selectedResult={state.selectedResult}
              onSelectResult={handleSelectResult}
              onContinue={handleContinueToDetails}
              onBack={handleBackToSearch}
              onCancel={onCancel}
            />
          )}

          {state.step === "details" && state.selectedResult && (
            <DiscogsDetailsStep
              searchResult={state.selectedResult}
              onBack={handleBackToResults}
              onCancel={onCancel}
              onAddToCollection={handleAddToCollection}
              onEditRelease={handleEditRelease}
              isAdding={isAdding}
            />
          )}
        </div>
      </div>

      {/* Search-step footer: show Cancel action so parent callers can abort the flow */}
      {state.step === "search" && (
        <div className="pt-2">
          <div className="flex">
            <button
              type="button"
              onClick={onCancel}
              className="text-sm text-gray-500 hover:text-gray-300 underline underline-offset-2"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

    </div>
  );
}
