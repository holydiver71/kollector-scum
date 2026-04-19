"use client";

/**
 * DiscogsResultsStep
 *
 * Step 2 of the Discogs add-release wizard.  Renders the search results list
 * and requires the user to explicitly select one result before the Continue
 * button becomes active.  Selection is highlighted in the list but does not
 * immediately advance the wizard – the user must press Continue.
 *
 * A Back button returns to the search step without losing search criteria or
 * results.
 */

import { useState } from "react";
import Image from "next/image";
import type { DiscogsSearchResult, DiscogsRelease } from "../../../lib/discogs-types";
import { toDiscogsProxyUrl, getDiscogsRelease } from "../../../lib/api";
import { ConfirmDialog } from "../../ConfirmDialog";

export interface DiscogsResultsStepProps {
  /** Results from the previous search step. */
  results: DiscogsSearchResult[];
  /** The currently selected result (may be null). */
  selectedResult: DiscogsSearchResult | null;
  /** Called when the user clicks on a result card. */
  onSelectResult: (result: DiscogsSearchResult) => void;
  /** Called when the user confirms the selection and presses Continue. */
  onContinue: (result: DiscogsSearchResult) => void;
  /** Called when the user presses Back to return to the search step. */
  onBack: () => void;
  /** Called when the user cancels the entire flow and returns to the add release home. */
  onCancel: () => void;
}

/**
 * Step 2 of the Discogs import wizard – results list with required selection.
 */
