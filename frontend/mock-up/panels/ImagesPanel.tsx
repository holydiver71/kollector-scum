"use client";

import type { MockFormData, ValidationErrors } from "../types";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/** Labelled image filename input with an in-line preview placeholder */
function ImageField({
  id,
  label,
  value,
  placeholder,
  onChange,
  error,
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  onChange: (v: string) => void;
  error?: string;
}) {
  const looksLikeUrl =
    value.startsWith("http://") || value.startsWith("https://");

  return (
    <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <label htmlFor={id} className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
        {label}
      </label>
      <div className="flex gap-3 items-start">
        {/* Preview thumbnail */}
        <div className="flex-shrink-0 w-16 h-16 rounded-lg bg-[#0F0F1A] border border-[#2A2A3C] flex items-center justify-center overflow-hidden">
          {looksLikeUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={value}
              alt={label}
              className="w-full h-full object-cover"
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
              }}
            />
          ) : (
            <svg
              className="w-6 h-6 text-gray-700"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.5}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z"
              />
            </svg>
          )}
        </div>

        {/* Input */}
        <div className="flex-1">
          <input
            id={id}
            type="text"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 font-mono text-sm focus:outline-none focus:ring-1 transition-colors ${
              error
                ? "border-red-500 focus:ring-red-500"
                : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
            }`}
          />
          {error && (
            <p className="mt-1 text-sm text-red-400" role="alert">
              {error}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Panel 7 – Images (optional).
 * Accepts filename strings for front cover, back cover and thumbnail.
 * Displays a small preview box when the value looks like an absolute URL.
 */
export default function ImagesPanel({ data, onChange, errors }: Props) {
  const images = data.images ?? {};

  const update = (key: keyof typeof images, value: string) => {
    onChange({ images: { ...images, [key]: value } });
  };

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Enter the filename of each image as stored on the server (e.g.{" "}
        <span className="font-mono text-xs text-gray-400">
          iron-maiden-beast-1982.jpg
        </span>
        ). You can also paste a full URL for a quick preview. Images are optional.
      </p>

      <div className="space-y-5">
        <ImageField
          id="mock-coverFront"
          label="Front Cover"
          value={images.coverFront ?? ""}
          placeholder="front-cover.jpg"
          onChange={(v) => update("coverFront", v)}
          error={errors.coverFront}
        />
        <ImageField
          id="mock-coverBack"
          label="Back Cover"
          value={images.coverBack ?? ""}
          placeholder="back-cover.jpg"
          onChange={(v) => update("coverBack", v)}
          error={errors.coverBack}
        />
        <ImageField
          id="mock-thumbnail"
          label="Thumbnail"
          value={images.thumbnail ?? ""}
          placeholder="thumbnail.jpg"
          onChange={(v) => update("thumbnail", v)}
          error={errors.thumbnail}
        />
      </div>
    </div>
  );
}
