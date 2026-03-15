/**
 * mapDiscogsRelease
 *
 * Converts a full Discogs release payload into:
 *  - `formData`    – a `Partial<CreateMusicReleaseDto>` suitable for prefilling
 *                   the manual release wizard or submitting directly to the API.
 *  - `sourceImages`– the original Discogs image URLs so they can be downloaded
 *                   and stored locally after the release is saved.
 *
 * This is the single source of truth for the Discogs → local mapping.
 * Both the "Add to Collection" and "Edit Release" code paths must call this
 * function so they cannot diverge.
 */

import type { DiscogsRelease } from "../../../lib/discogs-types";
import type { CreateMusicReleaseDto } from "../../AddReleaseForm";

// ─── Helpers ──────────────────────────────────────────────────────────────────

/**
 * Sanitises a string so it can be used as part of a filename.
 * Replaces any non-alphanumeric character with a dash, collapses runs of
 * dashes, and trims leading/trailing dashes.
 */
export function sanitizeFilename(str: string): string {
  return str
    .replace(/[^a-z0-9]/gi, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");
}

/**
 * Generates a deterministic image filename for a release.
 * Format: `{artist}-{title}-{year}.jpg` (or `{artist}-{title}.jpg` when year
 * is unknown).
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
 * Parses a Discogs duration string (`"M:SS"` or `"H:MM:SS"`) into a total
 * number of seconds.  Returns `undefined` for empty or malformed input.
 */
export function parseDuration(duration: string): number | undefined {
  if (!duration) return undefined;
  const parts = duration.split(":").map((p) => parseInt(p, 10));
  if (parts.some(isNaN)) return undefined;
  if (parts.length === 2) return parts[0] * 60 + parts[1];
  if (parts.length === 3) return parts[0] * 3600 + parts[1] * 60 + parts[2];
  return undefined;
}

/**
 * Extracts the bare filename from a URL path such as
 * `"http://localhost:5072/api/images/covers/Artist-Album-2020.jpg"`.
 */
export function extractFilenameFromUrl(url: string): string {
  const parts = url.split("/");
  return parts[parts.length - 1];
}

// ─── Main export ──────────────────────────────────────────────────────────────

export interface MapDiscogsReleaseResult {
  /** Form DTO ready to POST or prefill the manual wizard. */
  formData: Partial<CreateMusicReleaseDto>;
  /** Original Discogs image URLs for background download after save. */
  sourceImages: { cover: string | null; thumbnail: string | null };
}

/**
 * Maps a full Discogs release payload to the internal DTO and image metadata.
 *
 * @param release - Full release data returned by the Discogs API.
 * @returns `{ formData, sourceImages }`
 */
export function mapDiscogsRelease(
  release: DiscogsRelease
): MapDiscogsReleaseResult {
  // Artists
  const artistNames = release.artists?.map((a) => a.name) ?? [];

  // Genres – combine genres and styles
  const genreNames = [
    ...(release.genres ?? []),
    ...(release.styles ?? []),
  ];

  // Label
  const labelName = release.labels?.[0]?.name;
  const labelNumber = release.labels?.[0]?.catalogNumber;

  // Country
  const countryName = release.country;

  // Format
  const formatName = release.formats?.[0]?.name;

  // Barcode
  const barcodeIdentifier = release.identifiers?.find(
    (id) => id.type.toLowerCase() === "barcode"
  );
  const upc = barcodeIdentifier?.value;

  // Tracklist → media
  const media = release.tracklist?.length
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

  // Images
  let images: CreateMusicReleaseDto["images"] | undefined;
  let sourceImageUrl: string | null = null;
  let sourceThumbnailUrl: string | null = null;

  if (release.images?.[0]) {
    const primaryArtist = release.artists?.[0]?.name ?? "Unknown";
    const filename = generateImageFilename(
      primaryArtist,
      release.title,
      release.year
    );
    const thumbnailFilename = `thumb-${filename}`;

    sourceImageUrl = release.images[0].uri;
    sourceThumbnailUrl =
      release.images[0].uri150 ?? release.images[0].uri;

    images = {
      coverFront: filename,
      thumbnail: thumbnailFilename,
    };
  }

  const formData: Partial<CreateMusicReleaseDto> = {
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

  return {
    formData,
    sourceImages: { cover: sourceImageUrl, thumbnail: sourceThumbnailUrl },
  };
}
