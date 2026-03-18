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

// ─── Cover Front field with Search + Upload ───────────────────────────────────

interface CoverFrontFieldProps {
  value: string;
  onChange: (coverFront: string, thumbnail: string) => void;
  error?: string;
  defaultSearchQuery: string;
  defaultCatalogueNumber?: string;
}

/**
 * Enhanced Cover Front picker with two modes:
 * 1. **Search Web** – opens `ImageSearchModal` pre-filled with release metadata and optional catalogue number.
 * 2. **Upload File** – triggers a hidden file input; validates size ≤5 MB and image type.
 *
 * On selection (either mode) both `coverFront` and `thumbnail` form fields are set.
 */
function CoverFrontField({
  value,
  onChange,
  error,
  defaultSearchQuery,
  defaultCatalogueNumber,
}: CoverFrontFieldProps) {
  const [searchOpen, setSearchOpen] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  // Holds a fully-resolved URL for the preview image.
  // Separate from `value` (which stores the bare filename for the DB) so that
  // local-dev storage paths (/cover-art/{userId}/{uuid}.jpg) show correctly.
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const looksLikeUrl = value.startsWith("http://") || value.startsWith("https://");

  // Resolve a displayable URL: prefer the locally-tracked previewUrl (valid during
  // async upload), then derive from the stored value which may be a full URL, a
  // root-relative storage path (/cover-art/…), or a bare filename.
  const coverImageSrc = previewUrl
    ?? (value
      ? looksLikeUrl
        ? value
        : value.startsWith("/")
          ? `${API_BASE_URL}${value}`
          : `${API_BASE_URL}/api/images/${value}`
      : null);

  /** Called when the user picks a result from the search modal. */
  const handleSearchSelect = async (imageUrl: string, thumbnailUrl: string) => {
    setSearchOpen(false);
    setUploadError(null);
    // Show the CAA image immediately while the backend saves it.
    setPreviewUrl(imageUrl);
    setUploading(true);
    try {
      // Download the full-resolution image and auto-generate a thumbnail server-side.
      const data = await fetchJson<{
        filename: string;
        thumbnailFilename?: string;
        publicUrl?: string;
      }>(`/api/images/download?generateThumbnail=true`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: imageUrl }),
      });
      // Switch preview to the stored URL once available (works with local storage too).
      if (data.publicUrl) {
        setPreviewUrl(
          data.publicUrl.startsWith("http") ? data.publicUrl : `${API_BASE_URL}${data.publicUrl}`,
        );
      }
      // Store publicUrl (e.g. /cover-art/{userId}/uuid.jpg) as coverFront so the
      // value persists correctly on the Draft Preview step. toImageFilename() in
      // types.ts strips it back to the bare filename when building the save DTO.
      onChange(
        data.publicUrl ?? data.filename,
        data.thumbnailFilename ?? thumbnailUrl,
      );
    } catch (err: unknown) {
      // Fall back to storing the direct CAA URLs as a best-effort measure
      // (e.g., in offline/local dev or if the download endpoint is unavailable).
      console.warn("Image download endpoint unavailable, falling back to direct URLs", err);
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
      setUploadError(`File is too large. Maximum allowed size is ${MAX_UPLOAD_BYTES / 1024 / 1024} MB.`);
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
      const data: { filename: string; thumbnailFilename?: string; publicUrl?: string } = await res.json();
      if (data.publicUrl) {
        setPreviewUrl(
          data.publicUrl.startsWith("http") ? data.publicUrl : `${API_BASE_URL}${data.publicUrl}`,
        );
      }
      // Store publicUrl so the value is displayable on the Draft Preview step.
      onChange(data.publicUrl ?? data.filename, data.thumbnailFilename ?? "");
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

      {/* Large preview when an image has been selected */}
      {coverImageSrc ? (
        <div className="mb-4">
          <div className="w-44 h-44 rounded-xl bg-[#0F0F1A] border border-[#2A2A3C] overflow-hidden relative">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={coverImageSrc}
              alt="Front cover preview"
              className="w-full h-full object-cover"
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
              }}
            />
          </div>
          <p className="font-mono text-xs text-[#8B5CF6]/70 truncate mt-1.5 max-w-44">{value}</p>
        </div>
      ) : null}

      <div className="flex gap-3 items-start">
        {/* Small placeholder shown only when no image is selected */}
        {!coverImageSrc && (
          <div className="flex-shrink-0 w-16 h-16 rounded-lg bg-[#0F0F1A] border border-[#2A2A3C] flex items-center justify-center overflow-hidden">
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
          </div>
        )}

        <div className="flex-1 space-y-2">

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
                onClick={() => { onChange("", ""); setPreviewUrl(null); }}
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
          defaultCatalogueNumber={defaultCatalogueNumber}
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
        your own image (max 5 MB).
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
          defaultCatalogueNumber={data.labelNumber}
        />
      </div>
    </div>
  );
}

