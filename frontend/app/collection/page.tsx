"use client";
import { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { SearchAndFilter } from "../components/SearchAndFilter";
import { MusicReleaseList } from "../components/MusicReleaseList";
import { ArrowDownAZ, ArrowUpAZ, User, Clock, Disc3, Calendar } from "lucide-react";

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

  // Load filters from URL whenever URL changes (but not when we just updated it)
  useEffect(() => {
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
      setFilters(urlFilters);
      setIsInitialized(true);
    }
    isUpdatingUrl.current = false;
  }, [searchParams]);

  const handleFiltersChange = (newFilters: SearchFilters) => {
    console.log('CollectionPage handleFiltersChange:', newFilters);
    setFilters(newFilters);
    
    // Update URL
    isUpdatingUrl.current = true;
    const params = new URLSearchParams();
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.set(key, value.toString());
      }
    });
    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
    router.replace(newUrl, { scroll: false });
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <h1 className="text-2xl font-black text-gray-900">Music Collection</h1>
          <p className="text-gray-600 mt-1 font-medium">Browse and search your music releases</p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Search and Filters - only show after initialization to avoid passing empty filters */}
        {isInitialized && (
          <>
            <SearchAndFilter 
              onFiltersChange={handleFiltersChange}
              initialFilters={filters}
              enableUrlSync={false}
            />

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
