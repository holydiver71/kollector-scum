import type { CreateMusicReleaseDto } from "../AddReleaseForm";

/** Reusable lookup item for dropdowns */
export interface LookupItem {
  id: number;
  name: string;
}

/** A single track within a disc */
export interface WizardTrack {
  title: string;
  index: number;
  lengthSecs?: number;
}

/** A physical disc / medium */
export interface WizardMedia {
  name: string;
  tracks: WizardTrack[];
}

/** An external link */
export interface WizardLink {
  url: string;
  type: string;
  description: string;
}

/** Images for a release */
export interface WizardImages {
  coverFront?: string;
  coverBack?: string;
  thumbnail?: string;
}

/** Purchase details */
export interface WizardPurchaseInfo {
  storeId?: number;
  storeName?: string;
  price?: number;
  currency: string;
  purchaseDate?: string;
  notes?: string;
}

/**
 * Full wizard form state. Closely mirrors CreateMusicReleaseDto but:
 *  - releaseYear / origReleaseYear are stored as plain YYYY strings
 *  - purchaseInfo / images are always initialised (never undefined) to avoid null-checks in panels
 *  - links / media are always arrays
 */
export interface WizardFormData {
  // Basic Information (Step 0 – required)
  title: string;
  /** IDs of existing artists selected from the database */
  artistIds: number[];
  /** Names of brand-new artists to be created (not in DB) */
  artistNames: string[];
  /** Resolved display names for ALL artists (both by-ID and free-text) — used for preview only, not sent to API */
  artistDisplayNames: string[];

  // Release Information (Step 1)
  genreIds: number[];
  genreNames: string[];
  live: boolean;
  formatId?: number;
  formatName: string;
  packagingId?: number;
  packagingName: string;
  countryId?: number;
  countryName: string;

  // Label & Dates (Step 2)
  releaseYear: string;
  origReleaseYear: string;
  labelId?: number;
  labelName: string;
  labelNumber: string;
  upc: string;

  // Purchase Information (Step 3)
  purchaseInfo: WizardPurchaseInfo;

  // Images (Step 4)
  images: WizardImages;

  // Track Listing (Step 5)
  media: WizardMedia[];

  // External Links (Step 6)
  links: WizardLink[];
}

/** Configuration for a single wizard step */
export interface WizardStep {
  id: number;
  title: string;
  description: string;
  /** Whether this panel must pass validation before Next is enabled */
  required: boolean;
}

/** Validation errors keyed by field name */
export type ValidationErrors = Record<string, string>;

/** Wizard step definitions */
export const WIZARD_STEPS: WizardStep[] = [
  { id: 0, title: "Basic Information",    description: "Release title and contributing artists",                    required: true  },
  { id: 1, title: "Release Information",  description: "Format, packaging, country, genres and live recording",    required: false },
  { id: 2, title: "Label & Dates",        description: "Record label, catalogue number, barcode and release years", required: false },
  { id: 3, title: "Purchase Information", description: "Where and when you bought this release",                   required: false },
  { id: 4, title: "Images",              description: "Cover art front, back and thumbnail filenames",             required: false },
  { id: 5, title: "Track Listing",        description: "Add discs and tracks for this release",                    required: false },
  { id: 6, title: "External Links",       description: "Links to Discogs, Spotify and other services",            required: false },
  { id: 7, title: "Draft Preview",        description: "Review your release before adding to your collection",     required: false },
];

/** Empty form state used when starting a fresh entry */
export const EMPTY_FORM_DATA: WizardFormData = {
  title: "",
  artistIds: [],
  artistNames: [],
  artistDisplayNames: [],
  genreIds: [],
  genreNames: [],
  live: false,
  formatName: "",
  packagingName: "",
  countryName: "",
  releaseYear: "",
  origReleaseYear: "",
  labelName: "",
  labelNumber: "",
  upc: "",
  purchaseInfo: { currency: "GBP" },
  images: {},
  media: [],
  links: [],
};

// ─── Mapper ───────────────────────────────────────────────────────────────────

/**
 * Convert a YYYY year string to a backend-compatible ISO date string.
 * Handles bare years ("1982"), YYYY-MM-DD, and full ISO strings.
 */
function toBackendDate(value: string): string {
  if (!value.trim()) return value;
  if (/^\d{4}$/.test(value)) return `${value}-01-01T00:00:00.000Z`;
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return `${value}T00:00:00.000Z`;
  const d = new Date(value);
  if (!isNaN(d.getTime())) return d.toISOString();
  return value;
}

/**
 * Strip a full URL down to just the filename component, if needed.
 * Filenames are stored without the server path prefix.
 */
function toImageFilename(value?: string): string | undefined {
  if (!value) return undefined;
  try {
    const u = new URL(value);
    const parts = u.pathname.split("/").filter(Boolean);
    return parts.length ? parts[parts.length - 1] : value;
  } catch {
    return value.includes("/") ? value.split("/").pop() : value;
  }
}

