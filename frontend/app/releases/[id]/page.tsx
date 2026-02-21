"use client";
export const runtime = 'edge';
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
import { AddToListDialog } from "../../components/AddToListDialog";
import { Play, Check, X, ChevronDown, List, ChevronLeft } from "lucide-react";

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
  const [showAddToList, setShowAddToList] = useState(false);

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
    <div className="min-h-screen bg-transparent p-4 md:p-8">
      <div className="max-w-[1400px] mx-auto">
        {/* Header Actions Row */}
        <div className="flex items-center justify-between mb-6">
          <button
            onClick={() => router.back()}
            className="bg-white text-gray-600 hover:text-[#D93611] shadow-sm hover:shadow-md px-4 py-2 rounded-lg transition-all flex items-center gap-2 font-medium text-sm"
          >
            <ChevronLeft className="w-4 h-4" />
            Back to Collection
          </button>
          
          <div className="flex items-center gap-3">
            <div className="bg-white rounded-lg shadow-sm p-1 flex gap-1">
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

        {/* Main Content Card */}
        <div className="bg-white rounded-2xl shadow-xl overflow-hidden">
          <div className="p-6 md:p-10 lg:p-12">
            
            {/* Header Section */}
            <div className="flex flex-col md:flex-row md:items-start justify-between gap-8 mb-12 border-b border-gray-100 pb-8">
              <div className="flex-1">
                {/* Artist Name */}
                {release.artists && release.artists.length > 0 && (
                  <div className="mb-2">
                    {release.artists.map((artist, index) => (
                      <span key={artist.id}>
                        <Link
                          href={`/collection?artistId=${artist.id}`}
                          className="text-4xl md:text-5xl lg:text-6xl font-black text-gray-900 hover:text-[#D93611] transition-colors tracking-tight leading-none"
                        >
                          {artist.name}
                        </Link>
                        {release.artists && index < release.artists.length - 1 && (
                          <span className="text-4xl md:text-5xl lg:text-6xl font-light text-gray-300"> & </span>
                        )}
                      </span>
                    ))}
                  </div>
                )}

                {/* Album Title */}
                <h1 className="text-2xl md:text-3xl text-gray-500 font-medium tracking-wide">
                  {release.title}
                </h1>
                
                {/* Badges Row */}
                <div className="flex flex-wrap items-center gap-3 mt-4">
                   <div className="px-3 py-1 rounded-full bg-[#D9601A] text-white text-xs font-bold uppercase tracking-wider">
                      {release.format?.name || 'Unknown Format'}
                   </div>
                   <div className="px-3 py-1 rounded-full bg-gray-100 text-gray-600 text-xs font-bold uppercase tracking-wider">
                      #{release.id}
                   </div>
                   {release.live && (
                      <div className="px-3 py-1 rounded-full bg-red-100 text-red-600 text-xs font-bold uppercase tracking-wider">
                        Live Recording
                      </div>
                   )}
                </div>
              </div>

              {/* Actions & External Links */}
              <div className="flex items-center gap-3">
                 {/* Add to List Button */}
                 <button
                    onClick={() => setShowAddToList(true)}
                    className="w-12 h-12 rounded-full bg-orange-100 text-[#D93611] hover:bg-orange-200 flex items-center justify-center transition-all shadow-sm hover:shadow-md transform hover:-translate-y-0.5"
                    title="Add to list"
                 >
                    <List className="w-6 h-6" />
                 </button>

                 {/* Now Playing Button */}
                 <div className="relative">
                    <button
                      onClick={handleNowPlayingClick}
                      disabled={isPlayingLoading}
                      className={`w-12 h-12 rounded-full flex items-center justify-center transition-all shadow-md hover:shadow-lg transform hover:-translate-y-0.5 ${
                        isPlaying 
                          ? 'bg-green-500 text-white' 
                          : 'bg-[#D93611] text-white hover:bg-[#b92b0b]'
                      } ${isPlayingLoading ? 'opacity-70 cursor-not-allowed' : ''}`}
                      title={isPlaying ? 'Playing Now' : 'Play Now'}
                    >
                      {isPlaying ? <Check className="w-6 h-6" /> : <Play className="w-6 h-6 fill-current" />}
                    </button>
                    
                    {/* Confirmation Panel */}
                    <div 
                      className={`absolute right-0 top-full mt-2 bg-white rounded-xl shadow-xl border border-gray-100 p-3 z-20 min-w-[280px] transition-all duration-200 ${
                        showConfirmation ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-2 pointer-events-none'
                      }`}
                    >
                      {confirmationTime && (
                        <div className="flex items-center justify-between gap-2">
                          <span className="text-xs text-gray-500 font-medium">
                            Confirm play at {confirmationTime.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}?
                          </span>
                          <div className="flex gap-1">
                            <button
                              onClick={handleConfirm}
                              className="w-7 h-7 rounded-full bg-green-100 text-green-600 hover:bg-green-200 flex items-center justify-center transition-colors"
                            >
                              <Check className="w-4 h-4" />
                            </button>
                            <button
                              onClick={handleCancel}
                              className="w-7 h-7 rounded-full bg-red-100 text-red-600 hover:bg-red-200 flex items-center justify-center transition-colors"
                            >
                              <X className="w-4 h-4" />
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                 </div>

                 {/* Discogs Link */}
                 {getDiscogsLink() && (
                    <a
                      href={getDiscogsLink() || ''}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="w-12 h-12 rounded-full bg-black hover:bg-gray-800 text-white flex items-center justify-center transition-all shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                      title="View on Discogs"
                    >
                      <svg className="w-6 h-6" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm0 10c-2.21 0-4-1.79-4-4s1.79-4 4-4 4 1.79 4 4-1.79 4-4 4zm0-6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" />
                      </svg>
                    </a>
                 )}
              </div>
            </div>

            {/* Content Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-[400px_1fr] xl:grid-cols-[450px_1fr] gap-12 lg:gap-16">
              {/* Left Column - Cover & Quick Stats */}
              <div className="space-y-8">
                <div className="rounded-xl overflow-hidden shadow-2xl bg-gray-100 aspect-square relative">
                   <ImageGallery images={release.images} title={release.title} />
                </div>
                
                {/* Quick Stats / Metadata Card */}
                <div className="bg-gray-50 rounded-xl p-6 border border-gray-100">
                   <h3 className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4">Release Details</h3>
                   <dl className="space-y-3 text-sm">
                      {release.releaseYear && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Year</dt>
                          <dd className="font-medium text-gray-900 text-right">{new Date(release.releaseYear).getFullYear()}</dd>
                        </div>
                      )}
                      {release.origReleaseYear && release.origReleaseYear !== release.releaseYear && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Original Year</dt>
                          <dd className="font-medium text-gray-900 text-right">{new Date(release.origReleaseYear).getFullYear()}</dd>
                        </div>
                      )}
                      {release.packaging && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Packaging</dt>
                          <dd className="font-medium text-gray-900 text-right">{release.packaging.name}</dd>
                        </div>
                      )}
                      {release.label && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Label</dt>
                          <dd className="font-medium text-gray-900 text-right">{release.label.name}</dd>
                        </div>
                      )}
                      {release.labelNumber && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Catalog #</dt>
                          <dd className="font-medium text-gray-900 text-right">{release.labelNumber}</dd>
                        </div>
                      )}
                      {release.upc && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Barcode</dt>
                          <dd className="font-medium text-gray-900 text-right">{release.upc}</dd>
                        </div>
                      )}
                      {release.country && (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Country</dt>
                          <dd className="font-medium text-gray-900 text-right">{release.country.name}</dd>
                        </div>
                      )}
                      {(release.lengthInSeconds && release.lengthInSeconds > 0) ? (
                        <div className="flex justify-between py-1 border-b border-gray-200 last:border-0">
                          <dt className="text-gray-500">Duration</dt>
                          <dd className="font-medium text-gray-900 text-right">{formatDuration(release.lengthInSeconds)}</dd>
                        </div>
                      ) : null}
                      
                      {/* Last Played with History Toggle */}
                      {release.lastPlayedAt && (
                        <div className="pt-2 mt-2 border-t border-gray-200">
                          <div className="flex justify-between items-center mb-2">
                            <dt className="text-gray-500">Last Played</dt>
                            <dd className="font-medium text-gray-900 text-right flex items-center gap-1">
                              {new Date(release.lastPlayedAt).toLocaleDateString('en-US', {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric'
                              })}
                              <button
                                onClick={handlePlayHistoryToggle}
                                className="p-0.5 text-gray-400 hover:text-gray-700 transition-colors"
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
                            <div className="bg-white rounded-lg border border-gray-200 p-3 text-xs">
                              {playHistoryLoading ? (
                                <div className="text-gray-500 text-center py-2">Loading history...</div>
                              ) : playHistory ? (
                                <div>
                                  <div className="font-bold text-gray-900 mb-2 pb-1 border-b border-gray-100">
                                    Total Plays: {playHistory.playCount}
                                  </div>
                                  {playHistory.playDates.length > 0 ? (
                                    <div className={`space-y-1 ${playHistory.playDates.length > 5 ? 'max-h-[150px] overflow-y-auto pr-1 custom-scrollbar' : ''}`}>
                                      {playHistory.playDates.map((item) => (
                                        <div key={item.id} className="flex justify-between items-center group">
                                          <span className="text-gray-600">
                                            {new Date(item.playedAt).toLocaleDateString('en-US', {
                                              year: 'numeric',
                                              month: 'short',
                                              day: 'numeric'
                                            })}
                                          </span>
                                          <button
                                            onClick={() => handleDeletePlayHistory(item.id)}
                                            className="text-red-400 opacity-0 group-hover:opacity-100 hover:text-red-600 transition-all"
                                            title="Delete this play"
                                          >
                                            <X className="w-3 h-3" />
                                          </button>
                                        </div>
                                      ))}
                                    </div>
                                  ) : (
                                    <div className="text-gray-400 italic">No dates recorded</div>
                                  )}
                                </div>
                              ) : (
                                <div className="text-red-500">Failed to load history</div>
                              )}
                            </div>
                          )}
                        </div>
                      )}
                   </dl>
                </div>
              </div>

              {/* Right Column - Tracklist & Extended Info */}
              <div className="space-y-10">
                 {/* Tracklist Section */}
                 <div>
                    <div className="bg-gray-50 rounded-xl p-1 border border-gray-100">
                        <TrackList 
                          media={release.media || []} 
                          albumArtists={release.artists?.map(artist => artist.name) || []}
                        />
                    </div>
                 </div>

                 {/* Purchase & Collection Info Grid */}
                 <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {/* Purchase Info */}
                    {release.purchaseInfo && (
                       <div className="bg-gray-50 rounded-xl p-6 border border-gray-100">
                          <h3 className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4 flex items-center gap-2">
                             <span className="w-2 h-2 rounded-full bg-green-500"></span>
                             Purchase Info
                          </h3>
                          <dl className="space-y-3 text-sm">
                            {(release.purchaseInfo.storeName || release.purchaseInfo.storeId) && (
                              <div className="flex justify-between">
                                <dt className="text-gray-500">Store</dt>
                                <dd className="font-medium text-gray-900">{release.purchaseInfo.storeName || release.purchaseInfo.storeId}</dd>
                              </div>
                            )}
                            {release.purchaseInfo.price !== undefined && release.purchaseInfo.price !== null && (
                              <div className="flex justify-between">
                                <dt className="text-gray-500">Price</dt>
                                <dd className="font-medium text-gray-900">
                                  {release.purchaseInfo.currency === 'GBP' || !release.purchaseInfo.currency 
                                    ? `£${release.purchaseInfo.price.toFixed(2)}`
                                    : `${release.purchaseInfo.currency} ${release.purchaseInfo.price.toFixed(2)}`
                                  }
                                </dd>
                              </div>
                            )}
                            {release.purchaseInfo.purchaseDate && (
                              <div className="flex justify-between">
                                <dt className="text-gray-500">Date</dt>
                                <dd className="font-medium text-gray-900">
                                  {new Date(release.purchaseInfo.purchaseDate).toLocaleDateString('en-US', {
                                    year: 'numeric',
                                    month: 'short',
                                    day: 'numeric'
                                  })}
                                </dd>
                              </div>
                            )}
                            {release.purchaseInfo.notes && (
                              <div className="pt-2 mt-2 border-t border-gray-200">
                                <dt className="text-gray-500 mb-1">Notes</dt>
                                <dd className="text-gray-700 italic text-xs bg-white p-2 rounded border border-gray-100">
                                  {release.purchaseInfo.notes}
                                </dd>
                              </div>
                            )}
                          </dl>
                       </div>
                    )}
                    
                    {/* Collection Info */}
                    <div className="bg-gray-50 rounded-xl p-6 border border-gray-100">
                       <h3 className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4 flex items-center gap-2">
                          <span className="w-2 h-2 rounded-full bg-blue-500"></span>
                          Collection Data
                       </h3>
                       <dl className="space-y-3 text-sm">
                          <div className="flex justify-between">
                            <dt className="text-gray-500">Added</dt>
                            <dd className="font-medium text-gray-900">
                              {new Date(release.dateAdded).toLocaleDateString('en-US', {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric'
                              })}
                            </dd>
                          </div>
                          <div className="flex justify-between">
                            <dt className="text-gray-500">Modified</dt>
                            <dd className="font-medium text-gray-900">
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
                 
                 {/* Genres Tags */}
                 {release.genres && release.genres.length > 0 && (
                    <div>
                       <h3 className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-3">Genres</h3>
                       <div className="flex flex-wrap gap-2">
                          {release.genres.map((genre) => (
                             <Link
                                key={genre.id}
                                href={`/collection?genreId=${genre.id}`}
                                className="px-3 py-1.5 rounded-lg bg-gray-100 text-gray-600 text-sm font-medium hover:bg-[#D93611] hover:text-white transition-colors"
                             >
                                {genre.name}
                             </Link>
                          ))}
                       </div>
                    </div>
                 )}
                 
                 {/* Links */}
                 {release.links && release.links.length > 0 && (
                    <div>
                       <h3 className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-3">External Links</h3>
                       <ReleaseLinks links={release.links} />
                    </div>
                 )}
              </div>
            </div>

          </div>
        </div>
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

      <AddToListDialog
        releaseId={release.id}
        releaseTitle={release.title}
        isOpen={showAddToList}
        onClose={() => setShowAddToList(false)}
      />
    </div>
  );
}
