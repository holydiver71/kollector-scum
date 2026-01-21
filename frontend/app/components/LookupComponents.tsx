"use client";
import { useState, useEffect, useRef, useCallback, useLayoutEffect } from "react";
import { createPortal } from 'react-dom';
import * as api from "../lib/api";

// Type definitions for lookup data
interface LookupItem {
  id: number;
  name: string;
}

interface PaginatedResponse<T> {
  items?: T[];
  hasNext?: boolean;
  totalPages?: number;
}

interface Country extends LookupItem {
  iso: string;
}

interface Genre extends LookupItem {
  description?: string;
}

interface Artist extends LookupItem {
  country?: string;
}

interface Label extends LookupItem {
  country?: string;
}

interface Format extends LookupItem {
  description?: string;
}

// Generic dropdown component for lookup data
interface LookupDropdownProps<T extends LookupItem> {
  items: T[];
  value?: number;
  placeholder: string;
  onSelect: (item: T | null) => void;
  loading?: boolean;
  searchable?: boolean;
  className?: string;
}

export function LookupDropdown<T extends LookupItem>({
  items,
  value,
  placeholder,
  onSelect,
  loading = false,
  searchable = false,
  className = ""
}: LookupDropdownProps<T>) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const containerRef = useRef<HTMLDivElement | null>(null);
  const buttonRef = useRef<HTMLButtonElement | null>(null);
  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties | null>(null);

  const filteredItems = searchable 
    ? items.filter(item => 
        item.name.toLowerCase().includes(searchTerm.toLowerCase())
      )
    : items;

  const selectedItem = items.find(item => item.id === value);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      const insideContainer = containerRef.current && containerRef.current.contains(target);
      const insideDropdown = dropdownRef.current && dropdownRef.current.contains(target);
      if (!insideContainer && !insideDropdown) setIsOpen(false);
    };

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsOpen(false);
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscape);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  // Position the portal dropdown so it aligns with the button
  const updateDropdownPosition = () => {
    if (typeof window === 'undefined') return;
    const btn = buttonRef.current;
    if (!btn) return setDropdownStyle(null);
    const r = btn.getBoundingClientRect();
    setDropdownStyle({
      position: 'absolute',
      top: `${r.bottom + window.scrollY}px`,
      left: `${r.left + window.scrollX}px`,
      width: `${r.width}px`,
      zIndex: 9999,
    });
  };

  useLayoutEffect(() => {
    if (!isOpen) return;
    updateDropdownPosition();
    window.addEventListener('resize', updateDropdownPosition);
    window.addEventListener('scroll', updateDropdownPosition, true);
    return () => {
      window.removeEventListener('resize', updateDropdownPosition);
      window.removeEventListener('scroll', updateDropdownPosition, true);
    };
  }, [isOpen]);

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <button
        ref={buttonRef}
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        disabled={loading}
        className="w-full px-3 py-2 text-left bg-white/5 border border-white/10 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-white focus:border-white disabled:bg-white/5 disabled:opacity-50 transition-all"
      >
        <span className={selectedItem ? "text-white" : "text-gray-400"}>
          {loading ? "Loading..." : selectedItem?.name || placeholder}
        </span>
        <span className="absolute inset-y-0 right-0 flex items-center pr-2">
          <svg className="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd" />
          </svg>
        </span>
      </button>

      {isOpen && dropdownStyle && createPortal(
        <div ref={dropdownRef} style={dropdownStyle} className="bg-neutral-900 border border-white/10 rounded-md shadow-lg backdrop-blur-xl">
          {searchable && (
            <div className="p-2">
              <input
                type="text"
                placeholder="Search..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full px-3 py-1 bg-white/5 border border-white/10 rounded-md focus:outline-none focus:ring-1 focus:ring-white text-white placeholder-gray-500"
              />
            </div>
          )}
          <div className="max-h-60 overflow-auto">
            {selectedItem && (
              <button
                type="button"
                onClick={() => {
                  onSelect(null);
                  setIsOpen(false);
                  setSearchTerm("");
                }}
                className="w-full px-3 py-2 text-left text-gray-400 hover:bg-white/10 border-b border-white/10"
              >
                Clear selection
              </button>
            )}
            {filteredItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => {
                  onSelect(item);
                  setIsOpen(false);
                  setSearchTerm("");
                }}
                className={`w-full px-3 py-2 text-left hover:bg-red-600/20 transition-colors ${
                  item.id === value ? "bg-red-600/30 text-white" : "text-gray-300"
                }`}
              >
                {item.name}
              </button>
            ))}
            {filteredItems.length === 0 && (
              <div className="px-3 py-2 text-gray-500 text-center">
                No items found
              </div>
            )}
          </div>
        </div>,
        document.body
      )}
    </div>
  );
}

