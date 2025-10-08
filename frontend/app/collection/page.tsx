"use client";
import { useState } from "react";
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
}

export default function CollectionPage() {
  const [filters, setFilters] = useState<SearchFilters>({});

  const handleFiltersChange = (newFilters: SearchFilters) => {
    setFilters(newFilters);
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
        {/* Search and Filters */}
        <SearchAndFilter 
          onFiltersChange={handleFiltersChange}
          initialFilters={filters}
        />

        {/* Results */}
        <MusicReleaseList 
          filters={filters}
          pageSize={20}
        />
      </div>
    </div>
  );
}
