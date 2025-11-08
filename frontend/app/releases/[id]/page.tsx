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

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center gap-4">
            <button
              onClick={() => router.back()}
              className="text-gray-600 hover:text-gray-900 flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 19l-7-7 7-7" />
              </svg>
              Back
            </button>
            <div className="flex-1">
              <h1 className="text-2xl font-bold text-gray-900">{release.title}</h1>
              {release.artists && release.artists.length > 0 && (
                <p className="text-gray-600 mt-1">
                  by {release.artists.map(artist => artist.name).join(", ")}
                </p>
              )}
            </div>
            {/* Delete Button */}
            <DeleteReleaseButton
              releaseId={release.id}
              releaseTitle={release.title}
              onDeleteSuccess={handleDeleteSuccess}
              onDeleteError={handleDeleteError}
            />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left Column - Images */}
          <div className="lg:col-span-1">
            <ImageGallery images={release.images} title={release.title} />
            
            {/* Quick Info */}
            <div className="bg-white rounded-lg border border-gray-200 p-6 mt-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Release Info</h3>
              <dl className="space-y-3">
                {release.releaseYear && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Release Year</dt>
                    <dd className="text-sm text-gray-900">{new Date(release.releaseYear).getFullYear()}</dd>
                  </div>
                )}
                {release.origReleaseYear && release.origReleaseYear !== release.releaseYear && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Original Release Year</dt>
                    <dd className="text-sm text-gray-900">{new Date(release.origReleaseYear).getFullYear()}</dd>
                  </div>
                )}
                {release.format && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Format</dt>
                    <dd className="text-sm text-gray-900">{release.format.name}</dd>
                  </div>
                )}
                {release.packaging && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Packaging</dt>
                    <dd className="text-sm text-gray-900">{release.packaging.name}</dd>
                  </div>
                )}
                {release.label && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Label</dt>
                    <dd className="text-sm text-gray-900">{release.label.name}</dd>
                  </div>
                )}
                {release.labelNumber && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Catalog Number</dt>
                    <dd className="text-sm text-gray-900">{release.labelNumber}</dd>
                  </div>
                )}
                {release.upc && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">UPC/Barcode</dt>
                    <dd className="text-sm text-gray-900 font-mono">{release.upc}</dd>
                  </div>
                )}
                {release.country && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Country</dt>
                    <dd className="text-sm text-gray-900">{release.country.name}</dd>
                  </div>
                )}
                {(release.lengthInSeconds && release.lengthInSeconds > 0) ? (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Duration</dt>
                    <dd className="text-sm text-gray-900">{formatDuration(release.lengthInSeconds)}</dd>
                  </div>
                ) : null}
                {release.live && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Type</dt>
                    <dd className="text-sm text-gray-900">Live Recording</dd>
                  </div>
                )}
              </dl>
            </div>

            {/* Purchase Info */}
            {release.purchaseInfo && (
              <div className="bg-white rounded-lg border border-gray-200 p-6 mt-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Purchase Information</h3>
                <dl className="space-y-4">
                  {/* Store Information */}
                  {(release.purchaseInfo.storeName || release.purchaseInfo.storeId) && (
                    <div>
                      <dt className="text-sm font-medium text-gray-500">Store</dt>
                      <dd className="text-sm text-gray-900">
                        {release.purchaseInfo.storeName || `Store ID: ${release.purchaseInfo.storeId}`}
                      </dd>
                    </div>
                  )}
                  
                  {/* Price Information */}
                  {release.purchaseInfo.price && (
                    <div>
                      <dt className="text-sm font-medium text-gray-500">Purchase Price</dt>
                      <dd className="text-sm text-gray-900">
                        <span className="font-medium">
                          {release.purchaseInfo.currency === 'GBP' || !release.purchaseInfo.currency 
                            ? `£${release.purchaseInfo.price.toFixed(2)}`
                            : `${release.purchaseInfo.currency} ${release.purchaseInfo.price.toFixed(2)}`
                          }
                        </span>
                      </dd>
                    </div>
                  )}
                  
                  {/* Purchase Date */}
                  {release.purchaseInfo.purchaseDate && (
                    <div>
                      <dt className="text-sm font-medium text-gray-500">Purchase Date</dt>
                      <dd className="text-sm text-gray-900">
                        {new Date(release.purchaseInfo.purchaseDate).toLocaleDateString('en-US', {
                          weekday: 'long',
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric'
                        })}
                      </dd>
                    </div>
                  )}
                  
                  {/* Purchase Notes */}
                  {release.purchaseInfo.notes && (
                    <div>
                      <dt className="text-sm font-medium text-gray-500">Notes</dt>
                      <dd className="text-sm text-gray-900 bg-gray-50 p-3 rounded-md border">
                        {release.purchaseInfo.notes}
                      </dd>
                    </div>
                  )}

                </dl>

              </div>
            )}
          </div>

          {/* Right Column - Details */}
          <div className="lg:col-span-2 space-y-6">
            {/* Genres */}
            {release.genres && release.genres.length > 0 && (
              <div className="bg-white rounded-lg border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Genres</h3>
                <div className="flex flex-wrap gap-2">
                  {release.genres.map((genre) => (
                    <span
                      key={genre.id}
                      className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-800"
                    >
                      {genre.name}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Tracklist */}
            {release.media && release.media.length > 0 && (
              <TrackList 
                media={release.media} 
                albumArtists={release.artists?.map(artist => artist.name) || []}
              />
            )}

            {/* Links */}
            {release.links && release.links.length > 0 && (
              <ReleaseLinks links={release.links} />
            )}

            {/* Collection Info */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Collection Info</h3>
              <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <dt className="text-sm font-medium text-gray-500">Date Added</dt>
                  <dd className="text-sm text-gray-900">
                    {new Date(release.dateAdded).toLocaleDateString()}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Last Modified</dt>
                  <dd className="text-sm text-gray-900">
                    {new Date(release.lastModified).toLocaleDateString()}
                  </dd>
                </div>
              </dl>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
