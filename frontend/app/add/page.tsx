"use client";

/**
 * Add Release page – source-selection flow switcher.
 *
 * Presents the user with two entry points:
 *   - Discogs Import → launches the Discogs add-release wizard
 *   - Manual Entry   → launches the existing AddReleaseWizard directly
 *
 * When the user chooses "Edit Release" inside the Discogs wizard the page
 * transitions to the manual wizard pre-populated with the Discogs data.
 */

import { useRouter } from "next/navigation";
import { useState } from "react";
import AddReleaseWizard from "../components/wizard/AddReleaseWizard";
import DiscogsAddReleaseWizard from "../components/wizard/discogs/DiscogsAddReleaseWizard";
import type { CreateMusicReleaseDto } from "../components/AddReleaseForm";
import { downloadDiscogsImages } from "../components/wizard/discogs/mapDiscogsRelease";

/** What the page is currently showing */
type ActiveFlow = "choose" | "discogs" | "manual";

/** Discogs image URLs preserved when switching from Discogs wizard to manual wizard */
interface PendingImages {
  cover: string | null;
  thumbnail: string | null;
}

export default function AddReleasePage() {
  const router = useRouter();
  const [flow, setFlow] = useState<ActiveFlow>("choose");
  const [showSuccess, setShowSuccess] = useState(false);
  const [newReleaseId, setNewReleaseId] = useState<number | null>(null);

  // When the user edits via the manual wizard after Discogs import we need
  // the original image URLs for post-save download.
  const [manualInitialData, setManualInitialData] = useState<
    Partial<CreateMusicReleaseDto> | undefined
  >(undefined);
  const [pendingImages, setPendingImages] = useState<PendingImages>({
    cover: null,
    thumbnail: null,
  });

  // ── Shared success handler ─────────────────────────────────────────────────

  const handleSuccess = (releaseId: number) => {
    setNewReleaseId(releaseId);
    setShowSuccess(true);
    setTimeout(() => {
      router.push(`/releases/${releaseId}`);
    }, 2000);
  };

  // ── Discogs wizard callbacks ───────────────────────────────────────────────

  const handleDiscogsSuccess = (releaseId: number) => {
    handleSuccess(releaseId);
  };

  const handleDiscogsEditRelease = (
    initialData: Partial<CreateMusicReleaseDto>,
    sourceImages: { cover: string | null; thumbnail: string | null }
  ) => {
    setManualInitialData(initialData);
    setPendingImages(sourceImages);
    setFlow("manual");
  };

  // ── Manual wizard callbacks ────────────────────────────────────────────────

  const handleManualSuccess = async (releaseId: number) => {
    // If we arrived here from a Discogs import, download images now
    if (pendingImages.cover || pendingImages.thumbnail) {
      await downloadDiscogsImages(pendingImages, manualInitialData ?? {});
      setPendingImages({ cover: null, thumbnail: null });
    }
    handleSuccess(releaseId);
  };

  const handleCancel = () => {
    router.push("/collection");
  };

  // ── Success screen ─────────────────────────────────────────────────────────

  if (showSuccess && newReleaseId) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-8">
        <div className="bg-[var(--theme-card-bg)] border border-emerald-600/30 rounded-2xl p-8 text-center">
          <svg
            className="mx-auto h-16 w-16 text-emerald-400 mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
          <h2 className="text-2xl font-black text-[var(--theme-foreground)] mb-2">
            Release Added Successfully!
          </h2>
          <p className="text-gray-400 mb-4">Redirecting to release details…</p>
          <button
            onClick={() => router.push(`/releases/${newReleaseId}`)}
            className="text-[var(--theme-accent)] hover:text-[var(--theme-accent-light)] underline transition-colors cursor-pointer"
          >
            View Now
          </button>
        </div>
      </div>
    );
  }

  // ── Manual wizard ──────────────────────────────────────────────────────────

  if (flow === "manual") {
    return (
      <div className="max-w-4xl mx-auto px-4 py-8 text-[var(--theme-foreground)] space-y-6">
        <div className="flex items-center gap-3">
          <button
            onClick={() => setFlow("choose")}
            className="text-gray-400 hover:text-[var(--theme-foreground)] transition-colors"
            aria-label="Back to source selection"
          >
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
                d="M15.75 19.5L8.25 12l7.5-7.5"
              />
            </svg>
          </button>
          <div>
            <h1 className="text-2xl font-black text-[var(--theme-foreground)]">
              Add Release
            </h1>
            <p className="text-gray-400 mt-0.5 text-sm">Manual entry</p>
          </div>
        </div>

        <AddReleaseWizard
          initialData={manualInitialData}
          onSuccess={handleManualSuccess}
          onCancel={handleCancel}
        />
      </div>
    );
  }

  // ── Discogs wizard ─────────────────────────────────────────────────────────

  if (flow === "discogs") {
    return (
      <div className="max-w-4xl mx-auto px-4 py-8 text-[var(--theme-foreground)] space-y-6">
        <div className="flex items-center gap-3">
          <button
            onClick={() => setFlow("choose")}
            className="text-gray-400 hover:text-[var(--theme-foreground)] transition-colors"
            aria-label="Back to source selection"
          >
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
                d="M15.75 19.5L8.25 12l7.5-7.5"
              />
            </svg>
          </button>
          <div>
            <h1 className="text-2xl font-black text-[var(--theme-foreground)]">
              Add Release
            </h1>
            <p className="text-gray-400 mt-0.5 text-sm">Discogs import</p>
          </div>
        </div>

        <DiscogsAddReleaseWizard
          onSuccess={handleDiscogsSuccess}
          onEditRelease={handleDiscogsEditRelease}
          onCancel={() => setFlow("choose")}
        />
      </div>
    );
  }

  // ── Source selection screen ────────────────────────────────────────────────

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 text-[var(--theme-foreground)] space-y-8">
      <div>
        <h1 className="text-2xl font-black text-[var(--theme-foreground)]">
          Add Release
        </h1>
        <p className="text-gray-400 mt-1 text-sm">
          How would you like to add a release to your collection?
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Discogs Import card */}
        <button
          onClick={() => setFlow("discogs")}
          className="group text-left bg-[var(--theme-card-bg)] border border-[var(--theme-card-border)] rounded-2xl p-6 hover:border-[var(--theme-accent)]/60 hover:bg-[var(--theme-accent)]/5 transition-all outline-none focus-visible:ring-2 focus-visible:ring-[var(--theme-accent)]"
        >
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-12 h-12 rounded-xl bg-[var(--theme-accent)]/15 flex items-center justify-center group-hover:bg-[var(--theme-accent)]/25 transition-colors">
              <svg
                className="w-6 h-6 text-[var(--theme-accent)]"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
              </svg>
            </div>
            <div>
              <h2 className="text-lg font-black text-[var(--theme-foreground)] group-hover:text-[var(--theme-accent)] transition-colors">
                Search Discogs
              </h2>
              <p className="text-sm text-gray-400 mt-1">
                Search by catalogue number and import release data automatically.
                Images, tracks, and metadata are prefilled for you.
              </p>
            </div>
          </div>
        </button>

        {/* Manual entry card */}
        <button
          onClick={() => setFlow("manual")}
          className="group text-left bg-[var(--theme-card-bg)] border border-[var(--theme-card-border)] rounded-2xl p-6 hover:border-[var(--theme-accent)]/60 hover:bg-[var(--theme-accent)]/5 transition-all outline-none focus-visible:ring-2 focus-visible:ring-[var(--theme-accent)]"
        >
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-12 h-12 rounded-xl bg-[var(--theme-accent)]/15 flex items-center justify-center group-hover:bg-[var(--theme-accent)]/25 transition-colors">
              <svg
                className="w-6 h-6 text-[var(--theme-accent)]"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                />
              </svg>
            </div>
            <div>
              <h2 className="text-lg font-black text-[var(--theme-foreground)] group-hover:text-[var(--theme-accent)] transition-colors">
                Manual Entry
              </h2>
              <p className="text-sm text-gray-400 mt-1">
                Enter all release details yourself, step by step. Use this for
                releases not on Discogs or when you want full control.
              </p>
            </div>
          </div>
        </button>
      </div>
    </div>
  );
}
