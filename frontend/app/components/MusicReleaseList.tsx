"use client";
import React, { useState, useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import { fetchJson, createNowPlaying, ApiError } from "../lib/api";
import { clearAuthToken } from "../lib/auth";
 
import { VinylSpinner } from "./VinylSpinner";
import { Play, Check, User, Clock, Calendar, Disc3, Eye, List } from "lucide-react";

import { AddToListDialog } from "./AddToListDialog";
import { SearchAndFilter } from "./SearchAndFilter";
import { FormatIcon } from "./FormatIcon";

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
  activeFiltersRender?: React.ReactNode;
}

export const MusicReleaseCard = React.memo(function MusicReleaseCard({ release }: { release: MusicRelease }) {
  const getCoverImageUrl = () => {
    if (release.coverImageUrl) {
      // Check if it's already a full URL
      if (release.coverImageUrl.startsWith('http://') || release.coverImageUrl.startsWith('https://')) {
        return release.coverImageUrl;
      }
      // If it starts with /cover-art/, it's already a full path (multi-tenant storage)
      if (release.coverImageUrl.startsWith('/cover-art/')) {
        const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
        return `${apiBaseUrl}${release.coverImageUrl}`;
      }
      // Otherwise, serve images through the backend API with /api/images/
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
    <div className="group cursor-pointer">
      <div className="aspect-square bg-[#13131F] rounded-xl border border-[#1C1C28] group-hover:border-[#8B5CF6]/50 transition-all mb-2 flex items-center justify-center text-4xl overflow-hidden relative">
        {!imageError ? (
          <Image
            src={getCoverImageUrl()}
            alt={`${release.title} cover`}
            fill
            className="object-cover"
            onError={() => setImageError(true)}
            onLoad={() => setImageError(false)}
            sizes="(max-width: 768px) 50vw, (max-width: 1200px) 33vw, 20vw"
            loading="lazy"
            quality={75}
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-600 bg-[#13131F]">
            <Disc3 className="w-12 h-12" />
          </div>
        )}

        <FormatIcon formatName={release.formatName} />

        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/50 transition-all duration-300 flex items-center justify-center opacity-0 group-hover:opacity-100 p-2">
          <div className="flex gap-2 flex-wrap justify-center">
            <Link 
              href={`/releases/${release.id}`}
              className="bg-[#8B5CF6] text-white rounded-full w-9 h-9 flex items-center justify-center hover:scale-110 transition-transform shadow-lg"
            >
              <Eye className="w-4 h-4" />
            </Link>
            
            <button
              onClick={handleNowPlaying}
              disabled={isLoading}
              className={`rounded-full w-9 h-9 flex items-center justify-center hover:scale-110 transition-transform shadow-lg ${
                isPlaying 
                  ? 'bg-emerald-500 text-white' 
                  : 'bg-white/90 text-[#8B5CF6]'
              }`}
              title={isPlaying ? 'Playing now' : 'Mark as now playing'}
            >
              {isPlaying ? (
                <Check className="w-4 h-4" />
              ) : (
                <Play className="w-4 h-4" />
              )}
            </button>

            <button
              onClick={handleAddToList}
              className="bg-white/90 text-[#8B5CF6] rounded-full w-9 h-9 flex items-center justify-center hover:scale-110 transition-transform shadow-lg"
              title="Add to list"
            >
              <List className="w-4 h-4" />
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
      
      <div className="text-xs font-semibold text-white truncate" title={release.title}>
        <Link href={`/releases/${release.id}`} className="hover:text-[#A78BFA] transition-colors">
          {release.title}
        </Link>
      </div>
      <div className="text-xs text-[#A78BFA] truncate font-medium" title={release.artistNames?.join(", ")}>
        {release.artistNames?.join(", ") || "Unknown Artist"}
      </div>
      <div className="text-xs text-[#A78BFA]/70 truncate">
        {releaseYear}{origReleaseYear && origReleaseYear !== releaseYear ? ` (${origReleaseYear})` : ''} {release.labelName ? `· ${release.labelName}` : ''}
      </div>
    </div>
  );
});

export const MusicReleaseList = React.memo(function MusicReleaseList({ filters = {}, pageSize = 60, onSortChange, activeFiltersRender }: MusicReleaseListProps & { onSortChange?: (f: MusicReleaseFilters) => void }) {
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
    } catch {
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
    } catch {
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

      // Retry once on transient failures (network, timeouts)
      let attempts = 0;
      const maxAttempts = 2;
      let lastErr: unknown = null;
      let response: PagedResult<MusicRelease> | MusicRelease[] | null = null;

      while (attempts < maxAttempts) {
        attempts += 1;
        try {
          response = await fetchJson<PagedResult<MusicRelease> | MusicRelease[]>(`/api/musicreleases?${params}`);
          lastErr = null;
          break;
        } catch (err) {
          lastErr = err;
          console.warn(`fetchReleases attempt ${attempts} failed`, err);
          if (attempts < maxAttempts) await new Promise(r => setTimeout(r, 600 * attempts));
        }
      }

      if (!response && lastErr) throw lastErr;

      if (Array.isArray(response)) {
        setReleases(response);
        setCurrentPage(1);
        setTotalPages(1);
        setTotalCount(response.length);
      } else if (response && typeof response === 'object') {
        const resp = response as PagedResult<MusicRelease>;
        setReleases(resp.items || []);
        setCurrentPage(resp.page || 1);
        setTotalPages(resp.totalPages || 0);
        setTotalCount(resp.totalCount || 0);
      } else {
        setReleases([]);
        setCurrentPage(1);
        setTotalPages(0);
        setTotalCount(0);
      }
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
        const anyErr = err as unknown;
        if (anyErr && typeof anyErr === 'object') {
          const errObj = anyErr as Record<string, unknown>;
          if (typeof errObj.message === 'string') message = errObj.message;
          if (typeof errObj.status === 'number') message += ` (status: ${errObj.status})`;
          if (errObj.details) {
            try {
              const d = typeof errObj.details === 'string' ? errObj.details : JSON.stringify(errObj.details);
              message += ` - ${d}`;
            } catch { /* ignore stringify errors */ }
          }
          if (typeof errObj.url === 'string') message += ` [url: ${errObj.url}]`;
        } else if (err instanceof Error) {
          message = err.message;
        }
      } catch {
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
      <div className="min-h-screen flex items-start justify-center pt-8">
        <VinylSpinner 
          size="large" 
          message="Loading your collection..." 
        />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-[#13131F] rounded-2xl border border-[#1C1C28] p-8 text-center">
        <div className="text-[#8B5CF6] mb-4">
          <svg className="w-12 h-12 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.502 0L4.312 15.5c-.77.833.192 2.5 1.732 2.5z" />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-white mb-2">Error Loading Releases</h3>
        <p className="text-gray-400 mb-4">{error}</p>
        <button
          onClick={() => fetchReleases(currentPage)}
          className="px-4 py-2 bg-[#8B5CF6] hover:bg-[#7C3AED] text-white rounded-xl transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (releases.length === 0) {
    return (
      <div className="bg-[#13131F] rounded-2xl border border-[#1C1C28] p-8 text-center">
        <div className="text-gray-600 mb-4">
          <svg className="w-12 h-12 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6-4h6m2 5.291A7.962 7.962 0 0112 15c-2.34 0-4.441.935-5.982 2.457M16.5 4.5L19 7l-2.5 2.5M4.5 4.5L7 7 4.5 9.5" />
          </svg>
        </div>
        <h3 className="text-lg font-medium text-white mb-2">No Releases Found</h3>
        <p className="text-gray-500">No music releases match your current filters.</p>
      </div>
    );
  }

  return (
    <div>
      {/* Results Header */}
      {!loading && (
        <div className="space-y-6 mb-6">
          <div className="flex gap-3 flex-wrap items-center">
            <div className="flex-1 min-w-[200px] relative">
              <input 
                type="text" 
                placeholder="Search releases, artists, albums..." 
                className="w-full bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:border-[#8B5CF6]"
                value={searchParams?.get('search') || ''}
                onChange={(e) => {
                  try {
                    const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                    if (e.target.value) params.set('search', e.target.value);
                    else params.delete('search');
                    const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                    router.replace(newUrl, { scroll: false });
                  } catch {}
                }}
              />
            </div>
            
            <select 
              className="bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-gray-300 text-sm focus:outline-none focus:border-[#8B5CF6] cursor-pointer"
              value={`${filters.sortBy || 'DateAdded'}_${filters.sortOrder || 'desc'}`}
              onChange={(e) => {
                const [by, order] = e.target.value.split('_');
                applySortChange({ sortBy: by, sortOrder: order });
              }}
            >
              <option value="DateAdded_desc">Sort: Date Added</option>
              <option value="Title_asc">Title (A-Z)</option>
              <option value="Title_desc">Title (Z-A)</option>
              <option value="Artist_asc">Artist (A-Z)</option>
              <option value="Artist_desc">Artist (Z-A)</option>
              <option value="Year_desc">Year (Newest)</option>
              <option value="Year_asc">Year (Oldest)</option>
            </select>

            <button
              onClick={() => {
                try {
                  const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                  const currentlyOpen = params.get('showAdvanced') === 'true';
                  if (currentlyOpen) params.delete('showAdvanced');
                  else params.set('showAdvanced', 'true');
                  const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                  router.replace(newUrl, { scroll: false });
                } catch {}
              }}
              className={`px-5 rounded-xl text-sm font-medium flex items-center gap-2 transition-all h-[46px] ${
                searchParams?.get('showAdvanced') === 'true'
                  ? "bg-[#8B5CF6] text-white shadow-lg shadow-[#8B5CF6]/25"
                  : "bg-[#13131F] border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#2E2E3E]"
              }`}
            >
              <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" /></svg>
              Filters
              <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor" className={`transition-transform duration-200 ${searchParams?.get('showAdvanced') === 'true' ? "rotate-180" : ""}`}><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round" /></svg>
            </button>
          </div>
          
          <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap mt-2 mb-4">
            <span>{totalCount} release{totalCount !== 1 ? 's' : ''}</span>
            {activeFiltersRender}
          </div>

          {searchParams?.get('showAdvanced') === 'true' && (
            <div className="w-full mt-0">
              <SearchAndFilter
                onFiltersChange={(newFilters) => {
                  try {
                    const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                    Object.entries(newFilters as Record<string, unknown>).forEach(([k, v]) => {
                      if (v !== undefined && v !== null && v !== '') params.set(k, String(v));
                      else params.delete(k);
                    });
                    const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                    router.replace(newUrl, { scroll: false });
                  } catch {}
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
                  } catch {}
                }}
              />
            </div>
          )}
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
            className="px-4 py-2 rounded-xl bg-[#13131F] border border-[#1C1C28] text-gray-400 text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:border-[#8B5CF6]/40 transition-colors"
          >
            ← Prev
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
                    className={`w-9 h-9 rounded-xl text-sm font-medium disabled:cursor-not-allowed ${
                      isCurrentPage
                        ? "bg-[#8B5CF6] text-white"
                        : "bg-[#13131F] border border-[#1C1C28] text-gray-400 hover:border-[#8B5CF6]/40"
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
            className="px-4 py-2 rounded-xl bg-[#13131F] border border-[#1C1C28] text-gray-400 text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:border-[#8B5CF6]/40 transition-colors"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
});
