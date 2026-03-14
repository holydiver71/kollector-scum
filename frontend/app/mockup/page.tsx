"use client";

import AddReleaseWizard from "../../mock-up/AddReleaseWizard";

/**
 * Mock-up review page – Add Release Wizard.
 *
 * Route: /mockup
 *
 * This page hosts the guided manual add-release flow mock-up for design
 * review. It is NOT linked from the main application navigation and is
 * intended for internal review only. The wizard makes no API calls; all data
 * is local state seeded from the fixtures in frontend/mock-up/fixtures.ts.
 *
 * To view this page:
 *   1. Start the frontend dev server:  cd frontend && npm run dev
 *   2. Log in to the application.
 *   3. Navigate to:  http://localhost:3000/mockup
 */
export default function MockupPage() {
  return (
    <div className="min-h-screen bg-[var(--theme-body-bg-start,#0A0A10)] px-4 py-4">
      {/* ── Review banner ─────────────────────────────────────────────────── */}
      <div className="max-w-4xl mx-auto mb-3">
        <div className="flex items-center gap-3 bg-[#13131F] border border-[#8B5CF6]/30 rounded-xl px-5 py-3">
          <span className="flex-shrink-0 text-[10px] font-bold uppercase tracking-widest bg-[#8B5CF6]/20 text-[#A78BFA] border border-[#8B5CF6]/30 px-2.5 py-1 rounded-full">
            Mock-up
          </span>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-white">
              Add Release – Guided Flow (Design Review)
            </p>
            <p className="text-xs text-gray-500 mt-0.5">
              This is a design mock-up only. No data is persisted and no API
              calls are made. Source: <span className="font-mono">frontend/mock-up/</span>
            </p>
          </div>
          <div className="flex-shrink-0 text-[10px] text-gray-600 font-semibold uppercase tracking-wider hidden sm:block">
            Not for production
          </div>
        </div>
      </div>

      {/* ── Wizard ──────────────────────────────────────────────────────────── */}
      <AddReleaseWizard useSeedData={true} />

      {/* ── Footer ──────────────────────────────────────────────────────────── */}
      <div className="max-w-4xl mx-auto mt-10 pt-6 border-t border-[#1C1C28]">
        <p className="text-xs text-gray-700 text-center">
          Mock-up · Source: <span className="font-mono">frontend/mock-up/</span>
          &nbsp;·&nbsp; Route: <span className="font-mono">/mockup</span>
          &nbsp;·&nbsp; Not linked from app navigation
        </p>
      </div>
    </div>
  );
}
