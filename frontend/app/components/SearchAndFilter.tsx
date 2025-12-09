"use client";
import React, { useState, useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { CountryDropdown, GenreDropdown, ArtistDropdown, LabelDropdown, FormatDropdown, LookupDropdown } from "./LookupComponents";
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
  showSearchInput?: boolean; // control rendering of the search input (header moved search)
  openAdvanced?: boolean; // allow external control of advanced filters visibility
  onAdvancedToggle?: (open: boolean) => void; // notify parent when advanced panel toggles
}

export function SearchAndFilter({ onFiltersChange, initialFilters, enableUrlSync = false, showSearchInput = true, openAdvanced, onAdvancedToggle }: SearchAndFilterProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [filters, setFilters] = useState<SearchFilters>(initialFilters || {});
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestionIndex, setSuggestionIndex] = useState(-1);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  const normalizedInitialFilters = initialFilters || {};

  // Sync local state when initialFilters prop changes
  // NOTE: if parent explicitly controls `openAdvanced`, don't auto-open/close based on initialFilters —
  // respect the parent's explicit intent so the header toggle can hide the panel reliably.
  useEffect(() => {
    console.log('SearchAndFilter initialFilters changed:', normalizedInitialFilters);
    setIsInitializing(true);
    setFilters(normalizedInitialFilters);
    // Only auto-open advanced panel when the parent hasn't explicitly controlled it
    if (openAdvanced === undefined) {
      if (normalizedInitialFilters.artistId || normalizedInitialFilters.genreId || normalizedInitialFilters.labelId || 
          normalizedInitialFilters.countryId || normalizedInitialFilters.formatId || normalizedInitialFilters.live || 
          normalizedInitialFilters.yearFrom || normalizedInitialFilters.yearTo) {
        setShowAdvanced(true);
      } else {
        setShowAdvanced(false);
      }
    }
    // Reset initializing flag after a short delay
    const timer = setTimeout(() => {
      console.log('SearchAndFilter initialization complete');
      setIsInitializing(false);
    }, 200);
    return () => clearTimeout(timer);
  }, [initialFilters, openAdvanced]);

  // If parent gives an explicit openAdvanced prop, keep showAdvanced in sync
  useEffect(() => {
    if (openAdvanced !== undefined) {
      setShowAdvanced(!!openAdvanced);
    }
  }, [openAdvanced]);

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

  // no-op: active filters are displayed and managed at the page level (collection page)

  return (
    <div className="bg-gradient-to-br from-red-900 via-red-950 to-black rounded-lg border border-white/10 p-4 mb-4 sm:p-6 text-white">
      <div className="space-y-4">
        {/* Search Input with Autocomplete */}
        {showSearchInput && (
        <div>
          <label htmlFor="search" className="block text-sm font-bold text-white mb-2">
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
        )}

        {/* Share / Clear area removed - handled at page level */}

        {/* Advanced Filters: animate open/close using max-height + opacity + translate */}
        <div
          aria-hidden={!showAdvanced}
          className={`transition-all duration-200 ease-in-out overflow-hidden ${
            showAdvanced
              ? 'max-h-[1200px] opacity-100 translate-y-0 py-1'
              : 'max-h-0 opacity-0 -translate-y-2 py-0'
          }`}
        >
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold text-white">Filters</h3>
            <button
              type="button"
              onClick={() => {
                if (onAdvancedToggle) onAdvancedToggle(false);
                else setShowAdvanced(false);
              }}
              aria-label="Close filters"
              title="Close filters"
              className="inline-flex items-center justify-center p-1 rounded hover:bg-white/10"
            >
              <X className="w-4 h-4 text-white" />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Artist Filter */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Artist
              </label>
              <ArtistDropdown
                value={filters.artistId}
                onSelect={(artist) => updateFilters({ artistId: artist?.id })}
              />
            </div>

            {/* Genre Filter */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Genre
              </label>
              <GenreDropdown
                value={filters.genreId}
                onSelect={(genre) => updateFilters({ genreId: genre?.id })}
              />
            </div>

            {/* Label Filter */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Label
              </label>
              <LabelDropdown
                value={filters.labelId}
                onSelect={(label) => updateFilters({ labelId: label?.id })}
              />
            </div>

            {/* Country Filter */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Country
              </label>
              <CountryDropdown
                value={filters.countryId}
                onSelect={(country) => updateFilters({ countryId: country?.id })}
              />
            </div>

            {/* Format Filter */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Format
              </label>
              <FormatDropdown
                value={filters.formatId}
                onSelect={(format) => updateFilters({ formatId: format?.id })}
              />
            </div>

            {/* Live Recording Filter - use the LookupDropdown styling for visual consistency */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
                Recording Type
              </label>
              <LookupDropdown
                items={[{ id: 1, name: 'Studio recordings' }, { id: 2, name: 'Live recordings' }]}
                value={filters.live === undefined ? undefined : (filters.live ? 2 : 1)}
                placeholder="All recordings"
                onSelect={(item) => {
                  if (!item) updateFilters({ live: undefined });
                  else updateFilters({ live: item.id === 2 });
                }}
                loading={false}
                searchable={false}
              />
            </div>

            {/* Year Range Filters */}
            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
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

            <div className="bg-white/5 rounded-md border border-white/10 p-4">
              <label className="block text-sm font-medium text-gray-200 mb-2">
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
        </div>

        {/* Removed: Active filters display handled at the page level (collection page) */}
      </div>
    </div>
  );
}

// Quick search component for smaller spaces
export function QuickSearch({
  onSearch,
  placeholder = "Search releases...",
  onSelectSuggestion,
  onQueryChange,
}: {
  onSearch: (query: string) => void;
  placeholder?: string;
  onSelectSuggestion?: (s: SearchSuggestion) => void;
  onQueryChange?: (q: string) => void;
}) {
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestionIndex, setSuggestionIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const id = setTimeout(async () => {
      if (!query || query.length < 2) {
        setSuggestions([]);
        setShowSuggestions(false);
        return;
      }

      try {
        const results = await getSearchSuggestions(query);
        setSuggestions(results);
        setShowSuggestions(results.length > 0);
      } catch (err) {
        console.error('QuickSearch suggestions failed', err);
        setSuggestions([]);
        setShowSuggestions(false);
      }
    }, 250);

    return () => clearTimeout(id);
  }, [query]);

  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (!showSuggestions) return;
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSuggestionIndex((i) => Math.min(i + 1, suggestions.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSuggestionIndex((i) => Math.max(i - 1, -1));
      } else if (e.key === 'Enter') {
        if (suggestionIndex >= 0 && suggestions[suggestionIndex]) {
          e.preventDefault();
          const s = suggestions[suggestionIndex];
          if (onSelectSuggestion) onSelectSuggestion(s);
          else onSearch(s.name);
          setShowSuggestions(false);
        }
      } else if (e.key === 'Escape') {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('keydown', handleKey);
    return () => document.removeEventListener('keydown', handleKey);
  }, [showSuggestions, suggestionIndex, suggestions, onSearch, onSelectSuggestion]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setShowSuggestions(false);
    onSearch(query);
  };

  const handleSuggestionClick = (s: SearchSuggestion) => {
    if (onSelectSuggestion) onSelectSuggestion(s);
    else onSearch(s.name);
    setShowSuggestions(false);
  };

  return (
    <form onSubmit={handleSubmit} className="relative">
      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
        <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      </div>
      <input
        ref={inputRef}
        type="text"
        placeholder={placeholder}
        value={query}
        onChange={(e) => {
          const v = e.target.value;
          setQuery(v);
          if (onQueryChange) onQueryChange(v);
        }}
        onFocus={() => suggestions.length > 0 && setShowSuggestions(true)}
        className="block w-full pl-10 pr-10 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-white/75 focus:bg-white"
        autoComplete="off"
      />
      {query && (
        <button
          type="button"
          onClick={() => {
            setQuery('');
            onSearch('');
            setShowSuggestions(false);
          }}
          className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600 cursor-pointer"
        >
          ×
        </button>
      )}
      {/* Removed submit arrow button — submission still works via Enter key */}

      {showSuggestions && suggestions.length > 0 && (
        <div className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto">
          {suggestions.map((s, idx) => (
            <button
              key={`${s.type}-${s.id}`}
              type="button"
              onClick={() => handleSuggestionClick(s)}
              className={`w-full text-left px-4 py-2 hover:bg-gray-100 flex items-center justify-between ${idx === suggestionIndex ? 'bg-blue-50' : ''}`}
            >
              <div>
                <div className="font-medium text-gray-900">{s.name}</div>
                {s.subtitle && <div className="text-sm text-gray-500">{s.subtitle}</div>}
              </div>
              <span className="text-xs text-gray-400 capitalize">{s.type}</span>
            </button>
          ))}
        </div>
      )}
    </form>
  );
}
