"use client";

import { useRef, useState, useCallback } from "react";
import type { WizardFormData, ValidationErrors } from "../types";
import ImageSearchModal from "../ImageSearchModal";

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors */
  errors: ValidationErrors;
}

const MAX_UPLOAD_BYTES = 5 * 1024 * 1024; // 5 MB
const ALLOWED_EXTENSIONS = new Set([".jpg", ".jpeg", ".png", ".webp", ".gif"]);

/** Response shape returned by `/api/images/download` and `/api/images/upload`. */
interface ImageStoreResult {
  filename: string;
  publicUrl: string;
  thumbnailFilename?: string;
  thumbnailPublicUrl?: string;
}

/**
 * Labelled image filename input with an in-line preview placeholder.
 * When the value looks like an absolute URL the image is rendered in the
 * preview box; otherwise a placeholder icon is shown.
 */
function ImageField({
  id,
  label,
  value,
  placeholder,
  onChange,
  error,
  note,
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  onChange: (v: string) => void;
  error?: string;
  note?: string;
}) {
  const looksLikeUrl =
    value.startsWith("http://") || value.startsWith("https://");

  return (
    <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <label
        htmlFor={id}
        className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
      >
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
          {note && <p className="mt-1 text-xs text-gray-500">{note}</p>}
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
 * Panel 5 – Images (optional).
 *
 * Cover Front: two-mode picker — "Search Web" (Google image search modal)
 * and "Upload File" (local file upload).  Either selection resizes the image
 * server-side, stores it, and auto-generates a 300px thumbnail.
 *
 * Back Cover and Thumbnail remain plain text inputs.
 */
export default function ImagesPanel({ data, onChange, errors }: Props) {
  const images = data.images ?? {};

  const update = (key: keyof typeof images, value: string) => {
    onChange({ images: { ...images, [key]: value } });
  };

  // ── Search modal state ──────────────────────────────────────────────────────
  const [searchModalOpen, setSearchModalOpen] = useState(false);

  // ── Upload state ────────────────────────────────────────────────────────────
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  // Build the default search query from artist + title + year
  const searchQuery = [
    data.artistDisplayNames?.[0] ?? data.artistNames?.[0] ?? "",
    data.title ?? "",
    data.releaseYear ?? "",
    "album cover",
  ]
    .filter(Boolean)
    .join(" ");

  // ── Search → download + thumbnail ──────────────────────────────────────────
  const handleSearchSelect = useCallback(
    async (imageUrl: string) => {
      try {
        const res = await fetch(
          `/api/images/download?generateThumbnail=true`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ url: imageUrl, filename: `cover-${Date.now()}.jpg` }),
          }
        );

        if (!res.ok) {
          const msg = await res.text().catch(() => "Download failed");
          console.error("Image download failed:", msg);
          return;
        }

        const result: ImageStoreResult = await res.json();
        onChange({
          images: {
            ...images,
            coverFront: result.publicUrl || result.filename,
            ...(result.thumbnailFilename
              ? { thumbnail: result.thumbnailPublicUrl || result.thumbnailFilename }
              : {}),
          },
        });
      } catch (err) {
        console.error("Error downloading image:", err);
      }
    },
    [images, onChange]
  );

  // ── Upload → resize + thumbnail ─────────────────────────────────────────────
  const handleFileChange = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      // Reset input so the same file can be re-selected
      e.target.value = "";

      if (!file) return;

      // Client-side validation — handle files with no dot in the name gracefully
      const dotIndex = file.name.lastIndexOf(".");
      const ext = dotIndex >= 0 ? "." + file.name.slice(dotIndex + 1).toLowerCase() : "";
      if (!ext || !ALLOWED_EXTENSIONS.has(ext)) {
        setUploadError(
          `File type not allowed (${ext || "unknown"}). Use .jpg, .jpeg, .png, .webp or .gif.`
        );
        return;
      }
      if (file.size > MAX_UPLOAD_BYTES) {
        setUploadError(`File is too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Maximum is 5 MB.`);
        return;
      }

      setUploadError(null);
      setUploading(true);

      try {
        const formData = new FormData();
        formData.append("file", file);

        const res = await fetch(`/api/images/upload?generateThumbnail=true`, {
          method: "POST",
          body: formData,
        });

        if (!res.ok) {
          const msg = await res.text().catch(() => "Upload failed");
          setUploadError(msg || "Upload failed");
          return;
        }

        const result: ImageStoreResult = await res.json();
        onChange({
          images: {
            ...images,
            coverFront: result.publicUrl || result.filename,
            ...(result.thumbnailFilename
              ? { thumbnail: result.thumbnailPublicUrl || result.thumbnailFilename }
              : {}),
          },
        });
      } catch (err) {
        setUploadError("Upload failed. Please try again.");
        console.error("Upload error:", err);
      } finally {
        setUploading(false);
      }
    },
    [images, onChange]
  );

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Use &ldquo;Search Web&rdquo; or &ldquo;Upload File&rdquo; to set the
        Cover Front image. Both options resize the image to fit within 1600px
        and auto-generate a thumbnail. Back Cover and Thumbnail can also be set
        manually.
      </p>

      <div className="space-y-5">
        {/* ── Cover Front (with picker) ─────────────────────────────────── */}
        <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28] space-y-3">
          <p className="text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70">
            Front Cover
          </p>

          {/* Picker buttons */}
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setSearchModalOpen(true)}
              disabled={uploading}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-semibold bg-[#8B5CF6] hover:bg-[#7C3AED] disabled:opacity-50 text-white transition-colors"
              data-testid="search-web-button"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 115 11a6 6 0 0112 0z" />
              </svg>
              Search Web
            </button>

            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploading}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-semibold border border-[#2A2A3C] hover:border-[#8B5CF6]/50 disabled:opacity-50 text-gray-300 hover:text-white transition-colors"
              data-testid="upload-file-button"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
              {uploading ? "Uploading…" : "Upload File"}
            </button>

            {/* Hidden file input */}
            <input
              ref={fileInputRef}
              type="file"
              accept=".jpg,.jpeg,.png,.webp,.gif"
              className="hidden"
              onChange={handleFileChange}
              data-testid="file-input"
            />
          </div>

          {/* Upload error */}
          {uploadError && (
            <p className="text-sm text-red-400" role="alert" data-testid="upload-error">
              {uploadError}
            </p>
          )}

          {/* Text input (manual override / shows selected file) */}
          <ImageField
            id="wiz-coverFront"
            label=""
            value={images.coverFront ?? ""}
            placeholder="front-cover.jpg or paste a URL"
            onChange={(v) => update("coverFront", v)}
            error={errors.coverFront}
          />
        </div>

        {/* ── Back Cover ──────────────────────────────────────────────────── */}
        <ImageField
          id="wiz-coverBack"
          label="Back Cover"
          value={images.coverBack ?? ""}
          placeholder="back-cover.jpg"
          onChange={(v) => update("coverBack", v)}
          error={errors.coverBack}
        />

        {/* ── Thumbnail ───────────────────────────────────────────────────── */}
        <ImageField
          id="wiz-thumbnail"
          label="Thumbnail"
          value={images.thumbnail ?? ""}
          placeholder="thumbnail.jpg"
          onChange={(v) => update("thumbnail", v)}
          error={errors.thumbnail}
          note="Auto-generated from Cover Front when using Search Web or Upload File."
        />
      </div>

      {/* Image search modal */}
      {searchModalOpen && (
        <ImageSearchModal
          defaultQuery={searchQuery}
          onSelect={handleSearchSelect}
          onClose={() => setSearchModalOpen(false)}
        />
      )}
    </div>
  );
}
