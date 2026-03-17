"use client";

import { useState } from "react";
import type { WizardFormData } from "../types";
import { FormatIcon } from "../../FormatIcon";
import ConfirmDialog from "../ConfirmDialog";

interface Props {
  /** Current wizard form data to preview */
  data: WizardFormData;
  /** Navigate back to the previous panel */
  onGoBack: () => void;
  /** Called when the user confirms and clicks Save Release */
  onSubmit?: () => void;
  /** Called when the user cancels the entire flow */
  onCancel?: () => void;
  /** True while the API call is in flight */
  isSubmitting?: boolean;
  /** Error message returned from the API, if any */
  submitError?: string | null;
  /**
   * Optional custom action buttons rendered in the sticky footer.
   * When provided, replaces the default Back / Save Release buttons.
   */
  actions?: React.ReactNode;
}

// ─── Helper functions ─────────────────────────────────────────────────────────

/** Format seconds as M:SS or H:MM:SS */
function formatDuration(seconds?: number): string | null {
  if (!seconds || seconds === 0) return null;
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  if (h > 0) {
    return `${h}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  }
  return `${m}:${s.toString().padStart(2, "0")}`;
}

/** Sum the total duration across all media */
function totalSeconds(data: WizardFormData): number {
  return (data.media ?? [])
    .flatMap((d) => d.tracks)
    .reduce((acc, t) => acc + (t.lengthSecs ?? 0), 0);
}

/** Format a numeric price with currency symbol */
function formatPrice(price?: number, currency?: string): string | null {
  if (price === undefined || price === null) return null;
  const symbols: Record<string, string> = {
    GBP: "£",
    USD: "$",
    EUR: "€",
    JPY: "¥",
    CAD: "CA$",
    AUD: "A$",
  };
  const symbol = symbols[currency ?? "GBP"] ?? currency ?? "";
  return `${symbol}${price.toFixed(2)}`;
}

/** Infer a human-readable service name from a URL + type pair */
function inferServiceLabel(url: string, type: string): string {
  if (type && type !== "Other") return type;
  const lower = url.toLowerCase();
  if (lower.includes("discogs.com")) return "Discogs";
  if (lower.includes("spotify.com")) return "Spotify";
  if (lower.includes("youtube.com") || lower.includes("youtu.be")) return "YouTube";
  if (lower.includes("bandcamp.com")) return "Bandcamp";
  if (lower.includes("soundcloud.com")) return "SoundCloud";
  if (lower.includes("musicbrainz.org")) return "MusicBrainz";
  try {
    return new URL(url).hostname.replace(/^www\./, "");
  } catch {
    return "Link";
  }
}

// ─── Sub-components ───────────────────────────────────────────────────────────

/** A simple key-value row for the metadata cards */
function MetaRow({ label, value }: { label: string; value: React.ReactNode }) {
  if (!value) return null;
  return (
    <div className="flex justify-between items-start text-sm py-0.5">
      <span className="text-gray-500 flex-shrink-0 mr-3">{label}</span>
      <span className="text-white font-medium text-right break-words max-w-[65%]">
        {value}
      </span>
    </div>
  );
}

/** Cover art placeholder or real image */
function CoverArt({
  images,
  title,
}: {
  images: WizardFormData["images"];
  title: string;
}) {
  const src = images?.coverFront;
  const looksLikeUrl = src && (src.startsWith("http://") || src.startsWith("https://"));

  if (looksLikeUrl) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={src}
        alt={`${title} – front cover`}
        className="w-full h-full object-cover"
        onError={(e) => {
          (e.target as HTMLImageElement).style.display = "none";
        }}
      />
    );
  }

  return (
    <div className="w-full h-full flex flex-col items-center justify-center gap-2 text-gray-700">
      <svg
        className="w-16 h-16 opacity-30"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth={1}
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909M3.75 21h16.5M21 3.75H3A.75.75 0 002.25 4.5v13.5A.75.75 0 003 18.75h18a.75.75 0 00.75-.75V4.5a.75.75 0 00-.75-.75z"
        />
      </svg>
      {src && (
        <p className="text-xs text-center px-4 font-mono opacity-50 break-all">{src}</p>
      )}
    </div>
  );
}

// ─── Panel component ──────────────────────────────────────────────────────────

/**
 * Panel 8 – Draft Preview.
 *
 * Renders a visual preview of the release as it will appear in the collection
 * after saving. Explicitly labelled as an unsaved draft so users understand
 * this is a pre-save review step, not a persisted release.
 *
 * Key differences from the live release detail page:
 *  - Prominent "DRAFT PREVIEW" banner
 *  - Sticky footer bar with Back / Save Release actions
 *  - Save button shows a spinner during submission and an error if it fails
 *  - No Collection Data block, no Play History, no administrative actions
 *  - Purple dashed outline around the preview content area
 */
export default function DraftPreviewPanel({
  data,
  onGoBack,
  onSubmit,
  onCancel,
  isSubmitting = false,
  submitError,
  actions,
}: Props) {
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const total = totalSeconds(data);
  const priceStr = formatPrice(data.purchaseInfo.price, data.purchaseInfo.currency);
  const hasMedia = data.media.length > 0 && data.media.some((d) => d.tracks.length > 0);
  const hasLinks = data.links.length > 0;
  const hasPurchase =
    data.purchaseInfo.storeName ||
    data.purchaseInfo.price !== undefined ||
    data.purchaseInfo.purchaseDate ||
    data.purchaseInfo.notes;

  // Derive display artist names regardless of whether they are stored by ID or name
  const allArtistNames = data.artistDisplayNames?.length
    ? data.artistDisplayNames
    : data.artistNames ?? [];

  return (
    <>
    <div className="space-y-0">
      {/* ── Unsaved banner ─────────────────────────────────────────────── */}
      <div
        className="flex items-start gap-3 bg-amber-500/10 border border-amber-500/30 rounded-xl p-4 mb-6"
        role="status"
        aria-live="polite"
      >
        <svg
          className="w-5 h-5 text-amber-400 flex-shrink-0 mt-0.5"
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path
            fillRule="evenodd"
            d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z"
            clipRule="evenodd"
          />
        </svg>
        <div>
          <p className="text-sm font-semibold text-amber-300">
            Draft Preview — not yet saved
          </p>
          <p className="text-xs text-amber-400/70 mt-0.5">
            This is a preview of how your release will appear. Review the
            details below, then click{" "}
            <strong className="text-amber-300">Add to Collection</strong> at the
            bottom to save it to your collection.
          </p>
        </div>
      </div>

      {/* ── Main release layout ─────────────────────────────────────────── */}
      <div
        className="rounded-xl border-2 border-dashed border-[#8B5CF6]/30 p-4 md:p-6"
        aria-label="Draft release preview"
      >
        <div className="grid lg:grid-cols-3 gap-8">
          {/* ── Left column ─────────────────────────────────────────────── */}
          <div className="space-y-4">
            {/* Draft badge */}
            <div className="flex items-center justify-between mb-1">
              <span className="inline-block text-[10px] font-bold uppercase tracking-widest bg-[#8B5CF6]/20 text-[#A78BFA] border border-[#8B5CF6]/30 px-3 py-1 rounded-full">
                Draft Preview
              </span>
              <span className="text-[10px] text-gray-600 font-semibold uppercase tracking-wider">
                Not yet saved
              </span>
            </div>

            {/* Cover art */}
            <div className="aspect-square bg-[#13131F] rounded-2xl border border-[#1C1C28] overflow-hidden relative">
              <CoverArt images={data.images} title={data.title} />
            </div>

            {/* Greyed-out action buttons (visual only) */}
            <div className="flex gap-2 opacity-30 pointer-events-none select-none">
              <button className="flex-1 py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 bg-[#8B5CF6] text-white cursor-not-allowed">
                <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                  <path d="M8 5v14l11-7z" />
                </svg>
                Mark as Played
              </button>
              <div className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400">
                <svg
                  className="w-5 h-5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M4 6h16M4 10h16M4 14h16M4 18h16"
                  />
                </svg>
              </div>
            </div>
            <p className="text-[10px] text-gray-700 text-center -mt-1">
              Actions available after saving
            </p>

            {/* Release Info card */}
            <div className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] space-y-1.5">
              <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-3">
                Release Info
              </h3>
              <MetaRow label="Year" value={data.releaseYear || null} />
              {data.origReleaseYear && data.origReleaseYear !== data.releaseYear && (
                <MetaRow label="Orig. Year" value={data.origReleaseYear} />
              )}
              <MetaRow label="Format" value={data.formatName || null} />
              <MetaRow label="Packaging" value={data.packagingName || null} />
              <MetaRow label="Label" value={data.labelName || null} />
              <MetaRow label="Cat #" value={data.labelNumber || null} />
              <MetaRow label="Country" value={data.countryName || null} />
              <MetaRow label="Barcode" value={data.upc || null} />
              {total > 0 && <MetaRow label="Duration" value={formatDuration(total)} />}
            </div>

            {/* Purchase Info card (conditional) */}
            {hasPurchase && (
              <div className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] space-y-1.5">
                <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-3">
                  Purchase Info
                </h3>
                <MetaRow label="Store" value={data.purchaseInfo.storeName || null} />
                <MetaRow label="Price" value={priceStr} />
                {data.purchaseInfo.purchaseDate && (
                  <MetaRow
                    label="Date"
                    value={new Date(data.purchaseInfo.purchaseDate).toLocaleDateString(
                      "en-GB",
                      { year: "numeric", month: "short", day: "numeric" }
                    )}
                  />
                )}
                <MetaRow label="Notes" value={data.purchaseInfo.notes || null} />
              </div>
            )}
          </div>

          {/* ── Right column (2 cols) ────────────────────────────────────── */}
          <div className="lg:col-span-2 space-y-6">
            {/* Title area */}
            <div>
              {allArtistNames.length > 0 && (
                <p className="text-[#8B5CF6] font-black text-4xl tracking-tight leading-tight mb-1">
                  {allArtistNames.join(" & ")}
                </p>
              )}
              <div className="flex items-center gap-3">
                {data.formatName && (
                  <div className="flex-shrink-0" title={data.formatName}>
                    <FormatIcon formatName={data.formatName} className="w-10 h-10 drop-shadow-md" />
                  </div>
                )}
                <h1 className="text-3xl font-black tracking-tight leading-tight text-white">
                  {data.title || (
                    <span className="text-gray-700 italic">Untitled release</span>
                  )}
                </h1>
              </div>
              <div className="flex flex-wrap gap-2 mt-2 items-center">
                {data.live && (
                  <span className="text-xs bg-red-600/20 text-red-400 px-2 py-1 rounded font-semibold">
                    Live Recording
                  </span>
                )}
                {data.genreNames.map((g) => (
                  <span
                    key={g}
                    className="text-xs bg-[#8B5CF6]/15 text-[#A78BFA] px-2 py-1 rounded"
                  >
                    {g}
                  </span>
                ))}
              </div>
            </div>

            {/* Tracklist */}
            {hasMedia ? (
              <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
                <div className="px-4 py-3 border-b border-[#1C1C28]">
                  <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest">
                    Tracklist
                  </h3>
                </div>
                <div className="space-y-6 px-4 pb-4">
                  {data.media.map((disc, di) => (
                    <div key={di} className={di > 0 ? "mt-6" : "mt-4"}>
                      {data.media.length > 1 && (
                        <div className="flex items-center justify-between mb-3 pb-2 border-b border-[#1C1C28]">
                          <span className="text-xs uppercase tracking-widest text-gray-500 font-bold">
                            {disc.name || `Disc ${di + 1}`}
                          </span>
                          <span className="text-xs text-gray-600">
                            {disc.tracks.length} track
                            {disc.tracks.length !== 1 ? "s" : ""}
                          </span>
                        </div>
                      )}
                      {disc.tracks.map((track, ti) => (
                        <div
                          key={ti}
                          className="flex items-center gap-4 py-2 border-b border-[#1C1C28]/50 last:border-0"
                        >
                          <span className="w-6 text-xs text-gray-600 text-right font-mono flex-shrink-0">
                            {track.index}
                          </span>
                          <span className="flex-1 text-sm text-white font-medium">
                            {track.title || (
                              <span className="text-gray-700 italic">
                                Untitled track
                              </span>
                            )}
                          </span>
                          {track.lengthSecs ? (
                            <span className="text-xs text-gray-500 font-mono flex-shrink-0">
                              {formatDuration(track.lengthSecs)}
                            </span>
                          ) : null}
                        </div>
                      ))}
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-6 text-center">
                <p className="text-xs text-gray-600 uppercase tracking-wider font-semibold">
                  No tracks added
                </p>
              </div>
            )}

            {/* Links */}
            {hasLinks && (
              <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
                <div className="px-4 py-3 border-b border-[#1C1C28] flex items-center justify-between">
                  <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2">
                    <svg
                      width="14"
                      height="14"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      strokeWidth="2"
                      className="text-[#8B5CF6]"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                      />
                    </svg>
                    Links
                  </h3>
                  <span className="text-[10px] text-gray-600">
                    {data.links.length} link{data.links.length !== 1 ? "s" : ""}
                  </span>
                </div>
                <div className="divide-y divide-[#1C1C28]">
                  {data.links.map((link, i) => (
                    <div key={i} className="flex items-center gap-3 px-4 py-3">
                      <div className="w-8 h-8 rounded-lg bg-[#8B5CF6]/10 flex items-center justify-center flex-shrink-0">
                        <svg
                          className="w-4 h-4 text-[#A78BFA]"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                          strokeWidth={2}
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                          />
                        </svg>
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-white">
                          {inferServiceLabel(link.url, link.type)}
                        </p>
                        {link.description && (
                          <p className="text-xs text-gray-500 truncate">
                            {link.description}
                          </p>
                        )}
                        <p className="text-xs text-gray-700 font-mono truncate mt-0.5">
                          {link.url}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ── Sticky action footer ────────────────────────────────────────── */}
      <div className="sticky bottom-4 mt-8 flex flex-col gap-3">
        {/* API error banner */}
        {submitError && (
          <div
            className="flex items-start gap-3 bg-red-500/10 border border-red-500/30 rounded-xl p-4"
            role="alert"
          >
            <svg
              className="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                clipRule="evenodd"
              />
            </svg>
            <p className="text-sm text-red-300">{submitError}</p>
          </div>
        )}

        <div className="flex items-center gap-3 bg-[#13131F]/90 backdrop-blur border border-[#1C1C28] rounded-2xl px-5 py-4 shadow-2xl">
          <div className="flex items-center justify-between w-full gap-3">
            {actions ?? (
              <>
                <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={onGoBack}
                  disabled={isSubmitting}
                  className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#8B5CF6]/50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
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
                {onCancel && (
                  <button
                    type="button"
                    onClick={() => setShowCancelConfirm(true)}
                    disabled={isSubmitting}
                    className="px-3 py-2 text-sm text-gray-500 hover:text-gray-300 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Cancel
                  </button>
                )}
                </div>
                <button
                  type="button"
                  onClick={onSubmit}
                  disabled={isSubmitting}
                  className="flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold bg-emerald-600 hover:bg-emerald-500 disabled:bg-emerald-800 disabled:cursor-not-allowed text-white transition-colors shadow-lg shadow-emerald-900/30"
                >
                  {isSubmitting ? (
                    <>
                      <svg
                        className="w-4 h-4 animate-spin"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle
                          className="opacity-25"
                          cx="12"
                          cy="12"
                          r="10"
                          stroke="currentColor"
                          strokeWidth="4"
                        />
                        <path
                          className="opacity-75"
                          fill="currentColor"
                          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                        />
                      </svg>
                      Saving…
                    </>
                  ) : (
                    <>
                      <svg
                        className="w-4 h-4"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        strokeWidth={2.5}
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          d="M4.5 12.75l6 6 9-13.5"
                        />
                      </svg>
                      Add to Collection
                    </>
                  )}
                </button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
    <ConfirmDialog
      isOpen={showCancelConfirm}
      onConfirm={() => { setShowCancelConfirm(false); onCancel?.(); }}
      onDismiss={() => setShowCancelConfirm(false)}
    />
    </>
  );
}
