"use client";

import { useCallback, useState } from "react";
import { fetchJson } from "../../lib/api";

/** Maximum allowed upload size (5 MB) */
export const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;

/** Accepted image MIME types for file uploads */
export const ALLOWED_IMAGE_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/gif",
  "image/webp",
  "image/bmp",
  "image/tiff",
]);

export interface UploadedImageData {
  filename: string;
  thumbnailFilename?: string;
  publicUrl?: string;
}

export interface UseImageUploadResult {
  /** True while an upload request is in-flight */
  uploading: boolean;
  /** Validation or network error message, or null if no error */
  uploadError: string | null;
  /** Upload a file; returns the server response or null on failure */
  upload: (file: File) => Promise<UploadedImageData | null>;
  /** Clear the current error message */
  clearError: () => void;
}

/**
 * Manages file upload state for cover art.
 * Validates file size and type before uploading via the centralised fetchJson
 * wrapper (which handles auth headers automatically).
 * FormData body is passed without a Content-Type header so the browser sets
 * the correct multipart/form-data boundary automatically.
 */
export function useImageUpload(): UseImageUploadResult {
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const upload = useCallback(async (file: File): Promise<UploadedImageData | null> => {
    setUploadError(null);

    if (file.size > MAX_UPLOAD_BYTES) {
      setUploadError(
        `File is too large. Maximum allowed size is ${MAX_UPLOAD_BYTES / 1024 / 1024} MB.`
      );
      return null;
    }
    if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
      setUploadError("Only image files (JPEG, PNG, GIF, WebP, BMP, TIFF) are accepted.");
      return null;
    }

    setUploading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      // No Content-Type header — the browser sets multipart/form-data with boundary
      const data = await fetchJson<UploadedImageData>(
        "/api/images/upload?generateThumbnail=true",
        { method: "POST", body: formData }
      );
      return data;
    } catch (err: unknown) {
      setUploadError((err as Error).message ?? "Upload failed. Please try again.");
      return null;
    } finally {
      setUploading(false);
    }
  }, []);

  const clearError = useCallback(() => setUploadError(null), []);

  return { uploading, uploadError, upload, clearError };
}
