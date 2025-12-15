"use client";
import React, { useState, useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import { fetchJson, createNowPlaying, ApiError } from "../lib/api";
import { clearAuthToken } from "../lib/auth";
import { LoadingSpinner, Skeleton } from "./LoadingComponents";
import { Play, Check, User, Clock, Calendar, Disc3, ChevronLeft, ChevronRight, Eye, List } from "lucide-react";

import SortPanel from "./SortPanel";
import { AddToListDialog } from "./AddToListDialog";
import { SearchAndFilter } from "./SearchAndFilter";

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
  kollectionId?: number;
}

interface MusicReleaseListProps {
  filters?: MusicReleaseFilters;
  pageSize?: number;
}

export const MusicReleaseCard = React.memo(function MusicReleaseCard({ release }: { release: MusicRelease }) {
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
  const [isPlaying, setIsPlaying] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddToList, setShowAddToList] = useState(false);

  const handleNowPlaying = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (isLoading) return;
    
    setIsLoading(true);
    try {
      await createNowPlaying(release.id);
      setIsPlaying(true);
    } catch (error) {
      console.error('Failed to record now playing:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddToList = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setShowAddToList(true);
  };

  const releaseYear = new Date(release.releaseYear).getFullYear();
  const origReleaseYear = release.origReleaseYear ? new Date(release.origReleaseYear).getFullYear() : null;

  return (
    <div className="group bg-white rounded-xl overflow-hidden shadow-lg hover:shadow-xl transition-all duration-300 hover:-translate-y-2">
      <div className="relative aspect-square bg-gray-100">
        {!imageError ? (
          <Image
            src={getCoverImageUrl()}
            alt={`${release.title} cover`}
            fill
            className="object-cover"
            onError={() => setImageError(true)}
            onLoad={() => setImageError(false)}
            sizes="(max-width: 768px) 50vw, (max-width: 1200px) 33vw, 20vw"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400">
            <Disc3 className="w-12 h-12" />
          </div>
        )}

        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-all duration-300 flex items-center justify-center opacity-0 group-hover:opacity-100">
          <div className="flex gap-2">
            <Link 
              href={`/releases/${release.id}`}
              className="bg-white text-[#D93611] rounded-full w-10 h-10 flex items-center justify-center hover:scale-110 transition-transform shadow-lg"
            >
              <Eye className="w-5 h-5" />
            </Link>
            
            <button
              onClick={handleNowPlaying}
              disabled={isLoading}
              className={`rounded-full w-10 h-10 flex items-center justify-center hover:scale-110 transition-transform shadow-lg ${
                isPlaying 
                  ? 'bg-green-500 text-white' 
                  : 'bg-white text-[#D93611]'
              }`}
              title={isPlaying ? 'Playing now' : 'Mark as now playing'}
            >
              {isPlaying ? (
                <Check className="w-5 h-5" />
              ) : (
                <Play className="w-5 h-5" />
              )}
            </button>

            <button
              onClick={handleAddToList}
              className="bg-white text-[#D93611] rounded-full w-10 h-10 flex items-center justify-center hover:scale-110 transition-transform shadow-lg"
              title="Add to list"
            >
              <List className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
      
      <AddToListDialog
        releaseId={release.id}
        releaseTitle={release.title}
        isOpen={showAddToList}
        onClose={() => setShowAddToList(false)}
      />
      
      <div className="p-3">
        <h3 className="font-bold text-sm text-gray-900 truncate mb-0.5" title={release.title}>
          <Link 
            href={`/releases/${release.id}`}
            className="hover:text-[#D93611] transition-colors"
          >
            {release.title}
          </Link>
        </h3>
        
        <p className="text-xs text-gray-600 truncate mb-1" title={release.artistNames?.join(", ")}>
          {release.artistNames?.join(", ")}
        </p>

        <div className="flex items-center gap-1 text-[10px] text-gray-500 truncate mb-2">
          {release.labelName && <span title={release.labelName}>{release.labelName}</span>}
          {release.labelName && release.countryName && <span>•</span>}
          {release.countryName && <span title={release.countryName}>{release.countryName}</span>}
        </div>

        <div className="flex items-center justify-between text-xs text-gray-500">
          <div className="flex flex-col leading-tight">
            <span>{releaseYear}</span>
            {origReleaseYear && origReleaseYear !== releaseYear && (
              <span className="text-[10px] text-gray-400">Orig: {origReleaseYear}</span>
            )}
          </div>
          {release.formatName && (
            <span className="px-2 py-0.5 rounded-full font-bold text-white text-[10px] bg-[#D9601A]">
              {release.formatName}
            </span>
          )}
        </div>
      </div>
    </div>
  );
});

