"use client";
import React, { useState, useEffect, useRef } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { CountryDropdown, GenreDropdown, ArtistDropdown, LabelDropdown, FormatDropdown } from "./LookupComponents";
import { getSearchSuggestions, SearchSuggestion } from '../lib/api';

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

interface SearchAndFilterProps {
  onFiltersChange: (filters: SearchFilters) => void;
  initialFilters?: SearchFilters;
  enableUrlSync?: boolean; // Enable URL parameter synchronization
}

export function SearchAndFilter({ onFiltersChange, initialFilters = {}, enableUrlSync = false }: SearchAndFilterProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [filters, setFilters] = useState<SearchFilters>(initialFilters);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestionIndex, setSuggestionIndex] = useState(-1);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  // Sync local state when initialFilters prop changes
  useEffect(() => {
    console.log('SearchAndFilter initialFilters changed:', initialFilters);
    setIsInitializing(true);
    setFilters(initialFilters);
    // Show advanced filters if any advanced filter is set in initialFilters
    if (initialFilters.artistId || initialFilters.genreId || initialFilters.labelId || 
        initialFilters.countryId || initialFilters.formatId || initialFilters.live || 
        initialFilters.yearFrom || initialFilters.yearTo) {
      setShowAdvanced(true);
    }
    // Reset initializing flag after a short delay
    const timer = setTimeout(() => {
      console.log('SearchAndFilter initialization complete');
      setIsInitializing(false);
    }, 200);
    return () => clearTimeout(timer);
  }, [initialFilters]);

  // Update URL when filters change (but not during initialization)
  useEffect(() => {
    if (enableUrlSync && !isInitializing) {
      console.log('SearchAndFilter updating URL, filters:', filters, 'isInitializing:', isInitializing);
      const params = new URLSearchParams();
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          params.set(key, value.toString());
        }
      });
      const newUrl = params.toString() ? `?${params.toString()}` : window.location.pathname;
      router.replace(newUrl, { scroll: false });
    }
  }, [filters, enableUrlSync, router, isInitializing]);

  // Fetch suggestions when search text changes
  useEffect(() => {
    const fetchSuggestions = async () => {
      if (filters.search && filters.search.length >= 2) {
        try {
          const results = await getSearchSuggestions(filters.search);
          setSuggestions(results);
          setShowSuggestions(true);
        } catch (error) {
          console.error('Failed to fetch suggestions:', error);
          setSuggestions([]);
        }
      } else {
        setSuggestions([]);
        setShowSuggestions(false);
      }
    };

    const timeoutId = setTimeout(fetchSuggestions, 300); // Debounce
    return () => clearTimeout(timeoutId);
  }, [filters.search]);

  // Close suggestions when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (suggestionsRef.current && !suggestionsRef.current.contains(event.target as Node) &&
          searchInputRef.current && !searchInputRef.current.contains(event.target as Node)) {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Update filters when they change
  const updateFilters = (newFilters: Partial<SearchFilters>) => {
    const updatedFilters = { ...filters, ...newFilters };
    console.log('SearchAndFilter updateFilters called:', { old: filters, new: newFilters, result: updatedFilters });
    setFilters(updatedFilters);
    onFiltersChange(updatedFilters);
  };

  // Clear all filters
  const clearFilters = () => {
    const emptyFilters: SearchFilters = {};
    setFilters(emptyFilters);
    onFiltersChange(emptyFilters);
    setSuggestions([]);
    setShowSuggestions(false);
  };

  // Handle suggestion selection
  const handleSuggestionClick = (suggestion: SearchSuggestion) => {
    if (suggestion.type === 'release') {
      router.push(`/releases/${suggestion.id}`);
    } else if (suggestion.type === 'artist') {
      updateFilters({ search: undefined, artistId: suggestion.id });
    } else if (suggestion.type === 'label') {
      updateFilters({ search: undefined, labelId: suggestion.id });
    }
    setShowSuggestions(false);
    setSuggestionIndex(-1);
  };

  // Handle keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!showSuggestions || suggestions.length === 0) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSuggestionIndex(prev => (prev < suggestions.length - 1 ? prev + 1 : prev));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSuggestionIndex(prev => (prev > 0 ? prev - 1 : -1));
    } else if (e.key === 'Enter' && suggestionIndex >= 0) {
      e.preventDefault();
      handleSuggestionClick(suggestions[suggestionIndex]);
    } else if (e.key === 'Escape') {
      setShowSuggestions(false);
      setSuggestionIndex(-1);
    }
  };

  // Copy filter URL to clipboard
  const copyFilterUrl = () => {
    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.set(key, value.toString());
      }
    });
    const url = `${window.location.origin}${window.location.pathname}?${params.toString()}`;
    navigator.clipboard.writeText(url);
    alert('Filter URL copied to clipboard!');
  };

  // Check if any filters are active
  const hasActiveFilters = Object.keys(filters).some(key => {
    const value = filters[key as keyof SearchFilters];
    return value !== undefined && value !== null && value !== '';
  });

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
      <div className="space-y-4">
        {/* Search Input with Autocomplete */}
        <div>
          <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-2">
            Search Releases
          </label>
          <div className="relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <input
              ref={searchInputRef}
              id="search"
              type="text"
              placeholder="Search by title, artist, or label..."
              value={filters.search || ''}
              onChange={(e) => updateFilters({ search: e.target.value || undefined })}
              onKeyDown={handleKeyDown}
              onFocus={() => suggestions.length > 0 && setShowSuggestions(true)}
              className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              autoComplete="off"
            />
            
            {/* Autocomplete Suggestions */}
            {showSuggestions && suggestions.length > 0 && (
              <div 
                ref={suggestionsRef}
                className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto"
              >
                {suggestions.map((suggestion, index) => (
                  <button
                    key={`${suggestion.type}-${suggestion.id}`}
                    onClick={() => handleSuggestionClick(suggestion)}
                    className={`w-full text-left px-4 py-2 hover:bg-gray-100 flex items-center justify-between ${
                      index === suggestionIndex ? 'bg-blue-50' : ''
                    }`}
                  >
                    <div>
                      <div className="font-medium text-gray-900">{suggestion.name}</div>
                      {suggestion.subtitle && (
                        <div className="text-sm text-gray-500">{suggestion.subtitle}</div>
                      )}
                    </div>
                    <span className="text-xs text-gray-400 capitalize">{suggestion.type}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Toggle Advanced Filters & Share Button */}
        <div className="flex items-center justify-between">
          <button
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="flex items-center gap-2 text-sm font-medium text-blue-600 hover:text-blue-700"
          >
            <span>{showAdvanced ? 'Hide' : 'Show'} Advanced Filters</span>
            <svg 
              className={`h-4 w-4 transition-transform ${showAdvanced ? 'rotate-180' : ''}`}
              fill="none" 
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <div className="flex items-center gap-2">
            {hasActiveFilters && enableUrlSync && (
              <button
                onClick={copyFilterUrl}
                className="text-sm font-medium text-green-600 hover:text-green-700 flex items-center gap-1"
                title="Copy shareable link"
              >
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                </svg>
                Share
              </button>
            )}
            {hasActiveFilters && (
              <button
                onClick={clearFilters}
                className="text-sm font-medium text-red-600 hover:text-red-700"
              >
                Clear All Filters
              </button>
            )}
          </div>
        </div>

        {/* Advanced Filters */}
        {showAdvanced && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 pt-4 border-t border-gray-200">
            {/* Artist Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Artist
              </label>
              <ArtistDropdown
                value={filters.artistId}
                onSelect={(artist) => updateFilters({ artistId: artist?.id })}
              />
            </div>

            {/* Genre Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Genre
              </label>
              <GenreDropdown
                value={filters.genreId}
                onSelect={(genre) => updateFilters({ genreId: genre?.id })}
              />
            </div>

            {/* Label Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Label
              </label>
              <LabelDropdown
                value={filters.labelId}
                onSelect={(label) => updateFilters({ labelId: label?.id })}
              />
            </div>

            {/* Country Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Country
              </label>
              <CountryDropdown
                value={filters.countryId}
                onSelect={(country) => updateFilters({ countryId: country?.id })}
              />
            </div>

            {/* Format Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Format
              </label>
              <FormatDropdown
                value={filters.formatId}
                onSelect={(format) => updateFilters({ formatId: format?.id })}
              />
            </div>

            {/* Live Recording Filter */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Recording Type
              </label>
              <select
                value={filters.live === undefined ? '' : filters.live.toString()}
                onChange={(e) => {
                  const value = e.target.value;
                  updateFilters({ 
                    live: value === '' ? undefined : value === 'true' 
                  });
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="">All recordings</option>
                <option value="false">Studio recordings</option>
                <option value="true">Live recordings</option>
              </select>
            </div>

            {/* Year Range Filters */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                From Year
              </label>
              <input
                type="number"
                placeholder="e.g., 1970"
                value={filters.yearFrom || ''}
                onChange={(e) => updateFilters({ yearFrom: e.target.value ? parseInt(e.target.value) : undefined })}
                min="1900"
                max={new Date().getFullYear()}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                To Year
              </label>
              <input
                type="number"
                placeholder="e.g., 2024"
                value={filters.yearTo || ''}
                onChange={(e) => updateFilters({ yearTo: e.target.value ? parseInt(e.target.value) : undefined })}
                min="1900"
                max={new Date().getFullYear()}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>
        )}

        {/* Active Filters Display */}
        {hasActiveFilters && (
          <div className="pt-4 border-t border-gray-200">
            <div className="flex items-center gap-2 mb-2">
              <span className="text-sm font-medium text-gray-700">Active filters:</span>
            </div>
            <div className="flex flex-wrap gap-2">
              {filters.search && (
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  Search: &ldquo;{filters.search}&rdquo;
                  <button
                    onClick={() => updateFilters({ search: undefined })}
                    className="ml-1 text-blue-600 hover:text-blue-800"
                  >
                    ×
                  </button>
                </span>
              )}
              {filters.live !== undefined && (
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                  {filters.live ? 'Live recordings' : 'Studio recordings'}
                  <button
                    onClick={() => updateFilters({ live: undefined })}
                    className="ml-1 text-purple-600 hover:text-purple-800"
                  >
                    ×
                  </button>
                </span>
              )}
              {filters.yearFrom && (
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  From: {filters.yearFrom}
                  <button
                    onClick={() => updateFilters({ yearFrom: undefined })}
                    className="ml-1 text-green-600 hover:text-green-800"
                  >
                    ×
                  </button>
                </span>
              )}
              {filters.yearTo && (
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  To: {filters.yearTo}
                  <button
                    onClick={() => updateFilters({ yearTo: undefined })}
                    className="ml-1 text-green-600 hover:text-green-800"
                  >
                    ×
                  </button>
                </span>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// Quick search component for smaller spaces
export function QuickSearch({ 
  onSearch, 
  placeholder = "Search releases..." 
}: { 
  onSearch: (query: string) => void;
  placeholder?: string;
}) {
  const [query, setQuery] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSearch(query);
  };

  return (
    <form onSubmit={handleSubmit} className="relative">
      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
        <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      </div>
      <input
        type="text"
        placeholder={placeholder}
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        className="block w-full pl-10 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
      />
      {query && (
        <button
          type="button"
          onClick={() => {
            setQuery('');
            onSearch('');
          }}
          className="absolute inset-y-0 right-8 flex items-center text-gray-400 hover:text-gray-600"
        >
          ×
        </button>
      )}
      <button
        type="submit"
        className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600"
      >
        <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
        </svg>
      </button>
    </form>
  );
}
