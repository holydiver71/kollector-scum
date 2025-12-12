"use client";
import React, { useState, useEffect, useRef, useLayoutEffect } from 'react';
import { createPortal } from 'react-dom';
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
  kollectionId?: number; // filter genres to only those in the kollection
  compact?: boolean; // render with reduced padding/spacing for inline placement
}

export function SearchAndFilter({ onFiltersChange, initialFilters, enableUrlSync = false, showSearchInput = true, openAdvanced, onAdvancedToggle, kollectionId, compact = false }: SearchAndFilterProps) {
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
  const [yearValidationError, setYearValidationError] = useState<string | null>(null);

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
    // Validate year inputs before triggering a parent query
    const validateYearFilters = (f: SearchFilters) => {
      const currentYear = new Date().getFullYear();
      const from = f.yearFrom;
      const to = f.yearTo;

      // Helper: a year is valid if undefined or a number with at least 4 digits and within sensible range
      const isValidYear = (y?: number) => {
        if (y === undefined || y === null) return true;
        if (!Number.isInteger(y)) return false;
        if (y < 1000) return false; // less than 4 digits
        if (y < 1900 || y > currentYear) return false;
        return true;
      };

      if (!isValidYear(from)) return { valid: false, message: "From year must be a 4-digit year between 1900 and current year" };
      if (!isValidYear(to)) return { valid: false, message: "To year must be a 4-digit year between 1900 and current year" };
      if (from !== undefined && to !== undefined && from > to) return { valid: false, message: "From year cannot be greater than To year" };
      return { valid: true, message: null };
    };

    const validation = validateYearFilters(updatedFilters);
    if (!validation.valid) {
      // Keep local UI state updated but don't re-query backend while user input is invalid
      setYearValidationError(validation.message);
      return;
    }

    // Clear any previous validation error and notify parent
    if (yearValidationError) setYearValidationError(null);
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
    <div 
      className={`relative bg-cover bg-center rounded-lg border border-white/10 ${compact ? 'px-4 py-1 mb-0 sm:py-1' : 'p-4 mb-4 sm:p-6'} text-white overflow-visible`}
      style={{ backgroundImage: "url('/images/Kollector-Skum-bg.png')" }}
    >
      {/* dark overlay for legibility */}
      <div className="absolute inset-0 bg-black/70" />

      <div className={`relative z-10 ${compact ? 'space-y-2' : 'space-y-4'}`}>
        {/* Search Input with Autocomplete */}
        {showSearchInput && (
        <div>
          <label htmlFor="search" className="block text-sm font-bold text-gray-200 mb-2 flex items-center gap-2">
            <svg className="h-4 w-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
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
              className="block w-full pl-10 pr-3 py-3 bg-white/5 border border-white/10 rounded-lg focus:outline-none focus:ring-2 focus:ring-white focus:border-white text-white placeholder-gray-400 transition-all duration-200 hover:bg-white/10"
              autoComplete="off"
            />
            
            {/* Autocomplete Suggestions */}
            {showSuggestions && suggestions.length > 0 && (
              <div 
                ref={suggestionsRef}
                className="absolute z-10 w-full mt-1 bg-neutral-900 border border-white/10 rounded-lg shadow-2xl max-h-60 overflow-auto backdrop-blur-xl"
              >
                {suggestions.map((suggestion, index) => (
                  <button
                    key={`${suggestion.type}-${suggestion.id}`}
                    onClick={() => handleSuggestionClick(suggestion)}
                    className={`w-full text-left px-4 py-3 hover:bg-red-600/20 flex items-center justify-between transition-colors border-b border-white/10 last:border-0 ${
                      index === suggestionIndex ? 'bg-red-600/30' : ''
                    }`}
                  >
                    <div>
                      <div className="font-medium text-white">{suggestion.name}</div>
                      {suggestion.subtitle && (
                        <div className="text-sm text-gray-400">{suggestion.subtitle}</div>
                      )}
                    </div>
                    <span className="text-xs text-red-400 capitalize px-2 py-1 bg-red-500/10 rounded-full">{suggestion.type}</span>
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
              : 'max-h-0 opacity-0 -translate-y-0 py-0'
          }`}
        >
          <div className={`flex items-center justify-between ${compact ? 'mb-1' : 'mb-3'}`}>
            <h3 className="text-sm font-semibold text-gray-200">
              Filters
            </h3>
            <button
              type="button"
              onClick={() => {
                if (onAdvancedToggle) onAdvancedToggle(false);
                else setShowAdvanced(false);
              }}
              aria-label="Close filters"
              title="Close filters"
              className="inline-flex items-center justify-center p-1.5 rounded-lg hover:bg-white/10 transition-colors"
            >
              <X className="w-4 h-4 text-gray-300" />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Artist Filter */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                Artist
              </label>
              <ArtistDropdown
                value={filters.artistId}
                onSelect={(artist) => updateFilters({ artistId: artist?.id })}
              />
            </div>

            {/* Genre Filter */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3" />
                </svg>
                Genre
              </label>
              <GenreDropdown
                value={filters.genreId}
                onSelect={(genre) => updateFilters({ genreId: genre?.id })}
                kollectionId={kollectionId}
              />
            </div>

            {/* Label Filter */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 21h18" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 16h5v5M8 16V9l4 2 4-2v7h3" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 7V3h2v4" />
                </svg>
                Label
              </label>
              <LabelDropdown
                value={filters.labelId}
                onSelect={(label) => updateFilters({ labelId: label?.id })}
              />
            </div>

            {/* Country Filter */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Country
              </label>
              <CountryDropdown
                value={filters.countryId}
                onSelect={(country) => updateFilters({ countryId: country?.id })}
              />
            </div>

            {/* Format Filter */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3" />
                  <circle cx="12" cy="12" r="10" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
                Format
              </label>
              <FormatDropdown
                value={filters.formatId}
                onSelect={(format) => updateFilters({ formatId: format?.id })}
              />
            </div>

            {/* Live Recording Filter - use the LookupDropdown styling for visual consistency */}
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z" />
                </svg>
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
            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                From Year
              </label>
              <input
                type="number"
                placeholder="e.g., 1970"
                value={filters.yearFrom || ''}
                onChange={(e) => updateFilters({ yearFrom: e.target.value ? parseInt(e.target.value) : undefined })}
                min="1900"
                max={new Date().getFullYear()}
                className="w-full px-3 py-2 bg-white/5 border border-white/10 rounded-lg focus:outline-none focus:ring-2 focus:ring-white focus:border-white text-white placeholder-gray-400 transition-all"
              />
            </div>

            <div className="group bg-white/5 rounded-lg border border-white/10 p-4 transition-all duration-200 hover:bg-white/10 hover:border-white hover:shadow-lg hover:shadow-white/10">
              <label className="block text-sm font-medium text-gray-300 mb-2 flex items-center gap-2">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                To Year
              </label>
              <input
                type="number"
                placeholder="e.g., 2024"
                value={filters.yearTo || ''}
                onChange={(e) => updateFilters({ yearTo: e.target.value ? parseInt(e.target.value) : undefined })}
                min="1900"
                max={new Date().getFullYear()}
                className="w-full px-3 py-2 bg-white/5 border border-white/10 rounded-lg focus:outline-none focus:ring-2 focus:ring-white focus:border-white text-white placeholder-gray-400 transition-all"
              />
            </div>
              {/* Validation message for year inputs */}
              {yearValidationError && (
                <div className="col-span-1 md:col-span-2 lg:col-span-3 px-4">
                  <p role="status" className="mt-1 text-sm text-red-400">{yearValidationError}</p>
                </div>
              )}
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
  const containerRef = useRef<HTMLFormElement | null>(null);
  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties | null>(null);

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

  // Close suggestions when clicking/tapping outside the quick search form or the portal dropdown
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent | TouchEvent) => {
      const target = e.target as Node | null;
      if (!target) return;
      const insideForm = containerRef.current && containerRef.current.contains(target);
      const insideDropdown = dropdownRef.current && dropdownRef.current.contains(target);
      if (!insideForm && !insideDropdown) setShowSuggestions(false);
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('touchstart', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('touchstart', handleClickOutside);
    };
  }, []);

  // Compute portal dropdown position and size so it aligns with the input
  const updateDropdownPosition = () => {
    const input = inputRef.current;
    if (!input) return setDropdownStyle(null);
    const r = input.getBoundingClientRect();
    setDropdownStyle({
      position: 'absolute',
      top: `${r.bottom + window.scrollY}px`,
      left: `${r.left + window.scrollX}px`,
      width: `${r.width}px`,
      zIndex: 9999,
    });
  };

  useLayoutEffect(() => {
    if (!showSuggestions) return;
    updateDropdownPosition();
    window.addEventListener('resize', updateDropdownPosition);
    window.addEventListener('scroll', updateDropdownPosition, true);
    return () => {
      window.removeEventListener('resize', updateDropdownPosition);
      window.removeEventListener('scroll', updateDropdownPosition, true);
    };
  }, [showSuggestions, suggestions]);

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
    <form ref={containerRef} onSubmit={handleSubmit} className="relative">
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
        className="block w-full pl-10 pr-10 py-2 border border-white/10 rounded-lg focus:outline-none focus:ring-2 focus:ring-white focus:border-white bg-white/5 focus:bg-white/10 text-white placeholder-gray-400 transition-all"
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

      {showSuggestions && suggestions.length > 0 && dropdownStyle && createPortal(
        <div ref={dropdownRef} style={dropdownStyle} className="bg-neutral-900 border border-white/10 rounded-lg shadow-2xl max-h-60 overflow-auto backdrop-blur-xl">
          {suggestions.map((s, idx) => (
            <button
              key={`${s.type}-${s.id}`}
              type="button"
              onClick={() => handleSuggestionClick(s)}
              className={`w-full text-left px-4 py-3 hover:bg-red-600/20 flex items-center justify-between transition-colors border-b border-white/10 last:border-0 ${idx === suggestionIndex ? 'bg-red-600/30' : ''}`}
            >
              <div>
                <div className="font-medium text-white">{s.name}</div>
                {s.subtitle && <div className="text-sm text-gray-400">{s.subtitle}</div>}
              </div>
              <span className="text-xs text-red-400 capitalize px-2 py-1 bg-red-500/10 rounded-full">{s.type}</span>
            </button>
          ))}
        </div>,
        document.body
      )}
    </form>
  );
}
