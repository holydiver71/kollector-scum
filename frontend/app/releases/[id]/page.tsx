"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { fetchJson, createNowPlaying, getPlayHistory, deleteNowPlaying, PlayHistoryDto } from "../../lib/api";
import { LoadingSpinner } from "../../components/LoadingComponents";
import { ImageGallery } from "../../components/ImageGallery";
import { TrackList } from "../../components/TrackList";
import { ReleaseLinks } from "../../components/ReleaseLinks";
import { DeleteReleaseButton } from "../../components/DeleteReleaseButton";
import { EditReleaseButton } from "../../components/EditReleaseButton";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { Play, Check, X, ChevronDown } from "lucide-react";

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
  lastPlayedAt?: string;
}

export default function ReleaseDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  
  const [release, setRelease] = useState<DetailedMusicRelease | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isPlayingLoading, setIsPlayingLoading] = useState(false);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [confirmationTime, setConfirmationTime] = useState<Date | null>(null);
  const [showPlayHistory, setShowPlayHistory] = useState(false);
  const [playHistory, setPlayHistory] = useState<PlayHistoryDto | null>(null);
  const [playHistoryLoading, setPlayHistoryLoading] = useState(false);
  const [playToDelete, setPlayToDelete] = useState<number | null>(null);

  const handleNowPlayingClick = () => {
    setConfirmationTime(new Date());
    setShowConfirmation(true);
  };

  const handleConfirm = async () => {
    if (isPlayingLoading || !release) return;
    
    setIsPlayingLoading(true);
    try {
      await createNowPlaying(release.id);
      setIsPlaying(true);
      // Update the release with the new last played date
      setRelease(prev => prev ? { ...prev, lastPlayedAt: new Date().toISOString() } : prev);
      setShowConfirmation(false);
      // Reset play history so it will be refetched when panel is opened again
      setPlayHistory(null);
    } catch (err) {
      console.error('Failed to record now playing:', err);
    } finally {
      setIsPlayingLoading(false);
    }
  };

  const handleCancel = () => {
    setShowConfirmation(false);
    setConfirmationTime(null);
  };

  const handlePlayHistoryToggle = async () => {
    if (!release) return;
    
    if (showPlayHistory) {
      setShowPlayHistory(false);
      return;
    }
    
    setShowPlayHistory(true);
    
    // Fetch play history if not already loaded
    if (!playHistory) {
      setPlayHistoryLoading(true);
      try {
        const history = await getPlayHistory(release.id);
        setPlayHistory(history);
      } catch (err) {
        console.error('Failed to fetch play history:', err);
      } finally {
        setPlayHistoryLoading(false);
      }
    }
  };

  const handleDeletePlayHistory = (playId: number) => {
    setPlayToDelete(playId);
  };

  const confirmDeletePlayHistory = async () => {
    if (!playToDelete) return;

    try {
      await deleteNowPlaying(playToDelete);
      
      // Update local state
      if (playHistory) {
        const updatedDates = playHistory.playDates.filter(p => p.id !== playToDelete);
        setPlayHistory({
          ...playHistory,
          playCount: playHistory.playCount - 1,
          playDates: updatedDates
        });
        
        // If we deleted the most recent play, update the release's lastPlayedAt
        // Since playDates are sorted descending, the first one is the most recent
        if (updatedDates.length > 0) {
           const mostRecent = updatedDates[0].playedAt;
           setRelease(prev => prev ? { ...prev, lastPlayedAt: mostRecent } : prev);
        } else {
           setRelease(prev => prev ? { ...prev, lastPlayedAt: undefined } : prev);
        }
      }
    } catch (err) {
      console.error('Failed to delete play history:', err);
      alert('Failed to delete play history record');
    } finally {
      setPlayToDelete(null);
    }
  };

  const getDeleteMessage = () => {
    if (!playToDelete || !playHistory) return "Are you sure you want to delete this play record? This action cannot be undone.";
    
    const playItem = playHistory.playDates.find(p => p.id === playToDelete);
    if (!playItem) return "Are you sure you want to delete this play record? This action cannot be undone.";

    const dateStr = new Date(playItem.playedAt).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    return `Are you sure you want to delete the play record from ${dateStr}? This action cannot be undone.`;
  };

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
        <div className="mb-8 flex items-start justify-between gap-8">
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
                    {release.artists && index < release.artists.length - 1 && (
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

          {/* Now Playing Button with Confirmation Panel - Below Album Title */}
          <div className="relative mt-4 h-[42px] overflow-hidden">
            <div className="absolute left-0 top-0 flex items-center">
              {/* Now Playing Button - Always on left */}
              <button
                onClick={handleNowPlayingClick}
                disabled={isPlayingLoading}
                className={`relative z-10 inline-flex items-center gap-2 px-4 py-2 rounded-full font-semibold text-sm transition-all ${
                  isPlaying 
                    ? 'bg-green-500 text-white' 
                    : 'bg-red-100 hover:bg-gray-200 text-gray-700'
                } ${isPlayingLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
                title={isPlaying ? 'Playing now' : 'Mark as now playing'}
              >
                {isPlaying ? (
                  <>
                    <Check className="w-4 h-4" />
                    Playing
                  </>
                ) : (
                  <>
                    <Play className="w-4 h-4" />
                    Now Playing
                  </>
                )}
              </button>

              {/* Confirmation Panel - Positioned behind button, slides out to right */}
              <div 
                className={`absolute left-0 flex items-center gap-3 pl-[140px] transition-all duration-300 ease-in-out ${
                  showConfirmation ? 'opacity-100' : 'opacity-0 pointer-events-none'
                }`}
                style={{
                  transform: showConfirmation ? 'translateX(0)' : 'translateX(-100%)'
                }}
              >
                {confirmationTime && (
                  <>
                    <span className="text-sm text-gray-700 font-medium whitespace-nowrap">
                      {confirmationTime.toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                      })} {confirmationTime.toLocaleTimeString('en-US', {
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </span>
                    <button
                      onClick={handleConfirm}
                      disabled={isPlayingLoading}
                      className="w-8 h-8 rounded-full bg-green-500 hover:bg-green-600 flex items-center justify-center transition-colors disabled:opacity-50"
                      title="Confirm"
                    >
                      <Check className="w-5 h-5 text-white" />
                    </button>
                    <button
                      onClick={handleCancel}
                      disabled={isPlayingLoading}
                      className="w-8 h-8 rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center transition-colors disabled:opacity-50"
                      title="Cancel"
                    >
                      <X className="w-5 h-5 text-white" />
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
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
      </div>

      {/* Two Column Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-[max-content_1fr] gap-16">
          {/* Left Column - Album Cover */}
          <div className="max-w-md">
            <ImageGallery images={release.images} title={release.title} />
          </div>

          {/* Right Column - Details */}
          <div className="space-y-8">
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
                {release.lastPlayedAt && (
                  <div className="relative">
                    <div className="flex items-baseline gap-2">
                      <dt className="text-xs text-gray-500 min-w-[80px] font-semibold">Last Played</dt>
                      <dd className="text-sm text-gray-900 font-medium flex items-center gap-1">
                        {new Date(release.lastPlayedAt).toLocaleDateString('en-US', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                        <button
                          onClick={handlePlayHistoryToggle}
                          className="p-0.5 text-gray-500 hover:text-gray-700 transition-colors"
                          title="Show play history"
                        >
                          <ChevronDown 
                            className={`w-4 h-4 transition-transform duration-200 ${showPlayHistory ? 'rotate-180' : ''}`} 
                          />
                        </button>
                      </dd>
                    </div>
                    
                    {/* Play History Panel */}
                    {showPlayHistory && (
                      <div className="mt-3 ml-[88px] p-3 bg-gray-50 rounded-lg border border-gray-200 max-w-xs">
                        {playHistoryLoading ? (
                          <div className="text-sm text-gray-500">Loading...</div>
                        ) : playHistory ? (
                          <div>
                            <div className="text-sm font-semibold text-gray-900 mb-2">
                              Played {playHistory.playCount} time{playHistory.playCount !== 1 ? 's' : ''}
                            </div>
                            {playHistory.playDates.length > 0 && (
                              <div 
                                className={`space-y-1 ${playHistory.playDates.length > 10 ? 'max-h-[240px] overflow-y-auto pr-2' : ''}`}
                              >
                                {playHistory.playDates.map((item) => (
                                  <div key={item.id} className="text-xs text-gray-600 flex items-center gap-2 group h-6">
                                    <span>
                                    {new Date(item.playedAt).toLocaleDateString('en-US', {
                                      year: 'numeric',
                                      month: 'short',
                                      day: 'numeric',
                                      hour: '2-digit',
                                      minute: '2-digit'
                                    })}
                                    </span>
                                    <button
                                      onClick={() => handleDeletePlayHistory(item.id)}
                                      className="text-red-500 opacity-0 group-hover:opacity-100 transition-opacity p-0.5 hover:bg-red-50 rounded"
                                      title="Delete this play"
                                    >
                                      <X className="w-3 h-3" />
                                    </button>
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        ) : (
                          <div className="text-sm text-gray-500">No play history available</div>
                        )}
                      </div>
                    )}
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
                    {release.purchaseInfo.price !== undefined && release.purchaseInfo.price !== null && (
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
                  {release.purchaseInfo.price !== undefined && release.purchaseInfo.price !== null && (
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

      <ConfirmDialog
        isOpen={!!playToDelete}
        title="Delete Play Record"
        message={getDeleteMessage()}
        confirmLabel="Delete"
        isDangerous={true}
        onConfirm={confirmDeletePlayHistory}
        onCancel={() => setPlayToDelete(null)}
      />
    </div>
  );
}
