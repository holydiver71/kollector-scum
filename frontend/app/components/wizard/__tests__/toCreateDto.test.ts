/**
 * Unit tests for the toCreateDto and fromCreateDto mapper utilities.
 * These are pure functions, so tests have no dependencies.
 */
import { toCreateDto, fromCreateDto, EMPTY_FORM_DATA, type WizardFormData } from "../types";

const BASE_DATA: WizardFormData = {
  title: "The Number of the Beast",
  artistIds: [1],
  artistNames: [],
  genreIds: [2],
  genreNames: [],
  live: false,
  formatId: 3,
  formatName: "LP",
  packagingId: 4,
  packagingName: "Gatefold",
  countryId: 5,
  countryName: "United Kingdom",
  releaseYear: "1982",
  origReleaseYear: "1982",
  labelId: 6,
  labelName: "EMI",
  labelNumber: "EMC 3400",
  upc: "077774681116",
  purchaseInfo: {
    storeId: 7,
    storeName: "HMV",
    price: 19.99,
    currency: "GBP",
    purchaseDate: "2023-06-15",
    notes: "UK first press",
  },
  images: {
    coverFront: "iron-maiden-beast.jpg",
    coverBack: "iron-maiden-beast-back.jpg",
    thumbnail: "thumb-iron-maiden-beast.jpg",
  },
  media: [
    {
      name: "Disc 1",
      tracks: [
        { title: "Invaders", index: 1, lengthSecs: 200 },
        { title: "Run to the Hills", index: 2, lengthSecs: 230 },
      ],
    },
  ],
  links: [
    { url: "https://www.discogs.com/release/12345", type: "Discogs", description: "" },
  ],
};

describe("toCreateDto", () => {
  it("maps title and live flag correctly", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.title).toBe("The Number of the Beast");
    expect(dto.live).toBe(false);
  });

  it("converts 4-digit year strings to ISO date strings", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.releaseYear).toBe("1982-01-01T00:00:00.000Z");
    expect(dto.origReleaseYear).toBe("1982-01-01T00:00:00.000Z");
  });

  it("omits releaseYear when empty", () => {
    const data = { ...BASE_DATA, releaseYear: "", origReleaseYear: "" };
    const dto = toCreateDto(data);
    expect(dto.releaseYear).toBeUndefined();
    expect(dto.origReleaseYear).toBeUndefined();
  });

  it("passes through artistIds and genreIds arrays", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.artistIds).toEqual([1]);
    expect(dto.genreIds).toEqual([2]);
  });

  it("sends empty arrays for artistIds and genreIds when none selected", () => {
    const data = { ...BASE_DATA, artistIds: [], genreIds: [] };
    const dto = toCreateDto(data);
    expect(dto.artistIds).toEqual([]);
    expect(dto.genreIds).toEqual([]);
  });

  it("maps classification IDs and names correctly", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.formatId).toBe(3);
    expect(dto.formatName).toBe("LP");
    expect(dto.packagingId).toBe(4);
    expect(dto.packagingName).toBe("Gatefold");
    expect(dto.countryId).toBe(5);
    expect(dto.countryName).toBe("United Kingdom");
  });

  it("maps label info correctly", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.labelId).toBe(6);
    expect(dto.labelName).toBe("EMI");
    expect(dto.labelNumber).toBe("EMC 3400");
    expect(dto.upc).toBe("077774681116");
  });

  it("strips image filenames from full URLs", () => {
    const data = {
      ...BASE_DATA,
      images: {
        coverFront: "https://img.discogs.com/covers/iron-maiden-beast.jpg",
        coverBack: undefined,
        thumbnail: undefined,
      },
    };
    const dto = toCreateDto(data);
    expect(dto.images?.coverFront).toBe("iron-maiden-beast.jpg");
  });

  it("preserves image filenames that are already bare", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.images?.coverFront).toBe("iron-maiden-beast.jpg");
    expect(dto.images?.coverBack).toBe("iron-maiden-beast-back.jpg");
    expect(dto.images?.thumbnail).toBe("thumb-iron-maiden-beast.jpg");
  });

  it("omits images when all values are empty", () => {
    const data = { ...BASE_DATA, images: {} };
    const dto = toCreateDto(data);
    expect(dto.images).toBeUndefined();
  });

  it("maps media and tracks correctly", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.media).toHaveLength(1);
    expect(dto.media![0].name).toBe("Disc 1");
    expect(dto.media![0].tracks).toHaveLength(2);
    expect(dto.media![0].tracks[0].title).toBe("Invaders");
    expect(dto.media![0].tracks[0].lengthSecs).toBe(200);
  });

  it("omits media when the array is empty", () => {
    const data = { ...BASE_DATA, media: [] };
    const dto = toCreateDto(data);
    expect(dto.media).toBeUndefined();
  });

  it("maps links correctly", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.links).toHaveLength(1);
    expect(dto.links![0].url).toBe("https://www.discogs.com/release/12345");
    expect(dto.links![0].type).toBe("Discogs");
  });

  it("includes purchaseInfo when fields are filled", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.purchaseInfo).toBeDefined();
    expect(dto.purchaseInfo!.storeId).toBe(7);
    expect(dto.purchaseInfo!.price).toBe(19.99);
    expect(dto.purchaseInfo!.currency).toBe("GBP");
  });

  it("sends storeName instead of storeId when storeId is absent", () => {
    const data: WizardFormData = {
      ...BASE_DATA,
      purchaseInfo: { ...BASE_DATA.purchaseInfo, storeId: undefined, storeName: "Bandcamp" },
    };
    const dto = toCreateDto(data);
    expect(dto.purchaseInfo!.storeId).toBeUndefined();
    expect(dto.purchaseInfo!.storeName).toBe("Bandcamp");
  });

  it("omits storeName when storeId is set", () => {
    const dto = toCreateDto(BASE_DATA);
    expect(dto.purchaseInfo!.storeId).toBe(7);
    expect(dto.purchaseInfo!.storeName).toBeUndefined();
  });

  it("omits purchaseInfo when no purchase fields are filled", () => {
    const data: WizardFormData = {
      ...BASE_DATA,
      purchaseInfo: { currency: "GBP" },
    };
    const dto = toCreateDto(data);
    expect(dto.purchaseInfo).toBeUndefined();
  });
});

