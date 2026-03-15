import type { LookupItem, MockFormData, WizardStep } from "./types";

// ─── Lookup fixtures ──────────────────────────────────────────────────────────

export const ARTISTS: LookupItem[] = [
  { id: 1, name: "Iron Maiden" },
  { id: 2, name: "Metallica" },
  { id: 3, name: "Black Sabbath" },
  { id: 4, name: "AC/DC" },
  { id: 5, name: "Judas Priest" },
  { id: 6, name: "Motörhead" },
  { id: 7, name: "Slayer" },
  { id: 8, name: "Megadeth" },
  { id: 9, name: "Pantera" },
  { id: 10, name: "Dio" },
  { id: 11, name: "Ozzy Osbourne" },
  { id: 12, name: "Rainbow" },
  { id: 13, name: "Deep Purple" },
  { id: 14, name: "Saxon" },
  { id: 15, name: "Accept" },
];

export const GENRES: LookupItem[] = [
  { id: 1, name: "Heavy Metal" },
  { id: 2, name: "Thrash Metal" },
  { id: 3, name: "Death Metal" },
  { id: 4, name: "Black Metal" },
  { id: 5, name: "Doom Metal" },
  { id: 6, name: "Hard Rock" },
  { id: 7, name: "Classic Rock" },
  { id: 8, name: "Progressive Rock" },
  { id: 9, name: "Speed Metal" },
  { id: 10, name: "Power Metal" },
  { id: 11, name: "Glam Metal" },
  { id: 12, name: "NWOBHM" },
];

export const FORMATS: LookupItem[] = [
  { id: 1, name: "Vinyl" },
  { id: 2, name: "CD" },
  { id: 3, name: "Cassette" },
  { id: 4, name: "Digital" },
  { id: 5, name: "DVD" },
  { id: 6, name: "Blu-ray" },
  { id: 7, name: "Box Set" },
];

export const PACKAGINGS: LookupItem[] = [
  { id: 1, name: "Gatefold" },
  { id: 2, name: "Jewel Case" },
  { id: 3, name: "Digipak" },
  { id: 4, name: "Cardboard Sleeve" },
  { id: 5, name: "Plastic Sleeve" },
  { id: 6, name: "Box Set" },
  { id: 7, name: "Slipcase" },
];

export const COUNTRIES: LookupItem[] = [
  { id: 1, name: "United Kingdom" },
  { id: 2, name: "United States" },
  { id: 3, name: "Germany" },
  { id: 4, name: "Japan" },
  { id: 5, name: "France" },
  { id: 6, name: "Netherlands" },
  { id: 7, name: "Sweden" },
  { id: 8, name: "Australia" },
  { id: 9, name: "Canada" },
  { id: 10, name: "Brazil" },
];

export const LABELS: LookupItem[] = [
  { id: 1, name: "EMI" },
  { id: 2, name: "Elektra Records" },
  { id: 3, name: "Roadrunner Records" },
  { id: 4, name: "Metal Blade Records" },
  { id: 5, name: "Nuclear Blast" },
  { id: 6, name: "Century Media" },
  { id: 7, name: "Columbia Records" },
  { id: 8, name: "Capitol Records" },
  { id: 9, name: "Sanctuary Records" },
  { id: 10, name: "SPV GmbH" },
];

export const STORES: LookupItem[] = [
  { id: 1, name: "Amazon" },
  { id: 2, name: "eBay" },
  { id: 3, name: "Discogs" },
  { id: 4, name: "HMV" },
  { id: 5, name: "Local Record Shop" },
  { id: 6, name: "Bandcamp" },
  { id: 7, name: "Music Magpie" },
];

export const LINK_TYPES = [
  "Discogs",
  "Spotify",
  "YouTube",
  "Bandcamp",
  "SoundCloud",
  "MusicBrainz",
  "AllMusic",
  "Last.fm",
  "Rate Your Music",
  "Official Website",
  "Other",
];

export const CURRENCIES = [
  { value: "GBP", label: "GBP (£)" },
  { value: "USD", label: "USD ($)" },
  { value: "EUR", label: "EUR (€)" },
  { value: "JPY", label: "JPY (¥)" },
  { value: "CAD", label: "CAD ($)" },
  { value: "AUD", label: "AUD ($)" },
];

