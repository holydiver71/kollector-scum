"use client";
import React from 'react';
import { User, Clock, Disc3, Calendar } from 'lucide-react';

interface Filters {
  sortBy?: string;
  sortOrder?: string;
}

export default function SortPanel({
  filters,
  onChange,
  open,
  onClose,
}: {
  filters: Filters;
  onChange: (f: Filters) => void;
  open: boolean;
  onClose?: () => void;
}) {
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
        return 'Title (A-Z)';
    }
  };
  return (
    <div
      aria-hidden={!open}
      className={`transition-all duration-200 ease-in-out overflow-hidden ${
        open ? 'max-h-[640px] opacity-100 translate-y-0' : 'max-h-0 opacity-0 -translate-y-0'
      } bg-gradient-to-br from-red-900 via-red-950 to-black rounded-lg border border-white/10 px-4 py-4 mb-0 text-white w-full`}
    >
      <div className="flex items-center justify-between">
        <h3
          className="text-sm font-semibold text-white"
          title={`Sorting on ${getSortLabel(filters?.sortBy, filters?.sortOrder)}`}
        >
          {`Sorting on ${getSortLabel(filters?.sortBy, filters?.sortOrder)}`}
        </h3>
        <div>
          <button
            type="button"
            onClick={() => onClose && onClose()}
            aria-label="Close sort"
            title="Close sort"
            className="inline-flex items-center justify-center p-1 rounded hover:bg-white/10"
          >
            {/* lightweight X without adding another import to keep bundle small */}
            <svg className="w-4 h-4 text-white/80" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
      </div>

      <div className="mt-2">
        <div className="flex flex-wrap items-center gap-3">
          {/* Pair: Title A-Z / Title Z-A */}
          <div className="inline-flex rounded-lg border border-white/10 bg-white/5 backdrop-blur-sm shadow-sm">
            <button
              onClick={() => onChange({ sortBy: 'title', sortOrder: 'asc' })}
              className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-l-lg border-r border-white/10
                ${filters.sortBy === 'title' && filters.sortOrder === 'asc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Title (A-Z)"
            >
              <Disc3 className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            </button>
            <button
              onClick={() => onChange({ sortBy: 'title', sortOrder: 'desc' })}
              className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-r-lg
                ${filters.sortBy === 'title' && filters.sortOrder === 'desc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Title (Z-A)"
            >
              <Disc3 className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            </button>
          </div>

          {/* Pair: Artist A-Z / Artist Z-A */}
          <div className="inline-flex rounded-lg border border-white/10 bg-white/5 backdrop-blur-sm shadow-sm">
            <button
              onClick={() => onChange({ sortBy: 'artist', sortOrder: 'asc' })}
                className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-l-lg border-r border-white/10
                ${filters.sortBy === 'artist' && filters.sortOrder === 'asc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Artist (A-Z)"
            >
              <User className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            </button>
            <button
              onClick={() => onChange({ sortBy: 'artist', sortOrder: 'desc' })}
                className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-r-lg
                ${filters.sortBy === 'artist' && filters.sortOrder === 'desc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Artist (Z-A)"
            >
              <User className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            </button>
          </div>

          {/* Pair: Recently Added / Oldest First */}
          <div className="inline-flex rounded-lg border border-white/10 bg-white/5 backdrop-blur-sm shadow-sm">
              <button
                onClick={() => onChange({ sortBy: 'dateadded', sortOrder: 'desc' })}
                className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-l-lg border-r border-white/10
                  ${filters.sortBy === 'dateadded' && filters.sortOrder === 'desc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
                title="Recently Added (Newest first)"
              >
                <Clock className="w-4 h-4" />
                <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                  <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
                </svg>
              </button>
              <button
                onClick={() => onChange({ sortBy: 'dateadded', sortOrder: 'asc' })}
                className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-r-lg
                  ${filters.sortBy === 'dateadded' && filters.sortOrder === 'asc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
                title="Oldest First"
              >
                <Clock className="w-4 h-4" />
                <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                  <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
                </svg>
              </button>
          </div>

          {/* Pair: Original Release Year (Newest / Oldest) */}
          <div className="inline-flex rounded-lg border border-white/10 bg-white/5 backdrop-blur-sm shadow-sm">
            <button
              onClick={() => onChange({ sortBy: 'origreleaseyear', sortOrder: 'desc' })}
              className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-l-lg border-r border-white/10
                ${filters.sortBy === 'origreleaseyear' && filters.sortOrder === 'desc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Original Release Year (Newest First)"
            >
              <Calendar className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,10 2.5,5 4.2,5 4.2,2 7.8,2 7.8,5 9.5,5" />
              </svg>
            </button>
            <button
              onClick={() => onChange({ sortBy: 'origreleaseyear', sortOrder: 'asc' })}
              className={`inline-flex items-center justify-center gap-2 w-12 h-10 text-sm font-bold transition-colors rounded-r-lg
                ${filters.sortBy === 'origreleaseyear' && filters.sortOrder === 'asc' ? 'bg-[#F28A2E]/50 text-white hover:bg-[#F28A2E]/40' : 'text-white/90 hover:bg-[#F28A2E]/10'}`}
              title="Original Release Year (Oldest First)"
            >
              <Calendar className="w-4 h-4" />
              <svg className="w-3 h-3 text-white" viewBox="0 0 12 12" fill="currentColor" xmlns="http://www.w3.org/2000/svg" aria-hidden>
                <polygon points="6,2 9.5,7 7.8,7 7.8,10 4.2,10 4.2,7 2.5,7" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
