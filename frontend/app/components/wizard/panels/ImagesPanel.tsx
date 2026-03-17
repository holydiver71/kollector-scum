"use client";

import { useRef, useState } from "react";
import type { WizardFormData, ValidationErrors } from "../types";
import ImageSearchModal from "../ImageSearchModal";
import { fetchJson, API_BASE_URL } from "../../../lib/api";

// ─── Max upload size (5 MB) ───────────────────────────────────────────────────
const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;

const ALLOWED_IMAGE_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/gif",
  "image/webp",
  "image/bmp",
  "image/tiff",
]);

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors */
  errors: ValidationErrors;
}

// ─── Shared text-input image field ───────────────────────────────────────────

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
  hint,
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  onChange: (v: string) => void;
  error?: string;
  hint?: string;
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
          {hint && <p className="mt-1 text-xs text-gray-600">{hint}</p>}
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

// ─── Cover Front field with Search + Upload ───────────────────────────────────

interface CoverFrontFieldProps {
  value: string;
  onChange: (coverFront: string, thumbnail: string) => void;
  error?: string;
  defaultSearchQuery: string;
}

/**
 * Enhanced Cover Front picker with two modes:
 * 1. **Search Web** – opens `ImageSearchModal` pre-filled with release metadata.
 * 2. **Upload File** – triggers a hidden file input; validates size ≤5 MB and image type.
 *
 * On selection (either mode) both `coverFront` and `thumbnail` form fields are set.
 */
function CoverFrontField({
  value,
  onChange,
  error,
  defaultSearchQuery,
}: CoverFrontFieldProps) {
  const [searchOpen, setSearchOpen] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const looksLikeUrl = value.startsWith("http://") || value.startsWith("https://");

  /** Called when the user picks a result from the search modal. */
  const handleSearchSelect = async (imageUrl: string, thumbnailUrl: string) => {
    setSearchOpen(false);
    setUploadError(null);
    setUploading(true);
    try {
      // Download the full-resolution image and auto-generate a thumbnail server-side.
      const data = await fetchJson<{
        filename: string;
        thumbnailFilename?: string;
      }>(`/api/images/download?generateThumbnail=true`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: imageUrl }),
      });
      onChange(
        data.filename,
        data.thumbnailFilename ?? thumbnailUrl,
      );
    } catch {
      // Fall back: store the direct URLs (covers edge-cases / offline local dev)
      onChange(imageUrl, thumbnailUrl);
    } finally {
      setUploading(false);
    }
  };

  /** Called when the user picks a local file. */
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!e.target) return;
    // Reset so the same file can be re-selected
    e.target.value = "";

    if (!file) return;

    setUploadError(null);

    if (file.size > MAX_UPLOAD_BYTES) {
      setUploadError(`File is too large. Maximum allowed size is 5 MB.`);
      return;
    }
    if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
      setUploadError("Only image files (JPEG, PNG, GIF, WebP, BMP, TIFF) are accepted.");
      return;
    }

    setUploading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      // We use a raw fetch here because fetchJson doesn't handle multipart easily.
      const token =
        typeof window !== "undefined" ? localStorage.getItem("auth_token") : null;
      const res = await fetch(
        `${API_BASE_URL}/api/images/upload?generateThumbnail=true`,
        {
          method: "POST",
          headers: token ? { Authorization: `Bearer ${token}` } : {},
          body: formData,
        },
      );
      if (!res.ok) {
        const msg = await res.text().catch(() => "Upload failed.");
        throw new Error(msg);
      }
      const data: { filename: string; thumbnailFilename?: string } = await res.json();
      onChange(data.filename, data.thumbnailFilename ?? "");
    } catch (err: unknown) {
      setUploadError((err as Error).message ?? "Upload failed. Please try again.");
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
      <label className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2">
        Front Cover
      </label>

      <div className="flex gap-3 items-start">
        {/* Preview */}
        <div className="flex-shrink-0 w-16 h-16 rounded-lg bg-[#0F0F1A] border border-[#2A2A3C] flex items-center justify-center overflow-hidden">
          {value && looksLikeUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={value}
              alt="Front cover preview"
              className="w-full h-full object-cover"
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
              }}
            />
          ) : value ? (
            <svg
              className="w-6 h-6 text-[#8B5CF6]/70"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.5}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
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

        <div className="flex-1 space-y-2">
          {/* Stored filename display */}
          {value && (
            <p className="font-mono text-xs text-[#8B5CF6]/80 truncate">{value}</p>
          )}

          {/* Action buttons */}
          <div className="flex gap-2 flex-wrap">
            <button
              type="button"
              onClick={() => setSearchOpen(true)}
              disabled={uploading}
              className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-semibold bg-[#8B5CF6]/20 hover:bg-[#8B5CF6]/30 text-[#8B5CF6] border border-[#8B5CF6]/30 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M11 19a8 8 0 100-16 8 8 0 000 16z" />
              </svg>
              Search Web
            </button>

            <button
              type="button"
              onClick={() => fileRef.current?.click()}
              disabled={uploading}
              className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-semibold bg-[#1C1C28] hover:bg-[#2A2A3C] text-gray-300 hover:text-white border border-[#2A2A3C] transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
              </svg>
              Upload File
            </button>

            {value && (
              <button
                type="button"
                onClick={() => onChange("", "")}
                disabled={uploading}
                className="inline-flex items-center gap-1 px-3 py-2 rounded-lg text-xs font-medium text-gray-500 hover:text-red-400 transition-colors disabled:opacity-50"
              >
                Clear
              </button>
            )}
          </div>

          {/* Upload progress / error feedback */}
          {uploading && (
            <p className="text-xs text-[#8B5CF6] animate-pulse">Saving image…</p>
          )}
          {uploadError && (
            <p className="text-xs text-red-400" role="alert">{uploadError}</p>
          )}
          {error && (
            <p className="text-xs text-red-400" role="alert">{error}</p>
          )}
        </div>
      </div>

      {/* Hidden file input */}
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        className="hidden"
        aria-hidden="true"
        onChange={handleFileChange}
      />

      {/* Image search modal */}
      {searchOpen && (
        <ImageSearchModal
          defaultQuery={defaultSearchQuery}
          onSelect={handleSearchSelect}
          onClose={() => setSearchOpen(false)}
        />
      )}
    </div>
  );
}

