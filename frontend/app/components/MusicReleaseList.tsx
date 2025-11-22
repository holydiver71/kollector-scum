"use client";
import { useState, useEffect } from "react";
import Link from "next/link";
import { fetchJson } from "../lib/api";
import { LoadingSpinner, Skeleton } from "./LoadingComponents";

// Type definitions for music releases
interface MusicRelease {
  id: number;
  title: string;
  releaseYear: string; // Backend returns DateTime as string
  origReleaseYear?: string; // Backend returns DateTime as string
  artistNames?: string[]; // Backend DTO field name
  genreNames?: string[];  // Backend DTO field name
  labelName?: string;
  countryName?: string;
  formatName?: string;
  coverImageUrl?: string; // Backend DTO field name
  dateAdded: string;
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
  yearFrom?: number;
  yearTo?: number;
  sortBy?: string;
  sortOrder?: string;
}

interface MusicReleaseListProps {
  filters?: MusicReleaseFilters;
  pageSize?: number;
}

export function MusicReleaseCard({ release }: { release: MusicRelease }) {
  const getCoverImageUrl = () => {
    if (release.coverImageUrl) {
      // Check if it's already a full URL
      if (release.coverImageUrl.startsWith('http://') || release.coverImageUrl.startsWith('https://')) {
        return release.coverImageUrl;
      }
      // Otherwise, serve images through the backend API - use full backend URL
      const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
      return `${apiBaseUrl}/api/images/${release.coverImageUrl}`;
    }
    return '/placeholder-album.svg'; // Default placeholder
  };

  const [imageError, setImageError] = useState(false);

  return (
    <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
      <div className="p-4">
        <div className="flex items-start gap-4">
          {/* Cover Art - Left */}
          <div className="flex-shrink-0">
            <Link 
              href={`/releases/${release.id}`}
              className="block hover:opacity-80 transition-opacity"
            >
              {!imageError ? (
                <img
                  src={getCoverImageUrl()}
                  alt={`${release.title} cover`}
                  className="w-36 h-36 rounded-md object-contain border border-gray-200 bg-white cursor-pointer"
                  onError={() => setImageError(true)}
                  onLoad={() => setImageError(false)}
                />
              ) : (
                <div className="w-36 h-36 bg-gray-100 rounded-md flex items-center justify-center border border-gray-200 cursor-pointer">
                  <span className="text-gray-400 text-4xl">🎵</span>
                </div>
              )}
            </Link>
          </div>

          {/* Release Info - Right */}
          <div className="flex-grow min-w-0">
            <h3 className="text-lg font-medium text-gray-900 truncate mb-1">
              <Link 
                href={`/releases/${release.id}`}
                className="hover:text-blue-600 transition-colors"
              >
                {release.title}
              </Link>
            </h3>
            
            {release.artistNames && release.artistNames.length > 0 && (
              <p className="text-sm text-gray-600 mb-2 truncate">
                {release.artistNames.join(", ")}
              </p>
            )}

            <div className="flex items-center gap-2 mb-2 text-sm text-gray-500">
              {release.releaseYear && (
                <span>
                  {new Date(release.releaseYear).getFullYear()}
                  {release.origReleaseYear && 
                   release.origReleaseYear !== release.releaseYear && 
                   ` (${new Date(release.origReleaseYear).getFullYear()})`}
                </span>
              )}
              {release.formatName && <span>• {release.formatName}</span>}
            </div>

            <div className="flex flex-wrap gap-1 mb-2">
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
            </div>

            {release.genreNames && release.genreNames.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {release.genreNames.slice(0, 3).map((genre: string, index: number) => (
                  <span
                    key={index}
                    className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800"
                  >
                    {genre}
                  </span>
                ))}
                {release.genreNames.length > 3 && (
                  <span className="text-xs text-gray-500">+{release.genreNames.length - 3}</span>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export function MusicReleaseList({ filters = {}, pageSize = 60 }: MusicReleaseListProps) {
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

      console.log('MusicReleaseList filters:', filters);

      const params = new URLSearchParams({
        'Pagination.PageNumber': page.toString(),
        'Pagination.PageSize': pageSize.toString(),
        ...(filters.search && { Search: filters.search }),
        ...(filters.artistId && { ArtistId: filters.artistId.toString() }),
        ...(filters.genreId && { GenreId: filters.genreId.toString() }),
        ...(filters.labelId && { LabelId: filters.labelId.toString() }),
        ...(filters.countryId && { CountryId: filters.countryId.toString() }),
        ...(filters.formatId && { FormatId: filters.formatId.toString() }),
        ...(filters.live !== undefined && { Live: filters.live.toString() }),
        ...(filters.yearFrom && { YearFrom: filters.yearFrom.toString() }),
        ...(filters.yearTo && { YearTo: filters.yearTo.toString() }),
        ...(filters.sortBy && { SortBy: filters.sortBy }),
        ...(filters.sortOrder && { SortOrder: filters.sortOrder })
      });

      console.log('API URL:', `/api/musicreleases?${params}`);

      const response: PagedResult<MusicRelease> = await fetchJson(`/api/musicreleases?${params}`);
      
      setReleases(response.items);
      setCurrentPage(response.page);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      console.error('Error fetching releases:', err);
      setError(err instanceof Error ? err.message : "Failed to load releases");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    console.log('MusicReleaseList filters changed, resetting to page 1:', filters);
    setCurrentPage(1);
    fetchReleases(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, pageSize]);

  const handlePageChange = (page: number) => {
    if (page < 1 || page > totalPages) {
      return;
    }
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

      {/* Release Cards - 3 per row grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
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
            {(() => {
              const maxPagesToShow = 5;
              let startPage = Math.max(1, currentPage - Math.floor(maxPagesToShow / 2));
              const endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);
              
              // Adjust startPage if we're near the end
              if (endPage - startPage < maxPagesToShow - 1) {
                startPage = Math.max(1, endPage - maxPagesToShow + 1);
              }
              
              const pages = [];
              for (let pageNum = startPage; pageNum <= endPage; pageNum++) {
                pages.push(pageNum);
              }
              
              return pages.map((pageNum) => {
                const isCurrentPage = pageNum === currentPage;
                
                return (
                  <button
                    key={`page-${pageNum}`}
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
              });
            })()}
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
