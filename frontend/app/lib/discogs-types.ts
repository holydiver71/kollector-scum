// TypeScript interfaces for Discogs API integration
// Matches backend DTOs in KollectorScum.Api/DTOs/

export interface DiscogsSearchRequest {
  catalogNumber: string;
  format?: string;
  country?: string;
  year?: number;
}

export interface DiscogsSearchResult {
  id: number;
  title: string;
  artist: string;
  format: string;
  country: string;
  year: string;
  label: string;
  catalogNumber: string;
  thumbUrl?: string;
  coverImageUrl?: string;
  resourceUrl: string;
}

export interface DiscogsArtist {
  id: number;
  name: string;
  role?: string;
}

export interface DiscogsLabel {
  id: number;
  name: string;
  catalogNumber?: string;
}

export interface DiscogsTrack {
  position: string;
  title: string;
  duration: string;
  artists?: string[];
}

export interface DiscogsImage {
  type: string; // 'primary', 'secondary'
  uri: string;
  uri150?: string;
  width: number;
  height: number;
}

export interface DiscogsFormat {
  name: string;
  qty: string;
  descriptions?: string[];
}

export interface DiscogsIdentifier {
  type: string;
  value: string;
}

export interface DiscogsRelease {
  id: number;
  title: string;
  artists: DiscogsArtist[];
  labels: DiscogsLabel[];
  formats: DiscogsFormat[];
  genres: string[];
  styles: string[];
  country: string;
  releaseDate: string;
  year: number;
  notes?: string;
  tracklist: DiscogsTrack[];
  images: DiscogsImage[];
  identifiers: DiscogsIdentifier[];
  uri: string;
  resourceUrl: string;
  dataQuality?: string;
}

// New entities that will be created in the database
export interface NewLookupEntities {
  artists: Array<{ id?: number; name: string }>;
  labels: Array<{ id?: number; name: string }>;
  genres: Array<{ id?: number; name: string }>;
  countries: Array<{ id?: number; name: string }>;
  formats: Array<{ id?: number; name: string }>;
  packagings: Array<{ id?: number; name: string }>;
}
