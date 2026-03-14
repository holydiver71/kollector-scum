/** Reusable lookup item for dropdowns */
export interface LookupItem {
  id: number;
  name: string;
}

/** A single track within a disc */
export interface MockTrack {
  title: string;
  index: number;
  lengthSecs?: number;
  artists?: string[];
}

/** A physical disc / medium */
export interface MockMedia {
  name: string;
  tracks: MockTrack[];
}

/** An external link */
export interface MockLink {
  url: string;
  type: string;
  description: string;
}

/** Images for a release */
export interface MockImages {
  coverFront?: string;
  coverBack?: string;
  thumbnail?: string;
}

/** Purchase details */
export interface MockPurchaseInfo {
  storeName?: string;
  price?: number;
  currency: string;
  purchaseDate?: string;
  notes?: string;
}

/** The full form data model, mirrors CreateMusicReleaseDto */
export interface MockFormData {
  /** Panel 0 – Basic Information */
  title: string;
  artistNames: string[];
  /** Panel 1 – Release Dates */
  releaseYear: string;
  origReleaseYear: string;
  /** Panel 2 – Live Recording */
  live: boolean;
  /** Panel 3 – Classification */
  genreNames: string[];
  formatName: string;
  packagingName: string;
  countryName: string;
  /** Panel 4 – Label Information */
  labelName: string;
  labelNumber: string;
  upc: string;
  /** Panel 5 – Purchase Information */
  purchaseInfo: MockPurchaseInfo;
  /** Panel 6 – Images */
  images: MockImages;
  /** Panel 7 – Track Listing */
  media: MockMedia[];
  /** Panel 8 – External Links */
  links: MockLink[];
}

/** Configuration for a single wizard step */
export interface WizardStep {
  id: number;
  title: string;
  description: string;
  /** Whether this panel must pass validation before proceeding */
  required: boolean;
}

/** Validation errors keyed by field name */
export type ValidationErrors = Record<string, string>;