describe("fromCreateDto", () => {
  it("returns EMPTY_FORM_DATA defaults for an empty object", () => {
    const result = fromCreateDto({});
    expect(result.title).toBe("");
    expect(result.artistIds).toEqual([]);
    expect(result.artistNames).toEqual([]);
    expect(result.live).toBe(false);
  });

  it("maps title and artistNames correctly", () => {
    const result = fromCreateDto({ title: "Somewhere in Time", artistNames: ["Iron Maiden"] });
    expect(result.title).toBe("Somewhere in Time");
    expect(result.artistNames).toEqual(["Iron Maiden"]);
  });

  it("extracts the year from an ISO releaseYear string", () => {
    const result = fromCreateDto({ releaseYear: "1986-01-01T00:00:00.000Z" });
    expect(result.releaseYear).toBe("1986");
  });

  it("extracts the year from a plain 4-digit releaseYear string", () => {
    const result = fromCreateDto({ releaseYear: "1986" });
    expect(result.releaseYear).toBe("1986");
  });

  it("preserves artistIds and genreIds", () => {
    const result = fromCreateDto({ artistIds: [1, 2], genreIds: [3] });
    expect(result.artistIds).toEqual([1, 2]);
    expect(result.genreIds).toEqual([3]);
  });

  it("sets live to false by default", () => {
    const result = fromCreateDto({});
    expect(result.live).toBe(false);
  });

  it("sets live to true when provided", () => {
    const result = fromCreateDto({ live: true });
    expect(result.live).toBe(true);
  });

  it("returns empty arrays for media and links when not provided", () => {
    const result = fromCreateDto({});
    expect(result.media).toEqual([]);
    expect(result.links).toEqual([]);
  });
});
