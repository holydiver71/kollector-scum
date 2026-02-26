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
import { Play, Check, X, List, ChevronLeft } from "lucide-react";

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
      <div className="min-h-screen bg-transparent flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-transparent flex items-center justify-center">
        <div className="text-center bg-[#13131F] border border-[#1C1C28] rounded-2xl p-8">
          <div className="text-[#8B5CF6] text-xl mb-4">Error loading release</div>
          <p className="text-gray-400 mb-4">{error}</p>
          <button
            onClick={() => router.back()}
            className="bg-[#8B5CF6] hover:bg-[#7C3AED] text-white px-4 py-2 rounded-xl transition-colors"
          >
            Go Back
          </button>
        </div>
      </div>
    );
  }

  if (!release) {
    return (
      <div className="min-h-screen bg-transparent flex items-center justify-center">
        <div className="text-center bg-[#13131F] border border-[#1C1C28] rounded-2xl p-8">
          <div className="text-gray-400 text-xl mb-4">Release not found</div>
          <Link
            href="/collection"
            className="bg-[#8B5CF6] hover:bg-[#7C3AED] text-white px-4 py-2 rounded-xl transition-colors"
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
    <div className="min-h-screen bg-transparent p-4 md:p-8 text-white">
      <div className="max-w-[1400px] mx-auto space-y-6">
        {/* Back button */}
        <button
          onClick={() => router.back()}
          className="flex items-center gap-1 text-gray-400 hover:text-white text-sm transition-colors"
        >
          <ChevronLeft className="w-4 h-4" />
          Back to Collection
        </button>

        <div className="grid lg:grid-cols-3 gap-8">
          {/* Left Column - Cover & Metadata */}
          <div className="space-y-4">
            {/* Cover art */}
            <div className="aspect-square bg-[#13131F] rounded-2xl border border-[#1C1C28] overflow-hidden relative">
              <ImageGallery images={release.images} title={release.title} />
            </div>

            {/* Action buttons */}
            <div className="flex gap-2">
              {/* Now Playing */}
              <div className="relative flex-1">
                <button
                  onClick={handleNowPlayingClick}
                  disabled={isPlayingLoading}
                  className={`w-full py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-colors ${
                    isPlaying
                      ? 'bg-emerald-600 text-white'
                      : 'bg-[#8B5CF6] hover:bg-[#7C3AED] text-white'
                  } ${isPlayingLoading ? 'opacity-70 cursor-not-allowed' : ''}`}
                  title={isPlaying ? 'Playing Now' : 'Mark as Played'}
                >
                  {isPlaying ? <Check className="w-4 h-4" /> : <Play className="w-4 h-4 fill-current" />}
                  {isPlaying ? 'Playing Now' : 'Mark as Played'}
                </button>
                {/* Confirmation Panel */}
                <div
                  className={`absolute left-0 top-full mt-2 bg-[#13131F] rounded-xl border border-[#1C1C28] shadow-xl p-3 z-20 min-w-[260px] transition-all duration-200 ${
                    showConfirmation ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-2 pointer-events-none'
                  }`}
                >
                  {confirmationTime && (
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs text-gray-400 font-medium">
                        Confirm play at {confirmationTime.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}?
                      </span>
                      <div className="flex gap-1">
                        <button
                          onClick={handleConfirm}
                          className="w-7 h-7 rounded-full bg-emerald-600/20 text-emerald-400 hover:bg-emerald-600/30 flex items-center justify-center transition-colors"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                        <button
                          onClick={handleCancel}
                          className="w-7 h-7 rounded-full bg-red-600/20 text-red-400 hover:bg-red-600/30 flex items-center justify-center transition-colors"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Add to list */}
              <button
                onClick={() => setShowAddToList(true)}
                className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-[#8B5CF6] transition-colors"
                title="Add to List"
              >
                <List className="w-5 h-5" />
              </button>

              {/* Discogs link */}
              {getDiscogsLink() && (
                <a
                  href={getDiscogsLink() || ''}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-white transition-colors"
                  title="View on Discogs"
                >
                  <svg className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm0-14c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm0 10c-2.21 0-4-1.79-4-4s1.79-4 4-4 4 1.79 4 4-1.79 4-4 4zm0-6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" />
                  </svg>
                </a>
              )}

              {/* Edit */}
              <div className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center">
                <EditReleaseButton releaseId={release.id} releaseTitle={release.title} />
              </div>

              {/* Delete */}
              <div className="w-12 h-12 bg-[#13131F] border border-red-900/30 rounded-xl flex items-center justify-center">
                <DeleteReleaseButton
                  releaseId={release.id}
                  releaseTitle={release.title}
                  onDeleteSuccess={handleDeleteSuccess}
                  onDeleteError={handleDeleteError}
                />
              </div>
            </div>

            {/* Release Info */}
            {[
              {
                title: "Release Info",
                items: [
                  release.releaseYear && ["Year", String(new Date(release.releaseYear).getFullYear())],
                  release.origReleaseYear && release.origReleaseYear !== release.releaseYear && ["Orig. Year", String(new Date(release.origReleaseYear).getFullYear())],
                  release.format?.name && ["Format", release.format.name],
                  release.packaging?.name && ["Packaging", release.packaging.name],
                  release.label?.name && ["Label", release.label.name],
                  release.labelNumber && ["Cat #", release.labelNumber],
                  release.country?.name && ["Country", release.country.name],
                  release.upc && ["Barcode", release.upc],
                  (release.lengthInSeconds && release.lengthInSeconds > 0) && ["Duration", formatDuration(release.lengthInSeconds) || ""],
                ].filter(Boolean) as [string, string][],
              },
              ...(release.purchaseInfo ? [{
                title: "Purchase Info",
                items: [
                  (release.purchaseInfo.storeName || release.purchaseInfo.storeId) && ["Store", String(release.purchaseInfo.storeName || release.purchaseInfo.storeId)],
                  release.purchaseInfo.price !== undefined && release.purchaseInfo.price !== null && ["Price",
                    release.purchaseInfo.currency === 'GBP' || !release.purchaseInfo.currency
                      ? `£${release.purchaseInfo.price.toFixed(2)}`
                      : `${release.purchaseInfo.currency} ${release.purchaseInfo.price.toFixed(2)}`
                  ],
                  release.purchaseInfo.purchaseDate && ["Date", new Date(release.purchaseInfo.purchaseDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })],
                  release.purchaseInfo.notes && ["Notes", release.purchaseInfo.notes],
                ].filter(Boolean) as [string, string][],
              }] : []),
              {
                title: "Collection Data",
                items: [
                  ["Added", new Date(release.dateAdded).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })],
                  ["Modified", new Date(release.lastModified).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })],
                  release.lastPlayedAt && ["Last Played", new Date(release.lastPlayedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })],
                ].filter(Boolean) as [string, string][],
              },
            ].map(({ title, items }) => items.length > 0 && (
              <div key={title} className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] space-y-2">
                <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-3">{title}</h3>
                {items.map(([k, v]) => (
                  <div key={k} className="flex justify-between text-sm">
                    <span className="text-gray-500">{k}</span>
                    <span className="text-white font-medium text-right max-w-[60%] break-words">{v}</span>
                  </div>
                ))}
              </div>
            ))}

            {/* Play History */}
            {release.lastPlayedAt && (
              <div className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28]">
                <div className="flex items-center justify-between mb-2">
                  <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest">Play History</h3>
                  <button
                    onClick={handlePlayHistoryToggle}
                    className="text-xs text-[#8B5CF6] hover:text-[#A78BFA] transition-colors"
                  >
                    {showPlayHistory ? 'Hide' : 'Show'}
                  </button>
                </div>
                {showPlayHistory && (
                  <div className="text-xs space-y-1 mt-2">
                    {playHistoryLoading ? (
                      <div className="text-gray-500 text-center py-2">Loading history...</div>
                    ) : playHistory ? (
                      <>
                        <div className="text-gray-400 mb-2">Total Plays: <span className="text-white font-bold">{playHistory.playCount}</span></div>
                        <div className={`space-y-1 ${playHistory.playDates.length > 5 ? 'max-h-[150px] overflow-y-auto' : ''}`}>
                          {playHistory.playDates.map((item) => (
                            <div key={item.id} className="flex justify-between items-center group">
                              <span className="text-gray-400">
                                {new Date(item.playedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}
                              </span>
                              <button
                                onClick={() => handleDeletePlayHistory(item.id)}
                                className="text-red-400 opacity-0 group-hover:opacity-100 hover:text-red-300 transition-all"
                                title="Delete this play"
                              >
                                <X className="w-3 h-3" />
                              </button>
                            </div>
                          ))}
                        </div>
                      </>
                    ) : (
                      <div className="text-red-400">Failed to load history</div>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Right Column - Title, Tracklist & Extended Info */}
          <div className="lg:col-span-2 space-y-6">
            {/* Title area */}
            <div>
              {release.artists && release.artists.length > 0 && (
                <p className="text-[#8B5CF6] font-semibold text-sm mb-1">
                  {release.artists.map((a, i) => (
                    <span key={a.id}>
                      <Link href={`/collection?artistId=${a.id}`} className="hover:text-[#A78BFA] transition-colors">{a.name}</Link>
                      {release.artists && i < release.artists.length - 1 && " & "}
                    </span>
                  ))}
                </p>
              )}
              <h1 className="text-4xl font-black tracking-tight leading-tight text-white">{release.title}</h1>
              <div className="flex flex-wrap gap-2 mt-2">
                {release.format?.name && (
                  <span className="text-xs bg-[#8B5CF6] text-white px-2 py-1 rounded font-semibold">{release.format.name}</span>
                )}
                {release.live && (
                  <span className="text-xs bg-red-600/20 text-red-400 px-2 py-1 rounded font-semibold">Live Recording</span>
                )}
                {release.genres?.map((g) => (
                  <Link key={g.id} href={`/collection?genreId=${g.id}`} className="text-xs bg-[#8B5CF6]/15 text-[#A78BFA] px-2 py-1 rounded hover:bg-[#8B5CF6]/25 transition-colors">{g.name}</Link>
                ))}
              </div>
            </div>

            {/* Tracklist */}
            <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
              <div className="px-4 py-3 border-b border-[#1C1C28]">
                <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest">Tracklist</h3>
              </div>
              <TrackList
                media={release.media || []}
                albumArtists={release.artists?.map(artist => artist.name) || []}
              />
            </div>

            {/* Links */}
            {release.links && release.links.length > 0 && (
              <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
                <div className="px-4 py-3 border-b border-[#1C1C28] flex items-center justify-between">
                  <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2">
                    <svg width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2" className="text-[#8B5CF6]"><path strokeLinecap="round" strokeLinejoin="round" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" /></svg>
                    Links
                  </h3>
                  <span className="text-[10px] text-gray-600">{release.links.length} link{release.links.length !== 1 ? 's' : ''}</span>
                </div>
                <ReleaseLinks links={release.links} />
              </div>
            )}
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
