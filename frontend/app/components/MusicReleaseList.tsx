"use client";
import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchJson } from "../lib/api";
import { LoadingSpinner, Skeleton } from "./LoadingComponents";

// Type definitions for music releases
interface MusicRelease {
  id: number;
  title: string;
  releaseYear: number;
  origReleaseYear?: number;
  artists?: string[];
  genres?: string[];
  live: boolean;
  labelName?: string;
  countryName?: string;
  formatName?: string;
  labelNumber?: string;
  lengthInSeconds?: number;
  images?: Array<{ type: string; uri: string; height: number; width: number }>;
  dateAdded: string;
  lastModified: string;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface MusicReleaseFilters {
  search?: string;
  artistId?: number;
  genreId?: number;
  labelId?: number;
  countryId?: number;
  formatId?: number;
  live?: boolean;
}

interface MusicReleaseListProps {
  filters?: MusicReleaseFilters;
  pageSize?: number;
}

export function MusicReleaseCard({ release }: { release: MusicRelease }) {
  const formatDuration = (seconds?: number) => {
    if (!seconds) return null;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  const getCoverImage = () => {
    const coverImage = release.images?.find(img => img.type === "primary");
    return coverImage?.uri;
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
      <div className="p-6">
        <div className="flex items-start gap-4">
          {/* Cover Art */}
          <div className="flex-shrink-0">
            {getCoverImage() ? (
              <img
                src={getCoverImage()}
                alt={`${release.title} cover`}
                className="w-16 h-16 rounded-md object-cover"
                onError={(e) => {
                  e.currentTarget.style.display = 'none';
                }}
              />
            ) : (
              <div className="w-16 h-16 bg-gray-200 rounded-md flex items-center justify-center">
                <span className="text-gray-400 text-2xl">🎵</span>
              </div>
            )}
          </div>

          {/* Release Info */}
          <div className="flex-grow min-w-0">
            <div className="flex items-start justify-between">
              <div className="min-w-0 flex-grow">
                <h3 className="text-lg font-medium text-gray-900 truncate">
                  <Link 
                    href={`/releases/${release.id}`}
                    className="hover:text-blue-600 transition-colors"
                  >
                    {release.title}
                  </Link>
                  {release.live && (
                    <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">
                      LIVE
                    </span>
                  )}
                </h3>
                
                {release.artists && release.artists.length > 0 && (
                  <p className="text-sm text-gray-600 mt-1">
                    {release.artists.join(", ")}
                  </p>
                )}

                <div className="flex items-center gap-4 mt-2 text-sm text-gray-500">
                  <span>{release.releaseYear}</span>
                  {release.origReleaseYear && release.origReleaseYear !== release.releaseYear && (
                    <span>(orig. {release.origReleaseYear})</span>
                  )}
                  {release.formatName && <span>{release.formatName}</span>}
                  {release.lengthInSeconds && (
                    <span>{formatDuration(release.lengthInSeconds)}</span>
                  )}
                </div>

                <div className="flex items-center gap-2 mt-2 text-sm text-gray-500">
                  {release.labelName && (
                    <span className="bg-gray-100 px-2 py-1 rounded text-xs">
                      {release.labelName}
                    </span>
                  )}
                  {release.countryName && (
                    <span className="bg-gray-100 px-2 py-1 rounded text-xs">
                      {release.countryName}
                    </span>
                  )}
                  {release.labelNumber && (
                    <span className="bg-gray-100 px-2 py-1 rounded text-xs font-mono">
                      {release.labelNumber}
                    </span>
                  )}
                </div>

                {release.genres && release.genres.length > 0 && (
                  <div className="flex flex-wrap gap-1 mt-2">
                    {release.genres.map((genre, index) => (
                      <span
                        key={index}
                        className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800"
                      >
                        {genre}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function MusicReleaseList({ filters = {}, pageSize = 20 }: MusicReleaseListProps) {
  const [releases, setReleases] = useState<MusicRelease[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);

  const fetchReleases = async (page: number = 1) => {
    try {
      setLoading(true);
      setError(null);

      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
        ...(filters.search && { search: filters.search }),
        ...(filters.artistId && { artistId: filters.artistId.toString() }),
        ...(filters.genreId && { genreId: filters.genreId.toString() }),
        ...(filters.labelId && { labelId: filters.labelId.toString() }),
        ...(filters.countryId && { countryId: filters.countryId.toString() }),
        ...(filters.formatId && { formatId: filters.formatId.toString() }),
        ...(filters.live !== undefined && { live: filters.live.toString() })
      });

      const response: PagedResult<MusicRelease> = await fetchJson(`/api/musicreleases?${params}`);
      
      setReleases(response.items);
      setCurrentPage(response.page);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load releases");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setCurrentPage(1);
    fetchReleases(1);
  }, [filters, pageSize]);

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    fetchReleases(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  if (loading && releases.length === 0) {
    return (
      <div className="space-y-4">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-start gap-4">
              <div className="w-16 h-16 bg-gray-200 rounded-md animate-pulse" />
              <div className="flex-grow space-y-2">
                <Skeleton lines={3} />
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-red-200 p-8 text-center">
        <div className="text-red-600 mb-4">
          <svg className="w-12 h-12 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.502 0L4.312 15.5c-.77.833.192 2.5 1.732 2.5z" />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-gray-900 mb-2">Error Loading Releases</h3>
        <p className="text-gray-600 mb-4">{error}</p>
        <button
          onClick={() => fetchReleases(currentPage)}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (releases.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-8 text-center">
        <div className="text-gray-400 mb-4">
          <svg className="w-12 h-12 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6-4h6m2 5.291A7.962 7.962 0 0112 15c-2.34 0-4.441.935-5.982 2.457M16.5 4.5L19 7l-2.5 2.5M4.5 4.5L7 7 4.5 9.5" />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-gray-900 mb-2">No Releases Found</h3>
        <p className="text-gray-600">No music releases match your current filters.</p>
      </div>
    );
  }

  return (
    <div>
      {/* Results Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="text-sm text-gray-600">
          Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} releases
        </div>
        {loading && <LoadingSpinner />}
      </div>

      {/* Release Cards */}
      <div className="space-y-4 mb-8">
        {releases.map((release) => (
          <MusicReleaseCard key={release.id} release={release} />
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage <= 1 || loading}
            className="px-3 py-2 text-sm border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Previous
          </button>

          <div className="flex items-center gap-1">
            {[...Array(Math.min(5, totalPages))].map((_, i) => {
              const pageNum = Math.max(1, Math.min(currentPage - 2 + i, totalPages - 4 + i));
              const isCurrentPage = pageNum === currentPage;
              
              return (
                <button
                  key={pageNum}
                  onClick={() => handlePageChange(pageNum)}
                  disabled={loading}
                  className={`px-3 py-2 text-sm border rounded-md disabled:cursor-not-allowed ${
                    isCurrentPage
                      ? "bg-blue-600 text-white border-blue-600"
                      : "border-gray-300 hover:bg-gray-50"
                  }`}
                >
                  {pageNum}
                </button>
              );
            })}
          </div>

          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage >= totalPages || loading}
            className="px-3 py-2 text-sm border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