export default function DiscogsResultsStep({
  results,
  selectedResult,
  onSelectResult,
  onContinue,
  onBack,
  onCancel,
}: DiscogsResultsStepProps) {
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [expandedData, setExpandedData] = useState<Record<number, DiscogsRelease | null>>({});
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [errorId, setErrorId] = useState<Record<number, string>>({});

  const handleToggleExpand = async (result: DiscogsSearchResult, stop?: boolean) => {
    const id = Number(result.id);
    if (expandedId === id) {
      setExpandedId(null);
      return;
    }

    setExpandedId(id);
    if (expandedData[id]) return;

    try {
      setLoadingId(id);
      setErrorId((s) => ({ ...s, [id]: "" }));
      const data = await getDiscogsRelease(id);
      setExpandedData((s) => ({ ...s, [id]: data }));
    } catch (err: any) {
      setErrorId((s) => ({ ...s, [id]: err?.message ?? "Failed to load details" }));
    } finally {
      setLoadingId((s) => (s === id ? null : s));
    }
  };
  const derivePublicDiscogsUrl = (resourceUrl?: string | null): string | undefined => {
    if (!resourceUrl) return undefined;
    // resourceUrl example: https://api.discogs.com/releases/25904050
    try {
      const parts = resourceUrl.split('/').filter(Boolean);
      const id = parts[parts.length - 1];
      if (!/^[0-9]+$/.test(id)) return undefined;
      return `https://www.discogs.com/release/${id}`;
    } catch {
      return undefined;
    }
  };
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);

  const handleSelectResult = (result: DiscogsSearchResult) => {
    const id = Number(result.id);
    // Collapse any expanded result that isn't the one being selected
    if (expandedId !== null && expandedId !== id) {
      setExpandedId(null);
    }
    onSelectResult(result);
  };

  const handleContinue = () => {
    if (selectedResult) {
      onContinue(selectedResult);
    }
  };

  return (
    <>
    <div className="space-y-4 text-[var(--theme-card-text)]">
      <div>
        <h2 className="text-xl font-black text-[var(--theme-card-text)] mb-1">
          Select a Release
        </h2>
        <p className="text-sm text-[var(--theme-card-text)]/70">
          {results.length} {results.length === 1 ? "match" : "matches"} found.
          Select a release to view its full details.
        </p>
      </div>

      {/* Results list */}
      <div className="space-y-2">
        {results.map((result) => {
          const isSelected = selectedResult?.id === result.id;
          const publicDiscogsUrl = result.uri ?? derivePublicDiscogsUrl(result.resourceUrl);
          return (
            <div
              key={result.id}
              className={`w-full text-left rounded-xl border p-4 transition-all ${
                isSelected
                  ? "border-[var(--theme-accent)] bg-[var(--theme-accent)]/10 shadow-[0_0_0_1px_var(--theme-accent)]"
                  : "border-[var(--theme-card-border)] bg-[var(--theme-body-bg-start)] hover:border-[var(--theme-accent)]/50 hover:bg-[var(--theme-accent)]/5"
              }`}
            >
              <div
                role="button"
                tabIndex={0}
                onClick={() => handleSelectResult(result)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    handleSelectResult(result);
                  }
                }}
                aria-pressed={isSelected}
                aria-label={`Select ${result.title}`}
                className="w-full text-left"
              >
              <div className="flex gap-4 items-start">
                {/* Thumbnail */}
                <div className="flex-shrink-0">
                  {result.thumbUrl || result.coverImageUrl ? (
                    <div className="relative w-16 h-16 bg-[var(--theme-sidebar-hover)] rounded-lg overflow-hidden">
                      <Image
                        src={toDiscogsProxyUrl(result.thumbUrl ?? result.coverImageUrl) ?? ""}
                        alt={`${result.title} cover`}
                        fill
                        sizes="64px"
                        className="object-cover"
                        onError={(e) => {
                          e.currentTarget.style.display = "none";
                        }}
                      />
                    </div>
                  ) : (
                    <div className="w-16 h-16 bg-[var(--theme-sidebar-hover)] rounded-lg flex items-center justify-center">
                      <svg
                        className="w-8 h-8 text-[var(--theme-card-text)]/45"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3"
                        />
                      </svg>
                    </div>
                  )}
                </div>

                {/* Details */}
                <div className="flex-1 min-w-0">
                  <p className="font-semibold text-[var(--theme-card-text)] truncate">
                    {result.title}
                  </p>
                  <p className="text-sm text-[var(--theme-card-text)]/80 truncate">
                    {result.artist}
                  </p>
                  <div className="mt-1 flex flex-wrap gap-x-4 gap-y-0.5 text-xs text-[var(--theme-card-text)]/65">
                    {result.format && <span>Format: {result.format}</span>}
                    {result.country && <span>Country: {result.country}</span>}
                    {result.year && <span>Year: {result.year}</span>}
                    {result.label && <span>Label: {result.label}</span>}
                    {result.catalogNumber && (
                      <span className="font-mono">
                        Cat#: {result.catalogNumber}
                      </span>
                    )}
                  </div>
                  {/* notes removed from search results */}
                </div>

                {/* External Discogs link */}
                <div className="flex-shrink-0 ml-2 mr-2">
                  {publicDiscogsUrl && (
                    <a
                      href={publicDiscogsUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      title={`Open ${result.title} on Discogs`}
                      onClick={(e) => e.stopPropagation()}
                      className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-[var(--theme-accent)]/20 border border-[var(--theme-accent)]/60 shadow-sm hover:bg-[var(--theme-accent)]/35 hover:border-[var(--theme-accent)] transition-colors"
                      aria-label={`Open ${result.title} on Discogs (opens in new tab)`}
                    >
                      {/* Official Discogs logo (favicon) */}
                      <img
                        src="https://www.discogs.com/favicon.ico"
                        alt="Discogs"
                        className="w-4 h-4 object-contain"
                        // prevent the click from selecting the card
                        onClick={(e) => e.stopPropagation()}
                      />
                    </a>
                  )}
                </div>

                {/* Expand details control */}
                <div className="flex-shrink-0 ml-1 mr-2">
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      void handleToggleExpand(result);
                    }}
                    aria-label={`Expand details for ${result.title}`}
                    className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-[var(--theme-accent)]/20 border border-[var(--theme-accent)]/60 shadow-sm hover:bg-[var(--theme-accent)]/35 hover:border-[var(--theme-accent)] transition-colors"
                    title="Expand details"
                  >
                    <svg
                      className={`w-4 h-4 text-[var(--theme-card-text)]/70 transition-transform ${expandedId === Number(result.id) ? "rotate-180" : ""}`}
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth={2.5}
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
                    </svg>
                  </button>
                </div>

                {/* Selection indicator */}
                <div
                  className={`flex-shrink-0 w-5 h-5 rounded-full border-2 mt-0.5 flex items-center justify-center transition-colors ${
                    isSelected
                      ? "border-[var(--theme-accent)] bg-[var(--theme-accent)]"
                      : "border-[var(--theme-card-text)]/50 bg-[var(--theme-card-text)]/5"
                  }`}
                >
                  {isSelected && (
                    <svg
                      className="w-3 h-3 text-white"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth={3}
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  )}
                </div>
              </div>
              </div>
            {/* Inline expandable details */}
            {expandedId === Number(result.id) && (
              <div className="mt-3 pt-3 text-[var(--theme-card-text)] border-t border-[var(--theme-card-border)]">
                {loadingId === Number(result.id) && (
                  <div className="py-6 text-center text-sm text-[var(--theme-card-text)]/70">Loading details…</div>
                )}

                {errorId[Number(result.id)] && (
                  <div className="py-4 text-sm text-red-300">{errorId[Number(result.id)]}</div>
                )}

                {expandedData[Number(result.id)] && (
                  <div className="flex flex-col md:flex-row gap-4">
                    {/* Left: Tracklist */}
                    <div className="md:w-2/3 bg-transparent">
                      <h4 className="font-semibold text-[var(--theme-card-text)] mb-2">Tracklist</h4>
                      <div className="bg-transparent rounded-md p-2">
                        <table className="w-full text-sm table-fixed">
                          <thead>
                            <tr className="text-left text-[var(--theme-card-text)]/75 text-xs">
                              <th className="w-20">Pos</th>
                              <th>Title</th>
                              <th className="w-24">Duration</th>
                            </tr>
                          </thead>
                          <tbody>
                            {expandedData[Number(result.id)]!.tracklist.map((t, i) => (
                              <tr key={i} className="border-t border-[var(--theme-card-border)]">
                                <td className="py-2 text-[var(--theme-card-text)] font-mono">{t.position}</td>
                                <td className="py-2 text-[var(--theme-card-text)] truncate">{t.title}</td>
                                <td className="py-2 text-[var(--theme-card-text)]/80">{t.duration}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>

                    {/* Right: Formats + Notes */}
                    <div className="md:w-1/3 bg-transparent">
                      <h4 className="font-semibold text-[var(--theme-card-text)] mb-2">Details</h4>
                      <div className="mb-4 text-sm text-[var(--theme-card-text)]">
                        {expandedData[Number(result.id)]!.formats.map((f, idx) => (
                          <div key={idx} className="mb-2">
                            <div className="font-medium">{f.name} {f.qty ? `×${f.qty}` : ""}</div>
                            {f.descriptions && f.descriptions.length > 0 && (
                              <div className="text-xs text-[var(--theme-card-text)]/75">{f.descriptions.join(", ")}</div>
                            )}
                          </div>
                        ))}
                      </div>

                      {expandedData[Number(result.id)]!.notes && (
                        <>
                          <h4 className="font-semibold text-[var(--theme-card-text)] mb-2">Notes</h4>
                          <div className="text-sm text-[var(--theme-card-text)] bg-[var(--theme-sidebar-hover)]/50 rounded p-2 whitespace-pre-wrap">
                            {expandedData[Number(result.id)]!.notes}
                          </div>
                        </>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}
            </div>
          );
        })}
      </div>

      {/* Inline status hint when no selection */}
      {!selectedResult && (
        <div role="status" className="text-sm text-[var(--theme-card-text)]/70">
          Select a release to continue
        </div>
      )}

      {/* Footer action bar */}
      <div className="flex items-center justify-between gap-4 pt-2 border-t border-[var(--theme-card-border)]">
        <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[var(--theme-card-border)] text-[var(--theme-card-text)]/85 hover:text-[var(--theme-card-text)] hover:border-[var(--theme-accent)]/50 transition-colors"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M15.75 19.5L8.25 12l7.5-7.5"
            />
          </svg>
          Back
        </button>
        <button
          type="button"
          onClick={() => setShowCancelConfirm(true)}
          className="px-3 py-2 text-sm text-[var(--theme-card-text)]/65 hover:text-[var(--theme-card-text)] transition-colors cursor-pointer"
        >
          Cancel
        </button>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={handleContinue}
            disabled={!selectedResult}
            className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-[var(--theme-accent)] hover:bg-[var(--theme-accent-hover)] text-white shadow-lg disabled:opacity-40 disabled:cursor-not-allowed transition-all"
          >
            Continue
            <svg
              className="w-4 h-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M8.25 4.5l7.5 7.5-7.5 7.5"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>
    <ConfirmDialog
      isOpen={showCancelConfirm}
      title="Discard changes?"
      message="Any data you've entered will be lost. This action cannot be undone."
      onConfirm={() => { setShowCancelConfirm(false); onCancel(); }}
      onCancel={() => setShowCancelConfirm(false)}
      isDangerous
    />
    </>
  );
}
