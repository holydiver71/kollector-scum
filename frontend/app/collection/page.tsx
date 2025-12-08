"use client";
import { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { SearchAndFilter } from "../components/SearchAndFilter";
import { useLookupData } from "../components/LookupComponents";
import { MusicReleaseList } from "../components/MusicReleaseList";
import { ArrowDownAZ, ArrowUpAZ, User, Clock, Disc3, Calendar, X } from "lucide-react";

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
}

export default function CollectionPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [filters, setFilters] = useState<SearchFilters>({});
  const isUpdatingUrl = useRef(false);
  const [isInitialized, setIsInitialized] = useState(false);

  // Load lookup tables to show friendly names for selected ids in the active filters chip list
  const { data: artists } = useLookupData<any>("artists");
  const { data: genres } = useLookupData<any>("genres");
  const { data: labels } = useLookupData<any>("labels");
  const { data: countries } = useLookupData<any>("countries");
  const { data: formats } = useLookupData<any>("formats");

  // Load filters from URL whenever URL changes (but not when we just updated it)
  useEffect(() => {
    // Next's useSearchParams can return a new object instance on each render in tests
    // so use a stable string value and avoid repeatedly setting state when nothing
    // meaningful changed. Only update filters when the parsed values differ.
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
        } catch (e) {
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

  const showAdvancedFromParams = searchParams.get('showAdvanced') === 'true';

  const handleAdvancedToggle = (open: boolean) => {
    const params = new URLSearchParams(Array.from(searchParams.entries()));
    if (open) params.set('showAdvanced', 'true');
    else params.delete('showAdvanced');
    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
    router.replace(newUrl, { scroll: false });
  };

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

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header intentionally removed — header/hero provides page title and context */}

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Search and Filters - only show after initialization to avoid passing empty filters */}
        {isInitialized && (
          <>
            

            {/* Only render the SearchAndFilter panel when the advanced filters are visible
                or when the inline search input is required. This removes the empty white
                panel when filters are hidden (header handles search and the page shows chips). */}
            {showAdvancedFromParams && (
              <SearchAndFilter
                onFiltersChange={handleFiltersChange}
                initialFilters={filters}
                enableUrlSync={false}
                showSearchInput={false}
                openAdvanced={showAdvancedFromParams}
                onAdvancedToggle={handleAdvancedToggle}
              />
            )}

            {/* Active filters display (show currently applied filters as chips) */}
            {Object.keys(filters).length > 0 && (
              <div className="mb-6">
                <div className="flex flex-wrap gap-2 sm:gap-3 items-center">
                  {/* Search */}
                    {filters.search && (
                      <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                        Search: “{filters.search}”
                        <button
                          onClick={() => handleFiltersChange({ ...filters, search: undefined })}
                          className="ml-1 p-0.5 rounded hover:bg-white/10"
                          aria-label="Remove search filter"
                          title="Remove search filter"
                        >
                          <X className="h-3 w-3" aria-hidden="true" />
                        </button>
                      </span>
                    )}

                  {/* Artist */}
                  {filters.artistId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      Artist: {artists.find(a => a.id === filters.artistId)?.name || filters.artistId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, artistId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove artist filter"
                        title="Remove artist filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Genre */}
                  {filters.genreId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      Genre: {genres.find(g => g.id === filters.genreId)?.name || filters.genreId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, genreId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove genre filter"
                        title="Remove genre filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Label */}
                  {filters.labelId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      Label: {labels.find(l => l.id === filters.labelId)?.name || filters.labelId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, labelId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove label filter"
                        title="Remove label filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Country */}
                  {filters.countryId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      Country: {countries.find(c => c.id === filters.countryId)?.name || filters.countryId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, countryId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove country filter"
                        title="Remove country filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Format */}
                  {filters.formatId && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      Format: {formats.find(f => f.id === filters.formatId)?.name || filters.formatId}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, formatId: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove format filter"
                        title="Remove format filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Live */}
                  {filters.live !== undefined && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm sm:text-sm font-semibold text-white bg-[#D9601A]">
                      {filters.live ? 'Live recordings' : 'Studio recordings'}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, live: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove recording type filter"
                        title="Remove recording type filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  {/* Year range */}
                  {(filters.yearFrom || filters.yearTo) && (
                    <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-bold text-white bg-[#D9601A]">
                      Year: {filters.yearFrom || '—'}{filters.yearFrom || filters.yearTo ? '–' : ''}{filters.yearTo || '—'}
                      <button
                        onClick={() => handleFiltersChange({ ...filters, yearFrom: undefined, yearTo: undefined })}
                        className="ml-1 p-0.5 rounded hover:bg-white/10"
                        aria-label="Remove year filter"
                        title="Remove year filter"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </span>
                  )}

                  <button
                    onClick={() => handleFiltersChange({})}
                    className="text-sm font-bold text-[#D93611] ml-2 px-2 py-1"
                    aria-label="Clear all filters"
                  >
                    Clear all
                  </button>
                </div>
              </div>
            )}

            {/* Sort Controls */}
            <div className="mb-6 flex justify-end">
              <div className="flex items-center gap-2">
                <span className="text-sm font-bold text-gray-700 mr-2">Sort by:</span>
                <div className="inline-flex rounded-lg border border-gray-300 bg-white shadow-sm">
                  {/* Title A-Z */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'title', sortOrder: 'asc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors rounded-l-lg border-r border-gray-300
                      ${filters.sortBy === 'title' && filters.sortOrder === 'asc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Title (A-Z)"
                  >
                    <Disc3 className="w-4 h-4" />
                    <ArrowDownAZ className="w-3 h-3" />
                  </button>
                  
                  {/* Title Z-A */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'title', sortOrder: 'desc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'title' && filters.sortOrder === 'desc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Title (Z-A)"
                  >
                    <Disc3 className="w-4 h-4" />
                    <ArrowUpAZ className="w-3 h-3" />
                  </button>
                  
                  {/* Artist A-Z */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'artist', sortOrder: 'asc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'artist' && filters.sortOrder === 'asc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Artist (A-Z)"
                  >
                    <User className="w-4 h-4" />
                    <ArrowDownAZ className="w-3 h-3" />
                  </button>
                  
                  {/* Artist Z-A */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'artist', sortOrder: 'desc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'artist' && filters.sortOrder === 'desc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Artist (Z-A)"
                  >
                    <User className="w-4 h-4" />
                    <ArrowUpAZ className="w-3 h-3" />
                  </button>
                  
                  {/* Recently Added */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'dateadded', sortOrder: 'desc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'dateadded' && filters.sortOrder === 'desc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Recently Added"
                  >
                    <Clock className="w-4 h-4" />
                    <span className="hidden sm:inline">New</span>
                  </button>
                  
                  {/* Oldest First */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'dateadded', sortOrder: 'asc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'dateadded' && filters.sortOrder === 'asc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Oldest First"
                  >
                    <Clock className="w-4 h-4" />
                    <span className="hidden sm:inline">Old</span>
                  </button>
                  
                  {/* Original Release Year (Newest First) */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'origreleaseyear', sortOrder: 'desc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors border-r border-gray-300
                      ${filters.sortBy === 'origreleaseyear' && filters.sortOrder === 'desc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Original Release Year (Newest First)"
                  >
                    <Calendar className="w-4 h-4" />
                    <span className="hidden sm:inline">Year ↓</span>
                  </button>
                  
                  {/* Original Release Year (Oldest First) */}
                  <button
                    onClick={() => handleFiltersChange({ ...filters, sortBy: 'origreleaseyear', sortOrder: 'asc' })}
                    className={`inline-flex items-center gap-1.5 px-4 py-2 text-sm font-bold transition-colors rounded-r-lg
                      ${filters.sortBy === 'origreleaseyear' && filters.sortOrder === 'asc' 
                        ? 'bg-blue-50 text-blue-700' 
                        : 'text-gray-700 hover:bg-gray-50'}`}
                    title="Original Release Year (Oldest First)"
                  >
                    <Calendar className="w-4 h-4" />
                    <span className="hidden sm:inline">Year ↑</span>
                  </button>
                </div>
              </div>
            </div>

            {/* Results */}
            <MusicReleaseList 
              filters={filters}
              pageSize={60}
            />
          </>
        )}
      </div>
    </div>
  );
}
