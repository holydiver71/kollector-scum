"use client";

/**
 * DiscogsDetailsStep
 *
 * Step 3 of the Discogs add-release wizard.  Fetches the full release payload
 * for the selected search result and presents a detailed preview using the
 * same DraftPreviewPanel layout as the manual add wizard.
 *
 * From this step the user can:
 *  - Go back to the results list (preserves selection)
 *  - Add directly to collection (maps data, POSTs, triggers image download)
 *  - Edit release (maps data into the manual AddReleaseWizard)
 */

import { useEffect, useMemo, useState } from "react";
import { getDiscogsRelease } from "../../../lib/api";
import ConfirmDialog from "../ConfirmDialog";
import type { DiscogsRelease, DiscogsSearchResult } from "../../../lib/discogs-types";
import { mapDiscogsRelease } from "./mapDiscogsRelease";
import { fromCreateDto } from "../types";
import DraftPreviewPanel from "../panels/DraftPreviewPanel";

export interface DiscogsDetailsStepProps {
  /** The result selected in step 2. */
  searchResult: DiscogsSearchResult;
  /** Called when the user presses Back. */
  onBack: () => void;
  /** Called when the user cancels the entire flow and returns to the add release home. */
  onCancel: () => void;
  /** Called with the full release payload when the user chooses Add to Collection. */
  onAddToCollection: (release: DiscogsRelease) => void;
  /** Called with the full release payload when the user chooses Edit Release. */
  onEditRelease: (release: DiscogsRelease) => void;
  /** Whether the add-to-collection action is in progress. */
  isAdding?: boolean;
}

/**
 * Step 3 of the Discogs import wizard – full release preview using the same
 * DraftPreviewPanel layout as the manual add wizard.
 */
export default function DiscogsDetailsStep({
  searchResult,
  onBack,
  onCancel,
  onAddToCollection,
  onEditRelease,
  isAdding = false,
}: DiscogsDetailsStepProps) {
  const [release, setRelease] = useState<DiscogsRelease | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const fetchRelease = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await getDiscogsRelease(searchResult.id);
        if (!cancelled) setRelease(data);
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error ? err.message : "Failed to load release details"
          );
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    fetchRelease();
    return () => { cancelled = true; };
  }, [searchResult.id]);

  // Map to WizardFormData, overriding coverFront with the real Discogs URI so
  // the preview shows actual cover art rather than the local filename.
  const wizardData = useMemo(() => {
    if (!release) return null;
    const { formData, sourceImages } = mapDiscogsRelease(release);
    const data = fromCreateDto(formData);
    if (sourceImages.cover) {
      data.images = { ...data.images, coverFront: sourceImages.cover };
    }
    return data;
  }, [release]);

  // ── Loading ────────────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16 gap-4">
        <svg
          className="animate-spin h-10 w-10 text-[#8B5CF6]"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-label="Loading release details"
          role="img"
        >
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
        </svg>
        <p className="text-sm text-gray-400">Loading release details…</p>
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────

  if (error) {
    return (
      <div className="space-y-4">
        <div role="alert" className="flex gap-3 bg-red-500/10 border border-red-500/30 rounded-xl p-4">
          <svg className="h-5 w-5 text-red-400 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
          <div>
            <p className="text-sm font-medium text-red-300">Error Loading Details</p>
            <p className="mt-1 text-sm text-red-400">{error}</p>
          </div>
        </div>
        <button type="button" onClick={onBack} className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 transition-colors">
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
          </svg>
          Back to Results
        </button>
      </div>
    );
  }

  if (!release || !wizardData) return null;

  // ── Custom footer actions ──────────────────────────────────────────────────

  const footerActions = (
    <>
      <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={onBack}
        disabled={isAdding}
        className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
      >
        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
        </svg>
        Back to Results
      </button>
      <button
        type="button"
        onClick={() => setShowCancelConfirm(true)}
        disabled={isAdding}
        className="px-3 py-2 text-sm text-gray-500 hover:text-gray-300 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Cancel
      </button>
      </div>
      <div className="flex gap-3">
      <button
        type="button"
        onClick={() => onEditRelease(release)}
        disabled={isAdding}
        className="px-5 py-2.5 rounded-xl text-sm font-semibold bg-[#7C3AED] hover:bg-[#6D28D9] disabled:opacity-40 disabled:cursor-not-allowed text-white transition-colors shadow-lg shadow-purple-900/30"
      >
        Edit Release
      </button>
      <button
        type="button"
        onClick={() => onAddToCollection(release)}
        disabled={isAdding}
        className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-emerald-600 hover:bg-emerald-500 disabled:bg-emerald-800 disabled:cursor-not-allowed text-white transition-colors shadow-lg shadow-emerald-900/30"
      >
        {isAdding ? (
          <>
            <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            Adding…
          </>
        ) : (
          <>
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
            </svg>
            Add to Collection
          </>
        )}
      </button>
      </div>
    </>
  );

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <>
      <DraftPreviewPanel
        data={wizardData}
        onGoBack={onBack}
        isSubmitting={isAdding}
        actions={footerActions}
      />
      <ConfirmDialog
        isOpen={showCancelConfirm}
        onConfirm={() => { setShowCancelConfirm(false); onCancel(); }}
        onDismiss={() => setShowCancelConfirm(false)}
      />
    </>
  );
}

