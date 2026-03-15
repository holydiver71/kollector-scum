/**
 * Unit tests for mapDiscogsRelease utilities.
 *
 * Covers the pure functions: parseDuration, sanitizeFilename,
 * generateImageFilename, extractFilenameFromUrl, and mapDiscogsRelease.
 */

import {
  parseDuration,
  sanitizeFilename,
  generateImageFilename,
  extractFilenameFromUrl,
  mapDiscogsRelease,
} from "../mapDiscogsRelease";
import type { DiscogsRelease } from "../../../../lib/discogs-types";

// ─── parseDuration ────────────────────────────────────────────────────────────

describe("parseDuration", () => {
  it("parses MM:SS format", () => {
    expect(parseDuration("3:45")).toBe(225);
  });

  it("parses HH:MM:SS format", () => {
    expect(parseDuration("1:02:30")).toBe(3750);
  });

  it("returns undefined for empty string", () => {
    expect(parseDuration("")).toBeUndefined();
  });

  it("returns undefined for whitespace-only string", () => {
    expect(parseDuration("   ")).toBeUndefined();
  });

  it("returns undefined for non-numeric input", () => {
    expect(parseDuration("abc")).toBeUndefined();
  });

  it("parses 0:00 as 0 seconds", () => {
    expect(parseDuration("0:00")).toBe(0);
  });

  it("parses single-digit minutes and seconds", () => {
    expect(parseDuration("1:05")).toBe(65);
  });
});

// ─── sanitizeFilename ─────────────────────────────────────────────────────────

describe("sanitizeFilename", () => {
  it("replaces spaces with dashes", () => {
    expect(sanitizeFilename("The Beatles")).toBe("The-Beatles");
  });

  it("replaces special characters", () => {
    expect(sanitizeFilename("AC/DC")).toBe("AC-DC");
  });

  it("collapses multiple dashes", () => {
    expect(sanitizeFilename("A & B")).toBe("A-B");
  });

  it("strips leading and trailing dashes", () => {
    expect(sanitizeFilename("&foo&")).toBe("foo");
  });

  it("preserves alphanumeric characters", () => {
    expect(sanitizeFilename("Abbey123")).toBe("Abbey123");
  });
});

// ─── generateImageFilename ────────────────────────────────────────────────────

describe("generateImageFilename", () => {
  it("builds a filename from artist, title, and year", () => {
    expect(generateImageFilename("The Beatles", "Abbey Road", 1969)).toBe(
      "The-Beatles-Abbey-Road-1969.jpg"
    );
  });

  it("omits year when not provided", () => {
    expect(generateImageFilename("Artist", "Title")).toBe(
      "Artist-Title.jpg"
    );
  });

  it("handles special characters in artist and title", () => {
    expect(generateImageFilename("AC/DC", "Back in Black", 1980)).toBe(
      "AC-DC-Back-in-Black-1980.jpg"
    );
  });
});

// ─── extractFilenameFromUrl ───────────────────────────────────────────────────

describe("extractFilenameFromUrl", () => {
  it("extracts filename from a full URL", () => {
    expect(
      extractFilenameFromUrl(
        "http://localhost:5072/api/images/covers/foo.jpg"
      )
    ).toBe("foo.jpg");
  });

  it("returns the string itself when there is no slash", () => {
    expect(extractFilenameFromUrl("foo.jpg")).toBe("foo.jpg");
  });

  it("handles trailing slashes gracefully", () => {
    // Last segment after split will be empty string – reflects current behaviour
    const result = extractFilenameFromUrl("http://example.com/path/");
    expect(result).toBe("");
  });
});

// ─── mapDiscogsRelease ────────────────────────────────────────────────────────

/** A complete minimal Discogs release fixture */
const FIXTURE: DiscogsRelease = {
  id: 1234,
  title: "Abbey Road",
  artists: [{ id: 1, name: "The Beatles" }],
  labels: [{ id: 10, name: "Apple Records", catalogNumber: "PCS 7088" }],
  formats: [{ name: "Vinyl", qty: "1", descriptions: ["LP"] }],
  genres: ["Rock"],
  styles: ["Classic Rock"],
  country: "UK",
  releaseDate: "1969-09-26",
  year: 1969,
  tracklist: [
    { position: "A1", title: "Come Together", duration: "4:19" },
    { position: "A2", title: "Something", duration: "3:03" },
  ],
  images: [
    {
      type: "primary",
      uri: "https://img.discogs.com/cover.jpg",
      uri150: "https://img.discogs.com/thumb.jpg",
      width: 600,
      height: 600,
    },
  ],
  identifiers: [{ type: "Barcode", value: "094638241522" }],
  uri: "https://www.discogs.com/release/1234",
  resourceUrl: "https://api.discogs.com/releases/1234",
};