const lookupCache: Record<string, { data: unknown[]; timestamp: number }> = {};
const CACHE_TTL = 5 * 60 * 1000; // 5 minutes

// Test helper: allow tests to clear the module-level lookup cache so
// subsequent test runs reliably exercise network code instead of
// returning previously cached values.
export function clearLookupCache() {
  Object.keys(lookupCache).forEach((k) => delete lookupCache[k]);
}

// Hook for fetching lookup data
export function useLookupData<T extends LookupItem>(endpoint: string) {
  const [data, setData] = useState<T[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async (force = false) => {
    if (!force) {
      const cached = lookupCache[endpoint];
      if (cached && (Date.now() - cached.timestamp < CACHE_TTL)) {
        // cached.data is stored as unknown[] at module scope; assert to T[] here
        setData(cached.data as T[]);
        setLoading(false);
        return;
      }
    }

    try {
      setLoading(true);
      setError(null);
      
      let allItems: T[] = [];
      let currentPage = 1;
      let hasMore = true;
      
      // Fetch all pages
      while (hasMore) {
        const response = await api.fetchJson<PaginatedResponse<T> | T[]>(`/api/${endpoint}?pageSize=1000&page=${currentPage}`);
        console.log(`Fetched ${endpoint} page ${currentPage}:`, response);
        
        // Handle both paginated response and direct array response
        let items: T[] = [];
        if (Array.isArray(response)) {
          items = response;
          hasMore = false; // Direct array means no pagination
        } else {
          items = response.items || [];
          hasMore = response.hasNext || (response.totalPages ? currentPage < response.totalPages : false);
        }
        
        allItems = [...allItems, ...items];
        currentPage++;
      }
      
      lookupCache[endpoint] = { data: allItems, timestamp: Date.now() };
      setData(allItems);
      console.log(`Loaded ${allItems.length} total ${endpoint}, first: ${allItems[0]?.name}, last: ${allItems[allItems.length - 1]?.name}`);
      
    } catch (err) {
      console.error(`Error fetching ${endpoint}:`, err);
      setError(err instanceof Error ? err.message : "Failed to load data");
    } finally {
      setLoading(false);
    }
  }, [endpoint]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: () => fetchData(true) };
}

// Specific lookup components
export function CountryDropdown({ value, onSelect, className }: {
  value?: number;
  onSelect: (country: Country | null) => void;
  className?: string;
}) {
  const { data, loading, error } = useLookupData<Country>("countries");

  if (error) {
    return (
      <div className={`text-red-600 text-sm ${className}`}>
        Error loading countries: {error}
      </div>
    );
  }

  return (
    <LookupDropdown
      items={data}
      value={value}
      placeholder="Select country..."
      onSelect={onSelect}
      loading={loading}
      searchable={true}
      className={className}
    />
  );
}

interface KollectionDto {
  id: number;
  name: string;
  genreIds: number[];
  genreNames: string[];
}

