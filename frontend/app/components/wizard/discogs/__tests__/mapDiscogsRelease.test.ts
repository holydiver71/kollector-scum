/**
 * Unit tests for the shared Discogs release mapper.
 *
 * These tests verify the mapping logic in isolation so both the direct-add
 * and edit-wizard paths remain consistent.
 */

import {
  sanitizeFilename,
  generateImageFilename,
  parseDuration,
  extractFilenameFromUrl,
  mapDiscogsRelease,
} from "../mapDiscogsRelease";
import type { DiscogsRelease } from "../../../../lib/discogs-types";

// ─── Fixture ──────────────────────────────────────────────────────────────────

const BASE_RELEASE: DiscogsRelease = {
  id: 12345,
  title: "The Dark Side of the Moon",
  artists: [{ id: 1, name: "Pink Floyd" }],
  labels: [{ id: 10, name: "Harvest", catalogNumber: "SHVL 804" }],
  formats: [{ name: "Vinyl", qty: "1", descriptions: ["LP"] }],
  genres: ["Rock"],
  styles: ["Psychedelic Rock"],
  country: "UK",
  releaseDate: "1973-03-01",
  year: 1973,
  tracklist: [
    { position: "A1", title: "Speak to Me", duration: "1:30" },
    { position: "A2", title: "Breathe", duration: "2:43" },
  ],
  images: [
    {
      type: "primary",
      uri: "https://img.discogs.com/cover.jpg",
      uri150: "https://img.discogs.com/cover-150.jpg",
      width: 600,
      height: 600,
    },
  ],
  identifiers: [{ type: "Barcode", value: "5099902987927" }],
  uri: "https://www.discogs.com/release/12345",
  resourceUrl: "https://api.discogs.com/releases/12345",
};

// ─── sanitizeFilename ─────────────────────────────────────────────────────────

describe("sanitizeFilename", () => {
  it("replaces non-alphanumeric characters with dashes", () => {
    expect(sanitizeFilename("Hello World!")).toBe("Hello-World");
  });

  it("collapses multiple consecutive dashes into one", () => {
    expect(sanitizeFilename("Pink--Floyd")).toBe("Pink-Floyd");
  });

  it("removes leading and trailing dashes", () => {
    expect(sanitizeFilename("--test--")).toBe("test");
  });

  it("preserves alphanumeric characters unchanged", () => {
    expect(sanitizeFilename("abc123ABC")).toBe("abc123ABC");
  });

  it("returns an empty string for an input of only special characters", () => {
    expect(sanitizeFilename("!@#$%")).toBe("");
  });
});

// ─── generateImageFilename ────────────────────────────────────────────────────

describe("generateImageFilename", () => {
  it("produces artist-title-year.jpg when all parts are provided", () => {
    expect(generateImageFilename("Pink Floyd", "Dark Side", 1973)).toBe(
      "Pink-Floyd-Dark-Side-1973.jpg"
    );
  });

  it("omits the year when it is not provided", () => {
    expect(generateImageFilename("Artist", "Title")).toBe("Artist-Title.jpg");
  });

  it("sanitizes special characters in artist and title", () => {
    expect(generateImageFilename("AC/DC", "Back in Black", 1980)).toBe(
      "AC-DC-Back-in-Black-1980.jpg"
    );
  });
});

// ─── parseDuration ────────────────────────────────────────────────────────────

describe("parseDuration", () => {
  it("parses M:SS format correctly", () => {
    expect(parseDuration("3:45")).toBe(225);
  });

  it("parses H:MM:SS format correctly", () => {
    expect(parseDuration("1:02:03")).toBe(3723);
  });

  it("returns undefined for an empty string", () => {
    expect(parseDuration("")).toBeUndefined();
  });

  it("returns undefined for malformed input", () => {
    expect(parseDuration("abc")).toBeUndefined();
  });

  it("parses zero-padded seconds correctly", () => {
    expect(parseDuration("1:05")).toBe(65);
  });
});

// ─── extractFilenameFromUrl ───────────────────────────────────────────────────

describe("extractFilenameFromUrl", () => {
  it("returns the last path segment of a URL", () => {
    expect(
      extractFilenameFromUrl(
        "http://localhost:5072/api/images/covers/Artist-Title-2020.jpg"
      )
    ).toBe("Artist-Title-2020.jpg");
  });

  it("handles a URL with no path segments gracefully", () => {
    expect(extractFilenameFromUrl("filename.jpg")).toBe("filename.jpg");
  });
});