/**
 * Maps WizardFormData to the CreateMusicReleaseDto shape expected by the API.
 *
 * Rules:
 * - Empty arrays / strings are stripped rather than sent as empty
 * - Purchase date and year fields are normalised to ISO date strings
 * - Image values that look like full URLs are stripped to filenames only
 * - storeId OR storeName is sent, never both
 */
export function toCreateDto(data: WizardFormData): CreateMusicReleaseDto {
  const dto: CreateMusicReleaseDto = {
    title: data.title,
    live: data.live,
    // Artist IDs and new names
    artistIds: data.artistIds,
    artistNames: data.artistNames.length ? data.artistNames : undefined,
    // Genre IDs and new names
    genreIds: data.genreIds,
    genreNames: data.genreNames.length ? data.genreNames : undefined,
    // Classification — send ID OR name, never both (backend validator rejects both)
    formatId: data.formatId,
    formatName: data.formatId ? undefined : (data.formatName || undefined),
    packagingId: data.packagingId,
    packagingName: data.packagingId ? undefined : (data.packagingName || undefined),
    countryId: data.countryId,
    countryName: data.countryId ? undefined : (data.countryName || undefined),
    // Label & Dates — same rule
    releaseYear: data.releaseYear ? toBackendDate(data.releaseYear) : undefined,
    origReleaseYear: data.origReleaseYear ? toBackendDate(data.origReleaseYear) : undefined,
    labelId: data.labelId,
    labelName: data.labelId ? undefined : (data.labelName || undefined),
    labelNumber: data.labelNumber || undefined,
    upc: data.upc || undefined,
    // Images
    images: (data.images.coverFront || data.images.coverBack || data.images.thumbnail)
      ? {
          coverFront: toImageFilename(data.images.coverFront),
          coverBack: toImageFilename(data.images.coverBack),
          thumbnail: toImageFilename(data.images.thumbnail),
        }
      : undefined,
    // Media
    media: data.media.length
      ? data.media.map((disc) => ({
          name: disc.name,
          tracks: disc.tracks.map((t) => ({
            title: t.title,
            index: t.index,
            lengthSecs: t.lengthSecs,
          })),
        }))
      : undefined,
    // Links
    links: data.links.length ? data.links : undefined,
    // Purchase info
    purchaseInfo: undefined,
  };

  // Only include purchaseInfo if something was filled in
  const pi = data.purchaseInfo;
  const hasPurchase =
    pi.storeId !== undefined ||
    pi.storeName ||
    pi.price !== undefined ||
    pi.purchaseDate ||
    pi.notes;

  if (hasPurchase) {
    dto.purchaseInfo = {
      // Send storeId OR storeName, not both
      storeId: pi.storeId,
      storeName: pi.storeId ? undefined : pi.storeName,
      price: pi.price,
      currency: pi.currency || "GBP",
      purchaseDate: pi.purchaseDate || undefined,
      notes: pi.notes || undefined,
    };
  }

  return dto;
}

/**
 * Adapts a Partial<CreateMusicReleaseDto> (e.g. prefilled from Discogs) into a
 * WizardFormData, preserving all supplied values and filling defaults for the rest.
 */
export function fromCreateDto(partial: Partial<CreateMusicReleaseDto>): WizardFormData {
  return {
    ...EMPTY_FORM_DATA,
    title: partial.title ?? "",
    artistIds: partial.artistIds ?? [],
    artistNames: partial.artistNames ?? [],
    artistDisplayNames: partial.artistNames ?? [],
    genreIds: partial.genreIds ?? [],
    genreNames: partial.genreNames ?? [],
    live: partial.live ?? false,
    formatId: partial.formatId,
    formatName: partial.formatName ?? "",
    packagingId: partial.packagingId,
    packagingName: partial.packagingName ?? "",
    countryId: partial.countryId,
    countryName: partial.countryName ?? "",
    // Extract YYYY from ISO dates if needed
    releaseYear: partial.releaseYear
      ? String(new Date(partial.releaseYear).getFullYear())
      : "",
    origReleaseYear: partial.origReleaseYear
      ? String(new Date(partial.origReleaseYear).getFullYear())
      : "",
    labelId: partial.labelId,
    labelName: partial.labelName ?? "",
    labelNumber: partial.labelNumber ?? "",
    upc: partial.upc ?? "",
    purchaseInfo: {
      currency: "GBP",
      ...partial.purchaseInfo,
    },
    images: partial.images ?? {},
    media: (partial.media ?? []).map((m) => ({
      name: m.name ?? "",
      tracks: (m.tracks ?? []).map((t) => ({
        title: t.title,
        index: t.index,
        lengthSecs: t.lengthSecs,
      })),
    })),
    links: (partial.links ?? []).map((l) => ({
      url: l.url,
      type: l.type,
      description: l.description ?? "",
    })),
  };
}
