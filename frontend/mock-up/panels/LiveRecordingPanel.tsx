"use client";

import type { MockFormData, ValidationErrors } from "../types";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/**
 * Panel 3 – Live Recording.
 * A single prominent toggle indicating whether this release is a live recording.
 * Kept as its own step to keep the concept clear and avoid burying it in Basic Info.
 */
export default function LiveRecordingPanel({ data, onChange }: Props) {
  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Mark this release as a live recording if it was captured at a live
        concert or event rather than recorded exclusively in a studio.
      </p>

      {/* Toggle card */}
      <button
        type="button"
        onClick={() => onChange({ live: !data.live })}
        className={`w-full flex items-center gap-5 p-5 rounded-xl border-2 transition-all text-left ${
          data.live
            ? "border-red-500/70 bg-red-600/10"
            : "border-[#1C1C28] bg-[#0F0F1A] hover:border-[#8B5CF6]/40"
        }`}
        aria-pressed={data.live}
      >
        {/* Icon */}
        <div
          className={`flex-shrink-0 w-12 h-12 rounded-xl flex items-center justify-center transition-all ${
            data.live ? "bg-red-600/20" : "bg-[#1C1C28]"
          }`}
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className={`w-6 h-6 transition-colors ${
              data.live ? "text-red-400" : "text-gray-500"
            }`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={1.5}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M15.75 10.5l4.72-4.72a.75.75 0 011.28.53v11.38a.75.75 0 01-1.28.53l-4.72-4.72M4.5 18.75h9a2.25 2.25 0 002.25-2.25v-9a2.25 2.25 0 00-2.25-2.25h-9A2.25 2.25 0 002.25 7.5v9a2.25 2.25 0 002.25 2.25z"
            />
          </svg>
        </div>

        {/* Text */}
        <div className="flex-1">
          <div
            className={`font-semibold text-base transition-colors ${
              data.live ? "text-red-300" : "text-white"
            }`}
          >
            Live Recording
          </div>
          <div className="text-sm text-gray-500 mt-0.5">
            {data.live
              ? "This release is marked as a live recording"
              : "This release is a studio recording (default)"}
          </div>
        </div>

        {/* Toggle pill */}
        <div
          className={`flex-shrink-0 w-12 h-6 rounded-full transition-colors relative ${
            data.live ? "bg-red-500" : "bg-[#1C1C28]"
          }`}
        >
          <div
            className={`absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-all ${
              data.live ? "left-6" : "left-0.5"
            }`}
          />
        </div>
      </button>

      {data.live && (
        <div
          className="flex items-start gap-3 bg-red-600/10 border border-red-500/30 rounded-lg p-4"
          role="status"
        >
          <svg
            className="w-4 h-4 text-red-400 flex-shrink-0 mt-0.5"
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fillRule="evenodd"
              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
              clipRule="evenodd"
            />
          </svg>
          <p className="text-sm text-red-300">
            A{" "}
            <span className="font-semibold text-red-400 uppercase tracking-wider text-xs">
              Live Recording
            </span>{" "}
            badge will appear on this release in your collection.
          </p>
        </div>
      )}
    </div>
  );
}
