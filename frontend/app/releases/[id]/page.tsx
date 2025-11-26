"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { fetchJson } from "../../lib/api";
import { LoadingSpinner } from "../../components/LoadingComponents";
import { ImageGallery } from "../../components/ImageGallery";
import { TrackList } from "../../components/TrackList";
import { ReleaseLinks } from "../../components/ReleaseLinks";
import { DeleteReleaseButton } from "../../components/DeleteReleaseButton";
import { EditReleaseButton } from "../../components/EditReleaseButton";

// Type definitions for detailed music release
interface Artist {
  id: number;
  name: string;
}

interface Genre {
  id: number;
  name: string;
}

interface Label {
  id: number;
  name: string;
}

interface Country {
  id: number;
  name: string;
}

interface Format {
  id: number;
  name: string;
}

interface Packaging {
  id: number;
  name: string;
}

interface PurchaseInfo {
  storeId?: number;
  storeName?: string;
  price?: number;
  currency?: string;
  purchaseDate?: string;
  notes?: string;
}

interface ReleaseImages {
  coverFront?: string;
  coverBack?: string;
  thumbnail?: string;
}

interface ReleaseLink {
  url?: string;
  type?: string;
  description?: string;
}

interface Track {
  title: string;
  releaseYear?: string;
  artists: string[];
  genres: string[];
  live: boolean;
  lengthSecs?: number;
  index: number;
}

interface Media {
  name?: string;
  tracks?: Track[];
}

interface DetailedMusicRelease {
  id: number;
  title: string;
  releaseYear?: string;
  origReleaseYear?: string;
  artists?: Artist[];
  genres?: Genre[];
  live: boolean;
  label?: Label;
  country?: Country;
  labelNumber?: string;
  upc?: string;
  lengthInSeconds?: number;
  format?: Format;
  packaging?: Packaging;
  purchaseInfo?: PurchaseInfo;
  images?: ReleaseImages;
  links?: ReleaseLink[];
  media?: Media[];
  dateAdded: string;
  lastModified: string;
}