// ─── mapDiscogsRelease ────────────────────────────────────────────────────────

describe("mapDiscogsRelease", () => {
  describe("formData", () => {
    it("maps the title correctly", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.title).toBe("The Dark Side of the Moon");
    });

    it("maps artist names from the artists array", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.artistNames).toEqual(["Pink Floyd"]);
      expect(formData.artistIds).toEqual([]);
    });

    it("combines genres and styles into genreNames", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.genreNames).toEqual(["Rock", "Psychedelic Rock"]);
    });

    it("maps the first label name and catalogue number", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.labelName).toBe("Harvest");
      expect(formData.labelNumber).toBe("SHVL 804");
    });

    it("maps the country", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.countryName).toBe("UK");
    });

    it("maps the first format name", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.formatName).toBe("Vinyl");
    });

    it("extracts the barcode from identifiers", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.upc).toBe("5099902987927");
    });

    it("sets releaseYear as a string from the numeric year", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.releaseYear).toBe("1973");
    });

    it("maps the Discogs URI into the links array", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.links).toEqual([
        {
          url: "https://www.discogs.com/release/12345",
          type: "Discogs",
          description: "",
        },
      ]);
    });

    it("produces an empty links array when uri is absent", () => {
      const release = { ...BASE_RELEASE, uri: "" };
      const { formData } = mapDiscogsRelease(release);
      expect(formData.links).toEqual([]);
    });

    it("maps the tracklist into a single Disc 1 medium", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.media).toHaveLength(1);
      expect(formData.media![0].name).toBe("Disc 1");
      expect(formData.media![0].tracks).toHaveLength(2);
      expect(formData.media![0].tracks[0].title).toBe("Speak to Me");
      expect(formData.media![0].tracks[0].lengthSecs).toBe(90);
      expect(formData.media![0].tracks[1].title).toBe("Breathe");
      expect(formData.media![0].tracks[1].lengthSecs).toBe(163);
    });

    it("sets live to false", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.live).toBe(false);
    });

    it("generates image filenames (not full URLs) in the images field", () => {
      const { formData } = mapDiscogsRelease(BASE_RELEASE);
      expect(formData.images?.coverFront).toBe("Pink-Floyd-The-Dark-Side-of-the-Moon-1973.jpg");
      expect(formData.images?.thumbnail).toBe("thumb-Pink-Floyd-The-Dark-Side-of-the-Moon-1973.jpg");
    });

    it("omits the images field when the release has no images", () => {
      const release = { ...BASE_RELEASE, images: [] };
      const { formData } = mapDiscogsRelease(release);
      expect(formData.images).toBeUndefined();
    });

    it("returns an empty media array when the release has no tracklist", () => {
      const release = { ...BASE_RELEASE, tracklist: [] };
      const { formData } = mapDiscogsRelease(release);
      expect(formData.media).toEqual([]);
    });
  });

  describe("sourceImages", () => {
    it("captures the full-size image URI as cover", () => {
      const { sourceImages } = mapDiscogsRelease(BASE_RELEASE);
      expect(sourceImages.cover).toBe("https://img.discogs.com/cover.jpg");
    });

    it("uses uri150 as the thumbnail when available", () => {
      const { sourceImages } = mapDiscogsRelease(BASE_RELEASE);
      expect(sourceImages.thumbnail).toBe("https://img.discogs.com/cover-150.jpg");
    });

    it("falls back to the full URI when uri150 is absent", () => {
      const release = {
        ...BASE_RELEASE,
        images: [
          { type: "primary", uri: "https://img.discogs.com/cover.jpg", width: 600, height: 600 },
        ],
      };
      const { sourceImages } = mapDiscogsRelease(release as DiscogsRelease);
      expect(sourceImages.thumbnail).toBe("https://img.discogs.com/cover.jpg");
    });

    it("returns null cover and thumbnail when there are no images", () => {
      const release = { ...BASE_RELEASE, images: [] };
      const { sourceImages } = mapDiscogsRelease(release);
      expect(sourceImages.cover).toBeNull();
      expect(sourceImages.thumbnail).toBeNull();
    });
  });
});