export function GenreDropdown({ value, onSelect, className, kollectionId }: {
  value?: number;
  onSelect: (genre: Genre | null) => void;
  className?: string;
  kollectionId?: number;
}) {
  const { data: allGenres, loading, error } = useLookupData<Genre>("genres");
  const [kollectionGenreIds, setKollectionGenreIds] = useState<number[]>([]);
  const [loadingKollection, setLoadingKollection] = useState(false);

  // Load kollection genres if kollectionId is provided
  useEffect(() => {
    if (kollectionId) {
      setLoadingKollection(true);
      api.fetchJson<KollectionDto>(`/api/kollections/${kollectionId}`)
        .then((kollection) => {
          setKollectionGenreIds(kollection.genreIds || []);
        })
        .catch((err) => {
          console.error('Failed to load kollection genres:', err);
          setKollectionGenreIds([]);
        })
        .finally(() => {
          setLoadingKollection(false);
        });
    } else {
      setKollectionGenreIds([]);
    }
  }, [kollectionId]);

  if (error) {
    return (
      <div className={`text-red-600 text-sm ${className}`}>
        Error loading genres: {error}
      </div>
    );
  }

  // Filter genres based on kollection
  const data = kollectionId && kollectionGenreIds.length > 0
    ? allGenres.filter(genre => kollectionGenreIds.includes(genre.id))
    : allGenres;

  return (
    <LookupDropdown
      items={data}
      value={value}
      placeholder="Select genre..."
      onSelect={onSelect}
      loading={loading || loadingKollection}
      searchable={true}
      className={className}
    />
  );
}

export function ArtistDropdown({ value, onSelect, className }: {
  value?: number;
  onSelect: (artist: Artist | null) => void;
  className?: string;
}) {
  const { data, loading, error } = useLookupData<Artist>("artists");

  if (error) {
    return (
      <div className={`text-red-600 text-sm ${className}`}>
        Error loading artists: {error}
      </div>
    );
  }

  return (
    <LookupDropdown
      items={data}
      value={value}
      placeholder="Select artist..."
      onSelect={onSelect}
      loading={loading}
      searchable={true}
      className={className}
    />
  );
}

export function LabelDropdown({ value, onSelect, className }: {
  value?: number;
  onSelect: (label: Label | null) => void;
  className?: string;
}) {
  const { data, loading, error } = useLookupData<Label>("labels");

  if (error) {
    return (
      <div className={`text-red-600 text-sm ${className}`}>
        Error loading labels: {error}
      </div>
    );
  }

  return (
    <LookupDropdown
      items={data}
      value={value}
      placeholder="Select label..."
      onSelect={onSelect}
      loading={loading}
      searchable={true}
      className={className}
    />
  );
}

export function FormatDropdown({ value, onSelect, className }: {
  value?: number;
  onSelect: (format: Format | null) => void;
  className?: string;
}) {
  const { data, loading, error } = useLookupData<Format>("formats");

  if (error) {
    return (
      <div className={`text-red-600 text-sm ${className}`}>
        Error loading formats: {error}
      </div>
    );
  }

  return (
    <LookupDropdown
      items={data}
      value={value}
      placeholder="Select format..."
      onSelect={onSelect}
      loading={loading}
      searchable={false}
      className={className}
    />
  );
}

// Lookup table display component
export function LookupTable<T extends LookupItem>({ 
  title, 
  endpoint, 
  columns 
}: {
  title: string;
  endpoint: string;
  columns: Array<{ key: keyof T; label: string; render?: (item: T) => React.ReactNode }>;
}) {
  const { data, loading, error } = useLookupData<T>(endpoint);

  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">{title}</h3>
        <div className="space-y-3">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-8 bg-gray-200 rounded animate-pulse" />
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-red-200 p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">{title}</h3>
        <div className="text-red-600">Error: {error}</div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-lg font-medium text-gray-900">{title}</h3>
        <span className="text-sm text-gray-500">{data.length} items</span>
      </div>
      
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead>
            <tr>
              {columns.map((col) => (
                <th
                  key={String(col.key)}
                  className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                >
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {data.map((item) => (
              <tr key={item.id} className="hover:bg-gray-50">
                {columns.map((col) => (
                  <td key={String(col.key)} className="px-4 py-2 text-sm text-gray-900">
                    {col.render ? col.render(item) : String(item[col.key])}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