describe("mapDiscogsRelease", () => {
  it("maps title", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.title).toBe("Abbey Road");
  });

  it("maps artist names", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.artistNames).toEqual(["The Beatles"]);
    expect(dto.artistIds).toEqual([]);
  });

  it("combines genres and styles", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.genreNames).toEqual(["Rock", "Classic Rock"]);
    expect(dto.genreIds).toEqual([]);
  });

  it("maps label name and number", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.labelName).toBe("Apple Records");
    expect(dto.labelNumber).toBe("PCS 7088");
  });

  it("maps country and format", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.countryName).toBe("UK");
    expect(dto.formatName).toBe("Vinyl");
  });

  it("extracts barcode as UPC", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.upc).toBe("094638241522");
  });

  it("maps year as a string", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.releaseYear).toBe("1969");
  });

  it("maps tracklist into a single Disc 1", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.media).toHaveLength(1);
    expect(dto.media![0].name).toBe("Disc 1");
    expect(dto.media![0].tracks).toHaveLength(2);
    expect(dto.media![0].tracks[0].title).toBe("Come Together");
    expect(dto.media![0].tracks[0].lengthSecs).toBe(259); // 4:19
    expect(dto.media![0].tracks[1].title).toBe("Something");
    expect(dto.media![0].tracks[1].lengthSecs).toBe(183); // 3:03
  });

  it("generates local image filenames", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.images?.coverFront).toMatch(/^The-Beatles-Abbey-Road-1969\.jpg$/);
    expect(dto.images?.thumbnail).toMatch(/^thumb-The-Beatles-Abbey-Road-1969\.jpg$/);
  });

  it("returns source image URLs", () => {
    const { sourceImages } = mapDiscogsRelease(FIXTURE);
    expect(sourceImages.cover).toBe("https://img.discogs.com/cover.jpg");
    expect(sourceImages.thumbnail).toBe("https://img.discogs.com/thumb.jpg");
  });

  it("includes Discogs link", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.links).toHaveLength(1);
    expect(dto.links![0].url).toBe("https://www.discogs.com/release/1234");
    expect(dto.links![0].type).toBe("Discogs");
  });

  it("returns null sourceImages when no images are present", () => {
    const noImages = { ...FIXTURE, images: [] };
    const { sourceImages, dto } = mapDiscogsRelease(noImages);
    expect(sourceImages.cover).toBeNull();
    expect(sourceImages.thumbnail).toBeNull();
    expect(dto.images).toBeUndefined();
  });

  it("falls back to uri when uri150 is not present", () => {
    const noThumb: DiscogsRelease = {
      ...FIXTURE,
      images: [
        {
          type: "primary",
          uri: "https://img.discogs.com/cover.jpg",
          width: 600,
          height: 600,
        },
      ],
    };
    const { sourceImages } = mapDiscogsRelease(noThumb);
    expect(sourceImages.thumbnail).toBe("https://img.discogs.com/cover.jpg");
  });

  it("produces an empty media array when tracklist is absent", () => {
    const noTracks = { ...FIXTURE, tracklist: [] };
    const { dto } = mapDiscogsRelease(noTracks);
    expect(dto.media).toHaveLength(0);
  });

  it("produces no link when uri is empty", () => {
    const noUri = { ...FIXTURE, uri: "" };
    const { dto } = mapDiscogsRelease(noUri);
    expect(dto.links).toHaveLength(0);
  });

  it("does not include UPC when barcode identifier is missing", () => {
    const noBarcode = { ...FIXTURE, identifiers: [] };
    const { dto } = mapDiscogsRelease(noBarcode);
    expect(dto.upc).toBeUndefined();
  });

  it("sets live to false", () => {
    const { dto } = mapDiscogsRelease(FIXTURE);
    expect(dto.live).toBe(false);
  });
});