export default function ReleaseDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  
  const [release, setRelease] = useState<DetailedMusicRelease | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRelease = async () => {
      try {
        setLoading(true);
        setError(null);
        
        const response: DetailedMusicRelease = await fetchJson(`/api/musicreleases/${id}`);
        setRelease(response);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load release details");
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchRelease();
    }
  }, [id]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-600 text-xl mb-4">Error loading release</div>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={() => router.back()}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Go Back
          </button>
        </div>
      </div>
    );
  }

  if (!release) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-gray-600 text-xl mb-4">Release not found</div>
          <Link
            href="/collection"
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Back to Collection
          </Link>
        </div>
      </div>
    );
  }

  const formatDuration = (seconds?: number) => {
    if (!seconds || seconds === 0) return null;
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = seconds % 60;
    
    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${remainingSeconds.toString().padStart(2, '0')}`;
    }
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  const handleDeleteSuccess = () => {
    // Navigate to collection page after successful deletion
    router.push('/collection');
  };

  const handleDeleteError = (error: { status?: number; message?: string }) => {
    console.error("Delete error:", error);
    // Error is already displayed by the DeleteReleaseButton component
  };

  // Helper function to get Discogs link from release links
  const getDiscogsLink = (): string | null => {
    if (!release?.links || release.links.length === 0) {
      return null;
    }

    // Check if any link has type 'discogs' or URL contains 'discogs'
    const discogsLink = release.links.find(
      (link) =>
        link.type?.toLowerCase() === 'discogs' ||
        link.url?.toLowerCase().includes('discogs')
    );

    return discogsLink?.url || null;
  };

  return (
    <div className="min-h-screen bg-white">
      {/* Minimal Header with Back Button and Actions */}
      <div className="border-b border-gray-200">
        <div className="max-w-[1400px] mx-auto px-8 py-4">
          <div className="flex items-center justify-between">
            <button
              onClick={() => router.back()}
              className="text-gray-400 hover:text-gray-700 flex items-center gap-2 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 19l-7-7 7-7" />
              </svg>
              <span className="text-sm uppercase tracking-wider font-bold">Back</span>
            </button>
            <div className="flex items-center gap-2">
              <EditReleaseButton
                releaseId={release.id}
                releaseTitle={release.title}
              />
              <DeleteReleaseButton
                releaseId={release.id}
                releaseTitle={release.title}
                onDeleteSuccess={handleDeleteSuccess}
                onDeleteError={handleDeleteError}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Main Content - Two Column Layout */}
      <div className="max-w-[1400px] mx-auto px-8 py-16">
        {/* Artist Name, Album Title & Index Badge */}
        <div className="mb-12 flex items-start justify-between gap-8">
          <div className="flex-1">
            {/* Artist Name - Large and Bold */}
            {release.artists && release.artists.length > 0 && (
              <div className="mb-3">
                {release.artists.map((artist, index) => (
                  <span key={artist.id}>
                    <Link
                      href={`/collection?artistId=${artist.id}`}
                      className="text-5xl md:text-6xl lg:text-7xl font-bold text-red-500 hover:text-red-600 transition-colors tracking-tight leading-none"
                    >
                      {artist.name}
                    </Link>
                    {index < release.artists.length - 1 && (
                      <span className="text-5xl md:text-6xl lg:text-7xl font-medium text-gray-300"> & </span>
                    )}
                  </span>
                ))}
              </div>
            )}

          {/* Album Title - Subtitle Style */}
          <h1 className="text-2xl md:text-3xl text-gray-900 font-bold tracking-wide uppercase">
            {release.title}
          </h1>
        </div>

        {/* Discogs Link & Index Number Badge - Top Right */}
        <div className="flex items-center gap-2">
          {/* Discogs Link Button */}
          {getDiscogsLink() && (
            <a
              href={getDiscogsLink() || ''}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-black hover:bg-gray-800 text-white transition-colors flex-shrink-0"
              title="View on Discogs"
            >
              {/* Discogs Logo SVG */}
              <svg
                className="w-7 h-7"
                viewBox="0 0 24 24"
                fill="currentColor"
                xmlns="http://www.w3.org/2000/svg"
              >
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm0 10c-2.21 0-4-1.79-4-4s1.79-4 4-4 4 1.79 4 4-1.79 4-4 4zm0-6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" />
              </svg>
            </a>
          )}
          
          {/* Index Number Badge */}
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-red-500 text-white font-bold text-sm flex-shrink-0">
            #{release.id}
          </div>
        </div>
      </div>        {/* Two Column Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-[max-content_1fr] gap-16 lg:items-start">
          {/* Left Column - Album Cover */}
          <div className="max-w-md">
            <ImageGallery images={release.images} title={release.title} />
          </div>

          {/* Right Column - Details */}
          <div className="space-y-8" style={{ paddingTop: release.media && release.media.length === 1 ? '0' : '0' }}>
            {/* Release Info */}
            <div>
              <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-6 font-bold">Release Info</h3>
              <dl className="space-y-4">
                {release.releaseYear && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Year</dt>
                    <dd className="text-sm text-gray-900 font-medium">{new Date(release.releaseYear).getFullYear()}</dd>
                  </div>
                )}
                {release.origReleaseYear && release.origReleaseYear !== release.releaseYear && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Original</dt>
                    <dd className="text-sm text-gray-900 font-medium">{new Date(release.origReleaseYear).getFullYear()}</dd>
                  </div>
                )}
                {release.format && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Format</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.format.name}</dd>
                  </div>
                )}
                {release.packaging && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Packaging</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.packaging.name}</dd>
                  </div>
                )}
                {release.label && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Label</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.label.name}</dd>
                  </div>
                )}
                {release.labelNumber && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Catalog</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.labelNumber}</dd>
                  </div>
                )}
                {release.upc && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Barcode</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.upc}</dd>
                  </div>
                )}
                {release.country && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Country</dt>
                    <dd className="text-sm text-gray-900 font-medium">{release.country.name}</dd>
                  </div>
                )}
                {(release.lengthInSeconds && release.lengthInSeconds > 0) ? (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Duration</dt>
                    <dd className="text-sm text-gray-900 font-medium">{formatDuration(release.lengthInSeconds)}</dd>
                  </div>
                ) : null}
                {release.live && (
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Type</dt>
                    <dd className="text-sm text-gray-900 font-medium">Live Recording</dd>
                  </div>
                )}
              </dl>
            </div>

            {/* Genres */}
            {release.genres && release.genres.length > 0 && (
              <div>
                <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-6 font-bold">Genres</h3>
                <div className="flex flex-wrap gap-2">
                  {release.genres.map((genre) => (
                    <span
                      key={genre.id}
                      className="text-xs px-3 py-1 bg-gray-100 text-gray-900 rounded-full font-semibold"
                    >
                      {genre.name}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Single-disc - Tracklist & Purchase Info Side by Side */}
        {release.media && release.media.length === 1 && (
          <div className="mt-12 grid grid-cols-1 lg:grid-cols-[45%_1fr] gap-1">
            {/* Tracklist */}
            <div className="max-w-md">
              <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-6 font-bold">Tracklist</h3>
              <TrackList 
                media={release.media} 
                albumArtists={release.artists?.map(artist => artist.name) || []}
              />
            </div>

            {/* Purchase Info & Collection */}
            <div className="space-y-8">
              {/* Purchase Info */}
              {release.purchaseInfo && (
                <div>
                  <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-6 font-bold">Purchase Info</h3>
                  <dl className="space-y-4">
                    {(release.purchaseInfo.storeName || release.purchaseInfo.storeId) && (
                      <div className="flex items-baseline gap-2">
                        <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Store</dt>
                        <dd className="text-sm text-gray-900 font-medium">
                          {release.purchaseInfo.storeName || `Store ID: ${release.purchaseInfo.storeId}`}
                        </dd>
                      </div>
                    )}
                    {release.purchaseInfo.price && (
                      <div className="flex items-baseline gap-2">
                        <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Price</dt>
                        <dd className="text-sm text-gray-900 font-medium">
                          {release.purchaseInfo.currency === 'GBP' || !release.purchaseInfo.currency 
                            ? `£${release.purchaseInfo.price.toFixed(2)}`
                            : `${release.purchaseInfo.currency} ${release.purchaseInfo.price.toFixed(2)}`
                          }
                        </dd>
                      </div>
                    )}
                    {release.purchaseInfo.purchaseDate && (
                      <div className="flex items-baseline gap-2">
                        <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Date</dt>
                        <dd className="text-sm text-gray-900 font-medium">
                          {new Date(release.purchaseInfo.purchaseDate).toLocaleDateString('en-US', {
                            year: 'numeric',
                            month: 'short',
                            day: 'numeric'
                          })}
                        </dd>
                      </div>
                    )}
                    {release.purchaseInfo.notes && (
                      <div>
                        <dt className="text-xs text-gray-500 mb-2 font-semibold">Notes</dt>
                        <dd className="text-sm text-gray-700 font-medium italic border-l-2 border-gray-300 pl-3">
                          {release.purchaseInfo.notes}
                        </dd>
                      </div>
                    )}
                  </dl>
                </div>
              )}

              {/* Collection Info */}
              <div>
                <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-4 font-bold">Collection</h3>
                <dl className="space-y-4">
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Added</dt>
                    <dd className="text-sm text-gray-900 font-medium">
                      {new Date(release.dateAdded).toLocaleDateString('en-US', {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric'
                      })}
                    </dd>
                  </div>
                  <div className="flex items-baseline gap-2">
                    <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Modified</dt>
                    <dd className="text-sm text-gray-900 font-medium">
                      {new Date(release.lastModified).toLocaleDateString('en-US', {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric'
                      })}
                    </dd>
                  </div>
                </dl>
              </div>
            </div>
          </div>
        )}

        {/* Multi-disc Tracklist - Full Width Below Cover */}
        {release.media && release.media.length > 1 && (
          <div className="mt-12">
            <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-6 font-bold">Tracklist</h3>
            <TrackList 
              media={release.media} 
              albumArtists={release.artists?.map(artist => artist.name) || []}
            />
          </div>
        )}

        {/* Multi-disc - Purchase Info & Collection - Side by Side */}
        {release.media && release.media.length > 1 && (
          <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-8 items-start">
            {/* Purchase Info */}
            {release.purchaseInfo && (
              <div>
                <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-4 font-bold">Purchase Info</h3>
                <dl className="space-y-4">
                  {(release.purchaseInfo.storeName || release.purchaseInfo.storeId) && (
                    <div className="flex items-baseline gap-2">
                      <dt className="text-xs text-gray-400 min-w-[80px]">Store</dt>
                      <dd className="text-sm text-gray-900 font-light">
                        {release.purchaseInfo.storeName || `Store ID: ${release.purchaseInfo.storeId}`}
                      </dd>
                    </div>
                  )}
                  {release.purchaseInfo.price && (
                    <div className="flex items-baseline gap-2">
                      <dt className="text-xs text-gray-400 min-w-[80px]">Price</dt>
                      <dd className="text-sm text-gray-900 font-light">
                        {release.purchaseInfo.currency === 'GBP' || !release.purchaseInfo.currency 
                          ? `£${release.purchaseInfo.price.toFixed(2)}`
                          : `${release.purchaseInfo.currency} ${release.purchaseInfo.price.toFixed(2)}`
                        }
                      </dd>
                    </div>
                  )}
                  {release.purchaseInfo.purchaseDate && (
                    <div className="flex items-baseline gap-2">
                      <dt className="text-xs text-gray-400 min-w-[80px]">Date</dt>
                      <dd className="text-sm text-gray-900 font-light">
                        {new Date(release.purchaseInfo.purchaseDate).toLocaleDateString('en-US', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric'
                        })}
                      </dd>
                    </div>
                  )}
                  {release.purchaseInfo.notes && (
                    <div>
                      <dt className="text-xs text-gray-400 mb-2">Notes</dt>
                      <dd className="text-sm text-gray-700 font-light italic border-l-2 border-gray-200 pl-3">
                        {release.purchaseInfo.notes}
                      </dd>
                    </div>
                  )}
                </dl>
              </div>
            )}

            {/* Collection Info */}
            <div>
              <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-4 font-bold">Collection</h3>
              <dl className="space-y-4">
                <div className="flex items-baseline gap-2">
                  <dt className="text-xs text-gray-400 min-w-[80px]">Added</dt>
                  <dd className="text-sm text-gray-900 font-light">
                    {new Date(release.dateAdded).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'short',
                      day: 'numeric'
                    })}
                  </dd>
                </div>
                <div className="flex items-baseline gap-2">
                  <dt className="text-xs text-gray-400 min-w-[80px]">Modified</dt>
                  <dd className="text-sm text-gray-900 font-light">
                    {new Date(release.lastModified).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'short',
                      day: 'numeric'
                    })}
                  </dd>
                </div>
              </dl>
            </div>
          </div>
        )}

        {/* Links - Always at the bottom for all releases */}
        {release.links && release.links.length > 0 && (
          <div className="mt-12">
            <h3 className="text-xs uppercase tracking-widest text-gray-900 mb-4 font-bold">Links</h3>
            <ReleaseLinks links={release.links} />
          </div>
        )}
      </div>
    </div>
  );
}
