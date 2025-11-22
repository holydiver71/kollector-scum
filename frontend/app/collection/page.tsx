"use client";
import { useState, useEffect, useRef } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { SearchAndFilter } from "../components/SearchAndFilter";
import { MusicReleaseList } from "../components/MusicReleaseList";

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

      if (search) urlFilters.search = search;
      if (artistId) urlFilters.artistId = parseInt(artistId);
      if (genreId) urlFilters.genreId = parseInt(genreId);
      if (labelId) urlFilters.labelId = parseInt(labelId);
      if (countryId) urlFilters.countryId = parseInt(countryId);
      if (formatId) urlFilters.formatId = parseInt(formatId);
      if (live) urlFilters.live = live === 'true';
      if (yearFrom) urlFilters.yearFrom = parseInt(yearFrom);
      if (yearTo) urlFilters.yearTo = parseInt(yearTo);

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
          <h1 className="text-2xl font-bold text-gray-900">Music Collection</h1>
          <p className="text-gray-600 mt-1">Browse and search your music releases</p>
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
