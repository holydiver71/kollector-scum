// Shared TypeScript types used by frontend pages and components

export interface LookupItemDto {
  id: number;
  name: string;
}

export type ArtistDto = LookupItemDto;
export type GenreDto = LookupItemDto;
export type LabelDto = LookupItemDto;
export type CountryDto = LookupItemDto;
export type FormatDto = LookupItemDto;
export type PackagingDto = LookupItemDto;
export type StoreDto = LookupItemDto;

export interface LinkDto {
  id?: number;
  url: string;
  type?: string;
}

export interface MediaDto {
  id?: number;
  type?: string; // e.g., 'vinyl', 'cd'
  durationSeconds?: number;
}

export interface MusicReleaseDto {
  id: number;
  title: string;
  releaseYear?: string;
  origReleaseYear?: string;
  artists?: ArtistDto[];
  genres?: GenreDto[];
  label?: LabelDto | null;
  country?: CountryDto | null;
  format?: FormatDto | null;
  packaging?: PackagingDto | null;
  store?: StoreDto | null;
  labelNumber?: string;
  upc?: string;
  lengthInSeconds?: number;
  live?: boolean;
  links?: LinkDto[];
  media?: MediaDto[];
  dateAdded?: string;
  coverImageUrl?: string | null;
}

export type CreateMusicReleaseDto = Omit<MusicReleaseDto, 'id' | 'dateAdded' | 'coverImageUrl'> & {
  artistIds?: number[];
  genreIds?: number[];
  labelId?: number | null;
  countryId?: number | null;
  formatId?: number | null;
  packagingId?: number | null;
  storeId?: number | null;
};

export default MusicReleaseDto;
