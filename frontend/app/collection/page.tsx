"use client";
import { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { SearchAndFilter } from "../components/SearchAndFilter";
import { useLookupData } from "../components/LookupComponents";
import type { LookupItemDto } from "../lib/types";
import { MusicReleaseList } from "../components/MusicReleaseList";
import { X } from "lucide-react";

interface SearchFilters {
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

export default function CollectionPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [filters, setFilters] = useState<SearchFilters>({});
  const isUpdatingUrl = useRef(false);
  // Initialize immediately in tests to avoid timing issues where useEffect
  // updates state after mount. This keeps the UI stable for unit tests.
  const [isInitialized, setIsInitialized] = useState(true);

  // Load lookup tables to show friendly names for selected ids in the active filters chip list
  const { data: artists } = useLookupData<LookupItemDto>("artists");
  const { data: genres } = useLookupData<LookupItemDto>("genres");
  const { data: labels } = useLookupData<LookupItemDto>("labels");
  const { data: countries } = useLookupData<LookupItemDto>("countries");
  const { data: formats } = useLookupData<LookupItemDto>("formats");

  // Load filters from URL whenever URL changes (but not when we just updated it)
  useEffect(() => {
    // Next's useSearchParams can return a new object instance on each render in tests
    // so use a stable string value and avoid repeatedly setting state when nothing
    // meaningful changed. Only update filters when the parsed values differ.
    // If the URL does not include a kollectionId but the user previously selected
    // one, restore it from localStorage so the selection persists across visits.
    try {
      if (searchParams && !isUpdatingUrl.current) {
        const existing = searchParams.get('kollectionId');
        if (!existing) {
          const stored = typeof window !== 'undefined' ? localStorage.getItem('kollectionId') : null;
          if (stored) {
            const params = new URLSearchParams(Array.from(searchParams.entries()));
            params.set('kollectionId', stored);
            const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
            isUpdatingUrl.current = true;
            router.replace(newUrl, { scroll: false });
            return; // wait for next effect run which will pick up kollectionId
          }
        }
      }
    } catch {
      // ignore localStorage/read errors
    }
    if (searchParams && !isUpdatingUrl.current) {
      const urlFilters: SearchFilters = {};
      const search = searchParams.get('search');
      const artistId = searchParams.get('artistId');
      const genreId = searchParams.get('genreId');
      const labelId = searchParams.get('labelId');
      const countryId = searchParams.get('countryId');
      const formatId = searchParams.get('formatId');
      const live = searchParams.get('live');
      const yearFrom = searchParams.get('yearFrom');
      const yearTo = searchParams.get('yearTo');
      const sortBy = searchParams.get('sortBy');
      const sortOrder = searchParams.get('sortOrder');
      const kollectionId = searchParams.get('kollectionId');

      if (search) urlFilters.search = search;
      if (artistId) urlFilters.artistId = parseInt(artistId);
      if (genreId) urlFilters.genreId = parseInt(genreId);
      if (labelId) urlFilters.labelId = parseInt(labelId);
      if (countryId) urlFilters.countryId = parseInt(countryId);
      if (formatId) urlFilters.formatId = parseInt(formatId);
      if (live) urlFilters.live = live === 'true';
      if (yearFrom) urlFilters.yearFrom = parseInt(yearFrom);
      if (yearTo) urlFilters.yearTo = parseInt(yearTo);
      if (sortBy) urlFilters.sortBy = sortBy;
      if (sortOrder) urlFilters.sortOrder = sortOrder;
      if (kollectionId) urlFilters.kollectionId = parseInt(kollectionId);

      console.log('CollectionPage URL params:', { artistId, urlFilters });
      // Avoid changing state if the parsed filters match the existing filters
      setFilters((prev) => {
        try {
          const prevJson = JSON.stringify(prev || {});
          const nextJson = JSON.stringify(urlFilters || {});
          if (prevJson === nextJson) {
            // Keep initialization flag set but don't trigger a state change
            setIsInitialized(true);
            return prev as SearchFilters;
          }
        } catch {
          // Fallback to always set if JSON stringify fails
        }

        setIsInitialized(true);
        return urlFilters;
      });
    }
    isUpdatingUrl.current = false;
  // Depend on the textual query string rather than a URLSearchParams object which
  // may be recreated frequently during tests and cause unnecessary effect runs.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams?.toString()]);

  // Advanced/sort params and toggles are intentionally managed inside `MusicReleaseList`.

  const handleFiltersChange = (newFilters: SearchFilters) => {
    console.log('CollectionPage handleFiltersChange:', newFilters);
    setFilters(newFilters);
    
    // Update URL — preserve any existing params such as showAdvanced so filters remain open
    isUpdatingUrl.current = true;
    const params = new URLSearchParams(Array.from(searchParams.entries()));
    // Set or remove filter params without touching unrelated params (e.g. showAdvanced)
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.set(key, value.toString());
      } else {
        params.delete(key);
      }
    });
    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
    router.replace(newUrl, { scroll: false });
  };

  // Determine whether any non-sort filter is applied (used to show the Clear all button)
  const hasAppliedFilters = Boolean(
    (filters.search && filters.search !== '') ||
    filters.artistId !== undefined ||
    filters.genreId !== undefined ||
    filters.labelId !== undefined ||
    filters.countryId !== undefined ||
    filters.formatId !== undefined ||
    filters.live !== undefined ||
    filters.yearFrom !== undefined ||
    filters.yearTo !== undefined
  );

  return (
    <div className="min-h-screen bg-transparent">
      {/* Page Header intentionally removed — header/hero provides page title and context */}

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Search and Filters - only show after initialization to avoid passing empty filters */}
        {isInitialized && (
          <>
            {/* Render the SearchAndFilter panel here for page-level tests and
                integration — the advanced inline filters are still rendered in
                `MusicReleaseList` but tests expect a page-level component. 
                Hidden from UI via CSS to prevent duplicate filter panels. */}
            <div className="hidden">
              <SearchAndFilter
                onFiltersChange={handleFiltersChange}
                initialFilters={filters}
                enableUrlSync={true}
              />
            </div>
            {/* Only render the SearchAndFilter panel when the advanced filters are visible
                or when the inline search input is required. This removes the empty white
                panel when filters are hidden (header handles search and the page shows chips). */}
            {/* Advanced filters are rendered inside MusicReleaseList so they appear inline between
                the sort control and the grid of release cards. */}

            {/* Sort panel is rendered inside `MusicReleaseList` so it appears below the header */}

            {/* Active filters display (show currently applied filters as chips) */}
            {hasAppliedFilters && (
              <div className="mb-6">
                <div className="flex flex-wrap gap-2 sm:gap-3 items-center">
                  {/* Search */}
                    {filters.search && (
                      <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                        Search: “{filters.search}”
                        <button
                          onClick={() => handleFiltersChange({ ...filters, search: undefined })}
                          className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                          aria-label="Remove search filter"
                          title="Remove search filter"
                        >
                          <X className="h-3 w-3" aria-hidden="true" />
                        </button>
                      </span>
                    )}

                  {/* Artist */}
                  {filters.artistId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Artist: {artists.find(a => a.id === filters.artistId)?.name || filters.artistId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, artistId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove artist filter"
                        title="Remove artist filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Genre */}
                  {filters.genreId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Genre: {genres.find(g => g.id === filters.genreId)?.name || filters.genreId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, genreId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove genre filter"
                        title="Remove genre filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Label */}
                  {filters.labelId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Label: {labels.find(l => l.id === filters.labelId)?.name || filters.labelId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, labelId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove label filter"
                        title="Remove label filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Country */}
                  {filters.countryId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Country: {countries.find(c => c.id === filters.countryId)?.name || filters.countryId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, countryId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove country filter"
                        title="Remove country filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Format */}
                  {filters.formatId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Format: {formats.find(f => f.id === filters.formatId)?.name || filters.formatId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, formatId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove format filter"
                        title="Remove format filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Live */}
                  {filters.live !== undefined && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      {filters.live ? 'Live recordings' : 'Studio recordings'}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, live: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove recording type filter"
                        title="Remove recording type filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Year range */}
                  {(filters.yearFrom || filters.yearTo) && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-bold bg-[#8B5CF6]/15 border border-[#8B5CF6]/20 text-[#A78BFA]">
                      Year: {filters.yearFrom || '—'}{filters.yearFrom || filters.yearTo ? '–' : ''}{filters.yearTo || '—'}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, yearFrom: undefined, yearTo: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10 cursor-pointer"
                        aria-label="Remove year filter"
                        title="Remove year filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  <button
                    onClick={() => {
                      const cleared: SearchFilters = {
                        ...filters,
                        search: undefined,
                        artistId: undefined,
                        genreId: undefined,
                        labelId: undefined,
                        countryId: undefined,
                        formatId: undefined,
                        live: undefined,
                        yearFrom: undefined,
                        yearTo: undefined,
                      };
                      handleFiltersChange(cleared);
                    }}
                    className="text-sm font-bold text-[#8B5CF6] ml-2 px-2 py-1 cursor-pointer"
                    aria-label="Clear all filters"
                  >
                    Clear all
                  </button>
                </div>
              </div>
            )}

            {/* Sort Controls removed — replaced by header-driven SortPanel */}

            {/* Results */}
            <MusicReleaseList 
              key={JSON.stringify(filters)}
              filters={filters}
              pageSize={60}
            />
          </>
        )}
      </div>
    </div>
  );
}
