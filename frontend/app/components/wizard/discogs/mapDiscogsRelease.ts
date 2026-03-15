/**
 * Discogs release mapping utilities.
 *
 * Converts a raw `DiscogsRelease` API payload into the two artefacts needed
 * by the Discogs add-release wizard:
 *
 * 1. A `Partial<CreateMusicReleaseDto>` that can pre-populate the manual edit
 *    wizard or be submitted directly to `POST /api/musicreleases`.
 * 2. A `DiscogsSourceImages` object containing the original image URLs for
 *    the background download calls that follow a successful save.
 *
 * Keeping this logic in one place ensures the "Add to Collection" and
 * "Edit Release" paths always produce identical payloads.
 */

import type { DiscogsRelease } from "../../../lib/discogs-types";
import type { CreateMusicReleaseDto } from "../../AddReleaseForm";
import type { DiscogsSourceImages } from "./types";

// ─── Duration parsing ─────────────────────────────────────────────────────────

/**
 * Parse a Discogs duration string (e.g. "3:45" or "1:02:30") into seconds.
 * Returns `undefined` when the input is empty or cannot be parsed.
 */
export function parseDuration(duration: string): number | undefined {
  if (!duration || !duration.trim()) return undefined;
  const parts = duration.split(":").map((p) => parseInt(p, 10));
  if (parts.some((p) => isNaN(p))) return undefined;
  if (parts.length === 2) return parts[0] * 60 + parts[1];
  if (parts.length === 3) return parts[0] * 3600 + parts[1] * 60 + parts[2];
  return undefined;
}

// ─── Filename helpers ─────────────────────────────────────────────────────────

/**
 * Replace any character that is not a letter or digit with a dash,
 * collapse consecutive dashes, and strip leading/trailing dashes.
 */
export function sanitizeFilename(str: string): string {
  return str
    .replace(/[^a-z0-9]/gi, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");
}

/**
 * Build a deterministic image filename from release metadata.
 * Example: `the-beatles-abbey-road-1969.jpg`
 */
export function generateImageFilename(
  artist: string,
  title: string,
  year?: number
): string {
  const artistPart = sanitizeFilename(artist);
  const titlePart = sanitizeFilename(title);
  const yearPart = year ? `-${year}` : "";
  return `${artistPart}-${titlePart}${yearPart}.jpg`;
}

/**
 * Extract only the filename from a full API URL path.
 * e.g. `http://localhost:5072/api/images/covers/foo.jpg` → `foo.jpg`
 */
export function extractFilenameFromUrl(url: string): string {
  const parts = url.split("/");
  return parts[parts.length - 1];
}

// ─── Result type ──────────────────────────────────────────────────────────────

/** The two outputs produced by `mapDiscogsRelease`. */
export interface MappedDiscogsRelease {
  /** DTO ready for the edit wizard or for direct POST */
  dto: Partial<CreateMusicReleaseDto>;
  /** Original Discogs image URLs needed for post-save download */
  sourceImages: DiscogsSourceImages;
}

// ─── Main mapper ─────────────────────────────────────────────────────────────

/**
 * Map a full `DiscogsRelease` payload to the wizard DTO and image metadata.
 *
 * The returned `dto` uses local filenames (not full URLs) for images so that
 * the backend stores references to files it manages rather than external URLs.
 *
 * The returned `sourceImages` contains the original Discogs URLs for the
 * background download calls that run after a successful save.
 */
export function mapDiscogsRelease(release: DiscogsRelease): MappedDiscogsRelease {
  // Artists
  const artistNames = release.artists?.map((a) => a.name) ?? [];

  // Genres – combine Discogs genres and styles
  const genreNames = [
    ...(release.genres ?? []),
    ...(release.styles ?? []),
  ];

  // Label (first entry only)
  const labelName = release.labels?.[0]?.name;
  const labelNumber = release.labels?.[0]?.catalogNumber;

  // Country and format
  const countryName = release.country;
  const formatName = release.formats?.[0]?.name;

  // Barcode / UPC from identifiers
  const barcodeIdentifier = release.identifiers?.find(
    (id) => id.type.toLowerCase() === "barcode"
  );
  const upc = barcodeIdentifier?.value;

  // Tracklist → media array
  const media =
    release.tracklist && release.tracklist.length > 0
      ? [
          {
            name: "Disc 1",
            tracks: release.tracklist.map((track, index) => ({
              title: track.title,
              index: index + 1,
              lengthSecs: track.duration
                ? parseDuration(track.duration)
                : undefined,
            })),
          },
        ]
      : [];

  // Images – build local filenames; keep source URLs for later download
  let images: CreateMusicReleaseDto["images"] | undefined;
  let sourceImages: DiscogsSourceImages = { cover: null, thumbnail: null };

  if (release.images?.[0]) {
    const primaryArtist = release.artists?.[0]?.name ?? "Unknown";
    const filename = generateImageFilename(
      primaryArtist,
      release.title,
      release.year
    );
    const thumbnailFilename = `thumb-${filename}`;

    sourceImages = {
      cover: release.images[0].uri,
      thumbnail: release.images[0].uri150 ?? release.images[0].uri,
    };

    images = {
      coverFront: filename,
      thumbnail: thumbnailFilename,
    };
  }

  const dto: Partial<CreateMusicReleaseDto> = {
    title: release.title,
    releaseYear: release.year?.toString(),
    artistNames,
    artistIds: [],
    genreNames,
    genreIds: [],
    live: false,
    labelName,
    labelNumber,
    countryName,
    formatName,
    upc,
    images,
    media,
    links: release.uri
      ? [{ url: release.uri, type: "Discogs", description: "" }]
      : [],
  };

  return { dto, sourceImages };
}

// ─── Image download helper ────────────────────────────────────────────────────

import { fetchJson } from "../../../lib/api";

/**
 * Download a single image from `sourceUrl` and save it under `filename`.
 * Errors are swallowed so a failed image download never blocks the user.
 */
async function downloadImage(
  sourceUrl: string,
  filename: string
): Promise<void> {
  try {
    await fetchJson("/api/images/download", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url: sourceUrl, filename }),
      swallowErrors: true,
    });
  } catch {
    // Image downloads are best-effort; do not propagate errors
  }
}

/**
 * Trigger background downloads for both cover and thumbnail images.
 * Both downloads run in parallel; errors are silently ignored.
 *
 * @param sourceImages - Original Discogs image URLs
 * @param dto - The DTO that contains the local target filenames
 */
export async function downloadDiscogsImages(
  sourceImages: DiscogsSourceImages,
  dto: Partial<CreateMusicReleaseDto>
): Promise<void> {
  const promises: Promise<void>[] = [];

  if (sourceImages.cover && dto.images?.coverFront) {
    const filename = extractFilenameFromUrl(dto.images.coverFront);
    promises.push(downloadImage(sourceImages.cover, filename));
  }

  if (sourceImages.thumbnail && dto.images?.thumbnail) {
    const filename = extractFilenameFromUrl(dto.images.thumbnail);
    promises.push(downloadImage(sourceImages.thumbnail, filename));
  }

  await Promise.allSettled(promises);
}