export const MusicReleaseList = React.memo(function MusicReleaseList({ filters = {}, pageSize = 60, onSortChange }: MusicReleaseListProps & { onSortChange?: (f: MusicReleaseFilters) => void }) {
  const [releases, setReleases] = useState<MusicRelease[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [iconAnimating, setIconAnimating] = useState(false);
  const [showSortOpen, setShowSortOpen] = useState<boolean>(false);

  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();

  // Apply default sort when none provided: Title A→Z
  const effectiveFilters: MusicReleaseFilters = {
    ...filters,
    sortBy: filters.sortBy ?? 'title',
    sortOrder: filters.sortOrder ?? 'asc',
  };

  // Ensure the URL contains the default sort when the page first loads so
  // the header control and SortPanel reflect the correct selected state.
  // This only writes defaults if neither sortBy nor sortOrder are present.
  useEffect(() => {
    try {
      const sp = searchParams;
      if (!sp) return;
      const hasSortBy = sp.get('sortBy');
      const hasSortOrder = sp.get('sortOrder');
      if (hasSortBy || hasSortOrder) return;

      const params = new URLSearchParams(sp.toString());
      params.set('sortBy', effectiveFilters.sortBy!);
      params.set('sortOrder', effectiveFilters.sortOrder!);

      const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
      router.replace(newUrl, { scroll: false });
    } catch (e) {
      // ignore
    }
    // run when search params / path / router or default values change
  }, [searchParams, pathname, router, effectiveFilters.sortBy, effectiveFilters.sortOrder]);

  // Keep a small local state for the open state so the middle button updates visually
  // immediately when clicked (router.replace updates searchParams asynchronously).
  useEffect(() => {
    setShowSortOpen(searchParams?.get('showSort') === 'true');
  }, [searchParams]);

  // order matches the SortPanel button order (left-to-right)
  const sortOptions: { sortBy: string; sortOrder: string }[] = [
    { sortBy: 'title', sortOrder: 'asc' },
    { sortBy: 'title', sortOrder: 'desc' },
    { sortBy: 'artist', sortOrder: 'asc' },
    { sortBy: 'artist', sortOrder: 'desc' },
    { sortBy: 'dateadded', sortOrder: 'desc' },
    { sortBy: 'dateadded', sortOrder: 'asc' },
    { sortBy: 'origreleaseyear', sortOrder: 'desc' },
    { sortBy: 'origreleaseyear', sortOrder: 'asc' },
  ];

  const getSortLabel = (sortBy?: string, sortOrder?: string) => {
    const order = sortOrder === 'asc' ? 'asc' : 'desc';
    switch (sortBy) {
      case 'title':
        return order === 'asc' ? 'Title (A-Z)' : 'Title (Z-A)';
      case 'artist':
        return order === 'asc' ? 'Artist (A-Z)' : 'Artist (Z-A)';
      case 'dateadded':
        return order === 'desc' ? 'Recently Added (Newest first)' : 'Oldest First';
      case 'origreleaseyear':
        return order === 'desc' ? 'Original Release Year (Newest First)' : 'Original Release Year (Oldest First)';
      default:
        return 'Sort';
    }
  };

  const renderSortIcon = () => {
    const order = effectiveFilters.sortOrder === 'asc' ? 'asc' : 'desc';
    switch (effectiveFilters.sortBy) {
      case 'title':
        return (
          <div className="flex items-center gap-1">
            <Disc3 className="w-5 h-5 text-white" />
            {order === 'asc' ? (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            ) : (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            )}
          </div>
        );
      case 'artist':
        return (
          <div className="flex items-center gap-1">
            <User className="w-5 h-5 text-white" />
            {order === 'asc' ? (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            ) : (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            )}
          </div>
        );
      case 'dateadded':
        return (
          <div className="flex items-center gap-1">
            <Clock className="w-5 h-5 text-white" />
            {order === 'asc' ? (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            ) : (
              <svg className="w-4 h-4 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            )}
          </div>
        );
      case 'origreleaseyear':
        return (
          <div className="flex items-center gap-1">
            <Calendar className="w-5 h-5 text-white" />
            {order === 'asc' ? (
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            ) : (
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            )}
          </div>
        );
      default:
        return null;
    }
  };

  const applySortChange = (newSort: { sortBy?: string; sortOrder?: string }) => {
    // If a parent handler exists, defer to it
    if (typeof onSortChange === 'function') {
      onSortChange({ ...(filters || {}), ...(newSort || {}) });
      return;
    }

    try {
      const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
      if (newSort.sortBy) params.set('sortBy', newSort.sortBy);
      if (newSort.sortOrder) params.set('sortOrder', newSort.sortOrder);
      const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
      router.replace(newUrl, { scroll: false });
    } catch (e) {
      // ignore
    }
  };

  // cycleSort removed: prev/next sort controls no longer present; sort panel toggles visibility instead

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
        ...(filters.kollectionId && { KollectionId: filters.kollectionId.toString() }),
          ...(effectiveFilters.sortBy && { SortBy: effectiveFilters.sortBy }),
          ...(effectiveFilters.sortOrder && { SortOrder: effectiveFilters.sortOrder })
      });

      console.log('API URL:', `/api/musicreleases?${params}`);

      const response: PagedResult<MusicRelease> = await fetchJson(`/api/musicreleases?${params}`);
      
      setReleases(response.items);
      setCurrentPage(response.page);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      console.error('Error fetching releases:', err);
      
      // Handle 401 Unauthorized
      const apiError = err as ApiError;
      if (apiError?.status === 401) {
        clearAuthToken();
        window.location.href = '/';
        return;
      }

      // Try to surface server status/details if available (ApiError shape from fetchJson)
      let message = 'Failed to load releases';
      try {
        const anyErr = err as any;
        if (anyErr && typeof anyErr === 'object') {
          if (anyErr.message) message = anyErr.message;
          if (anyErr.status) message += ` (status: ${anyErr.status})`;
          if (anyErr.details) {
            try {
              const d = typeof anyErr.details === 'string' ? anyErr.details : JSON.stringify(anyErr.details);
              message += ` - ${d}`;
            } catch { /* ignore stringify errors */ }
          }
          if (anyErr.url) message += ` [url: ${anyErr.url}]`;
        } else if (err instanceof Error) {
          message = err.message;
        }
      } catch (e) {
        // fallback
        message = (err instanceof Error) ? err.message : 'Failed to load releases';
      }

      setError(message);
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

  // trigger a tiny animation when the selected sort changes
  useEffect(() => {
    setIconAnimating(true);
    const t = setTimeout(() => setIconAnimating(false), 220);
    return () => clearTimeout(t);
  }, [filters.sortBy, filters.sortOrder]);

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
      <div className={`flex items-center justify-between ${(searchParams?.get('showSort') === 'true' || searchParams?.get('showAdvanced') === 'true') ? 'mb-1' : 'mb-6'}`}>
        <div className="text-sm text-white">
          Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} releases
        </div>
        <div className="flex items-center gap-3">
          {/* Filters toggle placed to the left of the sort control */}
          <div className="inline-flex items-center divide-x divide-white/10 rounded-md border border-white/10 bg-gradient-to-br from-red-900 via-red-950 to-black shadow-sm overflow-hidden">
            <button
              type="button"
              onClick={() => {
                try {
                  const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                  const currentlyOpen = params.get('showAdvanced') === 'true';
                  if (currentlyOpen) {
                    params.delete('showAdvanced');
                  } else {
                    params.set('showAdvanced', 'true');
                    params.delete('showSort');
                  }
                  const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                  router.replace(newUrl, { scroll: false });
                } catch (e) {
                  // ignore
                }
              }}
              aria-label="Filters"
              title="Filters"
              aria-expanded={searchParams?.get('showAdvanced') === 'true'}
              className={`px-2 py-2 flex items-center gap-2 text-sm transition-transform duration-200 w-20 h-9 justify-center text-white focus:outline-none`}
            >
              <div className={`${iconAnimating ? 'scale-105 opacity-90' : 'scale-100 opacity-100'} inline-flex items-center justify-center rounded-md w-16 h-7 ${searchParams?.get('showAdvanced') === 'true' ? 'bg-[#F28A2E]/50 hover:bg-[#F28A2E]/40 text-white' : 'bg-white/10 hover:bg-white/20 text-white'}`}>
                <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                  <path d="M3 5h18M6 12h12M10 19h4" />
                </svg>
              </div>
            </button>
            <button
              type="button"
              onClick={() => {
                try {
                  const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                  const currently = params.get('showSort') === 'true';
                  if (currently) {
                    params.delete('showSort');
                    setShowSortOpen(false);
                  } else {
                    params.set('showSort', 'true');
                    // ensure advanced filters are closed when opening sort
                    params.delete('showAdvanced');
                    setShowSortOpen(true);
                  }
                  const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                  router.replace(newUrl, { scroll: false });
                } catch (e) {
                  // ignore
                }
              }}
              title={getSortLabel(effectiveFilters.sortBy, effectiveFilters.sortOrder)}
              aria-label={`Current sort: ${getSortLabel(effectiveFilters.sortBy, effectiveFilters.sortOrder)}`}
              aria-expanded={showSortOpen}
              className={`px-2 py-2 flex items-center gap-2 text-sm transition-transform duration-200 w-20 h-9 justify-center focus:outline-none`}
            >
              <div className={`${iconAnimating ? 'scale-105 opacity-90' : 'scale-100 opacity-100'} inline-flex items-center justify-center rounded-md w-16 h-7 ${showSortOpen ? 'bg-[#F28A2E]/50 hover:bg-[#F28A2E]/40 text-white' : 'bg-white/10 hover:bg-white/20 text-white'}`}> 
                {loading ? (
                  <div className="flex items-center gap-1">
                    <div className="w-12 h-6 flex items-center justify-center">
                      <LoadingSpinner size="small" color="white" />
                    </div>
                  </div>
                ) : (
                  (filters?.sortBy) ? (
                    renderSortIcon()
                  ) : (
                    <div className="text-sm">Sort</div>
                  )
                )}
              </div>
            </button>

            {/* Next/Previous sort controls removed — sort toggle now opens the SortPanel */}
          </div>

          {/* loading spinner is shown in the middle segment to avoid layout shift */}
        </div>
      </div>

      {searchParams?.get('showSort') === 'true' && (
        <div className="w-full mt-0 mb-6">
          <SortPanel
            filters={filters}
            onChange={(newSort) => applySortChange(newSort)}
            open={true}
            onClose={() => {
              try {
                const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                params.delete('showSort');
                const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                router.replace(newUrl, { scroll: false });
              } catch (e) {
                // ignore
              }
            }}
          />
        </div>
      )}

      {searchParams?.get('showAdvanced') === 'true' && (
        <div className="w-full mt-0 mb-6">
          <SearchAndFilter
            onFiltersChange={(newFilters) => {
              try {
                const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                Object.entries(newFilters as any).forEach(([k, v]) => {
                  if (v !== undefined && v !== null && v !== '') params.set(k, (v as any).toString());
                  else params.delete(k);
                });
                const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                router.replace(newUrl, { scroll: false });
              } catch (e) {
                // ignore
              }
            }}
            initialFilters={filters}
            enableUrlSync={false}
            showSearchInput={false}
            openAdvanced={true}
            compact={true}
            onAdvancedToggle={(open) => {
              try {
                const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                if (open) params.set('showAdvanced', 'true'); else params.delete('showAdvanced');
                const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                router.replace(newUrl, { scroll: false });
              } catch (e) {
                // ignore
              }
            }}
          />
        </div>
      )}

      {/* Release Cards - Grid matching mock-up */}
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6 mb-8">
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
});
