"use client";

/**
 * AddReleasePage
 *
 * Entry point for adding a release to the collection.
 *
 * Renders a source-selection screen that lets the user choose between:
 *  - Discogs Import  → DiscogsAddReleaseWizard (3-step guided flow)
 *  - Manual Entry    → AddReleaseWizard         (8-step manual form)
 *
 * After a successful add the user is shown a brief confirmation then
 * redirected to the new release's detail page.
 */

import { useRouter } from "next/navigation";
import { useState } from "react";
import AddReleaseWizard from "../components/wizard/AddReleaseWizard";
import DiscogsAddReleaseWizard from "../components/wizard/discogs/DiscogsAddReleaseWizard";

type FlowChoice = "select" | "discogs" | "manual";

export default function AddReleasePage() {
  const router = useRouter();
  const [flow, setFlow] = useState<FlowChoice>("select");
  const [showSuccess, setShowSuccess] = useState(false);
  const [newReleaseId, setNewReleaseId] = useState<number | null>(null);

  const handleSuccess = (releaseId: number) => {
    setNewReleaseId(releaseId);
    setShowSuccess(true);
    setTimeout(() => {
      router.push(`/releases/${releaseId}`);
    }, 2000);
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

  // ── Page wrapper ───────────────────────────────────────────────────────────

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 text-[var(--theme-foreground)] space-y-6">
      <div>
        <h1 className="text-2xl font-black text-[var(--theme-foreground)]">
          Add Release
        </h1>
        <p className="text-gray-400 mt-1 text-sm">
          Add a new music release to your collection
        </p>
      </div>

      {/* ── Source selection ───────────────────────────────────────────────── */}
      {flow === "select" && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {/* Discogs import card */}
          <button
            type="button"
            onClick={() => setFlow("discogs")}
            className="group flex flex-col items-start gap-3 p-6 bg-[var(--theme-card-bg)] border border-[var(--theme-card-border)] rounded-2xl hover:border-[#8B5CF6]/60 hover:bg-[#8B5CF6]/5 transition-all text-left"
          >
            <div className="w-10 h-10 rounded-xl bg-[#8B5CF6]/15 border border-[#8B5CF6]/30 flex items-center justify-center group-hover:bg-[#8B5CF6]/25 transition-colors">
              <svg
                className="w-5 h-5 text-[#8B5CF6]"
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
              <p className="font-bold text-white text-base">
                Search Discogs
              </p>
              <p className="text-sm text-gray-500 mt-0.5">
                Find a release by catalogue number and import its details
                automatically.
              </p>
            </div>
            <span className="mt-auto text-xs font-semibold text-[#8B5CF6] group-hover:underline">
              Start Discogs import →
            </span>
          </button>

          {/* Manual entry card */}
          <button
            type="button"
            onClick={() => setFlow("manual")}
            className="group flex flex-col items-start gap-3 p-6 bg-[var(--theme-card-bg)] border border-[var(--theme-card-border)] rounded-2xl hover:border-[#8B5CF6]/60 hover:bg-[#8B5CF6]/5 transition-all text-left"
          >
            <div className="w-10 h-10 rounded-xl bg-[#8B5CF6]/15 border border-[#8B5CF6]/30 flex items-center justify-center group-hover:bg-[#8B5CF6]/25 transition-colors">
              <svg
                className="w-5 h-5 text-[#8B5CF6]"
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
              <p className="font-bold text-white text-base">Manual Entry</p>
              <p className="text-sm text-gray-500 mt-0.5">
                Enter all release details yourself using the step-by-step
                wizard.
              </p>
            </div>
            <span className="mt-auto text-xs font-semibold text-[#8B5CF6] group-hover:underline">
              Start manual entry →
            </span>
          </button>
        </div>
      )}

      {/* ── Discogs wizard ──────────────────────────────────────────────────── */}
      {flow === "discogs" && (
        <div className="space-y-3">
          <button
            type="button"
            onClick={() => setFlow("select")}
            className="flex items-center gap-1.5 text-xs text-gray-500 hover:text-gray-300 transition-colors"
          >
            <svg
              className="w-3.5 h-3.5"
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
            Change method
          </button>
          <DiscogsAddReleaseWizard
            onSuccess={handleSuccess}
            onCancel={handleCancel}
          />
        </div>
      )}

      {/* ── Manual wizard ───────────────────────────────────────────────────── */}
      {flow === "manual" && (
        <div className="space-y-3">
          <button
            type="button"
            onClick={() => setFlow("select")}
            className="flex items-center gap-1.5 text-xs text-gray-500 hover:text-gray-300 transition-colors"
          >
            <svg
              className="w-3.5 h-3.5"
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
            Change method
          </button>
          <AddReleaseWizard onSuccess={handleSuccess} onCancel={handleCancel} />
        </div>
      )}
    </div>
  );
}