// ─── Panel ────────────────────────────────────────────────────────────────────

/**
 * Panel 5 – Images (optional).
 *
 * - **Front Cover**: two-mode picker — "Search Web" (MusicBrainz + Cover Art Archive)
 *   and "Upload File". Both modes auto-generate a 300px thumbnail server-side.
 * - **Back Cover**: plain filename / URL text input (unchanged).
 * - **Thumbnail**: read-only note "Auto-generated from Cover Front"; text override retained.
 */
export default function ImagesPanel({ data, onChange, errors }: Props) {
  const images = data.images ?? {};

  const update = (key: keyof typeof images, value: string) => {
    onChange({ images: { ...images, [key]: value } });
  };

  /** Build the default search query from wizard metadata. */
  const searchQuery = [
    (data.artistDisplayNames ?? data.artistNames).join(", "),
    data.title,
    data.releaseYear,
  ]
    .filter(Boolean)
    .join(" ")
    .trim();

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Use{" "}
        <span className="text-gray-400 font-medium">Search Web</span> to find
        cover art from{" "}
        <a
          href="https://musicbrainz.org"
          target="_blank"
          rel="noopener noreferrer"
          className="text-[#8B5CF6]/70 hover:text-[#8B5CF6]"
        >
          MusicBrainz
        </a>{" "}
        &amp;{" "}
        <a
          href="https://coverartarchive.org"
          target="_blank"
          rel="noopener noreferrer"
          className="text-[#8B5CF6]/70 hover:text-[#8B5CF6]"
        >
          Cover Art Archive
        </a>
        , or{" "}
        <span className="text-gray-400 font-medium">Upload File</span> to use
        your own image (max 5 MB). Back cover and thumbnail are optional.
      </p>

      <div className="space-y-5">
        {/* Cover Front – enhanced picker */}
        <CoverFrontField
          value={images.coverFront ?? ""}
          onChange={(coverFront, thumbnail) =>
            onChange({
              images: {
                ...images,
                coverFront,
                // Only auto-fill thumbnail if the user hasn't set one manually
                thumbnail: images.thumbnail ? images.thumbnail : thumbnail,
              },
            })
          }
          error={errors.coverFront}
          defaultSearchQuery={searchQuery}
        />

        {/* Back Cover – plain text input */}
        <ImageField
          id="wiz-coverBack"
          label="Back Cover"
          value={images.coverBack ?? ""}
          placeholder="back-cover.jpg"
          onChange={(v) => update("coverBack", v)}
          error={errors.coverBack}
        />

        {/* Thumbnail – text override with auto-fill note */}
        <ImageField
          id="wiz-thumbnail"
          label="Thumbnail"
          value={images.thumbnail ?? ""}
          placeholder="thumbnail.jpg"
          onChange={(v) => update("thumbnail", v)}
          error={errors.thumbnail}
          hint="Auto-generated from Cover Front when searching or uploading."
        />
      </div>
    </div>
  );
}