// ─── Wizard step definitions ─────────────────────────────────────────────────

/** The 10 steps of the guided add-release flow (panels 0–8 + draft preview 9) */
export const WIZARD_STEPS: WizardStep[] = [
  {
    id: 0,
    title: "Basic Information",
    description: "Release title and contributing artists",
    required: true,
  },
  {
    id: 1,
    title: "Release Information",
    description: "Format, packaging, country, genres and live recording",
    required: false,
  },
  {
    id: 2,
    title: "Label & Dates",
    description: "Record label, catalogue number, barcode and release years",
    required: false,
  },
  {
    id: 3,
    title: "Purchase Information",
    description: "Where and when you bought this release",
    required: false,
  },
  {
    id: 4,
    title: "Images",
    description: "Cover art front, back and thumbnail filenames",
    required: false,
  },
  {
    id: 5,
    title: "Track Listing",
    description: "Add discs and tracks for this release",
    required: false,
  },
  {
    id: 6,
    title: "External Links",
    description: "Links to Discogs, Spotify and other services",
    required: false,
  },
  {
    id: 7,
    title: "Draft Preview",
    description: "Review your release before adding to your collection",
    required: false,
  },
];

// ─── Seeded form data for demonstration ──────────────────────────────────────

/** Pre-filled sample data so every panel shows representative content */
export const SEED_FORM_DATA: MockFormData = {
  title: "The Number of the Beast",
  artistNames: ["Iron Maiden"],
  releaseYear: "1982",
  origReleaseYear: "1982",
  live: false,
  genreNames: ["Heavy Metal", "NWOBHM"],
  formatName: "Vinyl",
  packagingName: "Gatefold",
  countryName: "United Kingdom",
  labelName: "EMI",
  labelNumber: "EMC 3400",
  upc: "077774681116",
  purchaseInfo: {
    storeName: "Local Record Shop",
    price: 22.99,
    currency: "GBP",
    purchaseDate: "2024-03-01",
    notes: "Original UK press in near-mint condition",
  },
  images: {
    coverFront: "iron-maiden-the-number-of-the-beast-1982.jpg",
    coverBack: "iron-maiden-the-number-of-the-beast-1982-back.jpg",
    thumbnail: "thumb-iron-maiden-the-number-of-the-beast-1982.jpg",
  },
  media: [
    {
      name: "Side A",
      tracks: [
        { title: "Invaders", index: 1, lengthSecs: 200 },
        { title: "Children of the Damned", index: 2, lengthSecs: 270 },
        { title: "The Prisoner", index: 3, lengthSecs: 360 },
        { title: "22 Acacia Avenue", index: 4, lengthSecs: 395 },
      ],
    },
    {
      name: "Side B",
      tracks: [
        { title: "The Number of the Beast", index: 1, lengthSecs: 294 },
        { title: "Run to the Hills", index: 2, lengthSecs: 228 },
        { title: "Gangland", index: 3, lengthSecs: 232 },
        { title: "Hallowed Be Thy Name", index: 4, lengthSecs: 431 },
      ],
    },
  ],
  links: [
    {
      url: "https://www.discogs.com/master/21648",
      type: "Discogs",
      description: "Master release on Discogs",
    },
    {
      url: "https://open.spotify.com/album/0J8oh5MAMyBLNcC8tJZiXg",
      type: "Spotify",
      description: "Stream on Spotify",
    },
  ],
};

/** Blank form data - used when user starts from scratch */
export const EMPTY_FORM_DATA: MockFormData = {
  title: "",
  artistNames: [],
  releaseYear: "",
  origReleaseYear: "",
  live: false,
  genreNames: [],
  formatName: "",
  packagingName: "",
  countryName: "",
  labelName: "",
  labelNumber: "",
  upc: "",
  purchaseInfo: {
    storeName: "",
    price: undefined,
    currency: "GBP",
    purchaseDate: "",
    notes: "",
  },
  images: {
    coverFront: "",
    coverBack: "",
    thumbnail: "",
  },
  media: [],
  links: [],
};
