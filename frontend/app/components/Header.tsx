"use client";

import React from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { QuickSearch } from './SearchAndFilter';
import type { SearchSuggestion } from '../lib/api';

/**
 * Simple header: logo and site title aligned with the page content container.
 * Kept intentionally minimal so alignment is handled by the shared `.max-w-7xl.mx-auto` wrapper.
 */
export default function Header() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const [headerQuery, setHeaderQuery] = React.useState('');
  const filtersOpen = pathname === '/collection' && (searchParams?.get('showAdvanced') === 'true');
  return (
    <header
      className="relative bg-cover bg-center shadow-sm"
      style={{ backgroundImage: "url('/images/Kollector-Skum-bg.png')" }}
    >
      {/* dark overlay for legibility */}
      <div className="absolute inset-0 bg-black/40" />

      <div className="relative z-10 max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-4">
        <div className="flex items-start gap-4">
          <Link href="/" aria-label="Home" className="block">
            <img
              src="/images/Kollector-Skum-v2.png"
              alt="Kollector Sküm"
              className="h-36 w-auto object-contain shadow-md"
              style={{ display: 'block', backgroundColor: 'rgba(255,255,255,0.02)' }}
            />
          </Link>
          <div className="flex flex-col">
            <p className="text-[1.3125rem] text-white/90 font-semibold" style={{ marginTop: '2rem' }}>
              Organise and discover your music library
            </p>
            {/* QuickSearch, left-justified under the subtitle with a Filters button to the right */}
            <div className="mt-1 w-full max-w-md flex items-center gap-2">
              <div className="flex-1">
                <QuickSearch
                  onQueryChange={(q: string) => setHeaderQuery(q)}
                  placeholder="Search releases, artists, albums..."
                  onSearch={(q: string) => {
                    const qs = q ? `?search=${encodeURIComponent(q)}` : '';
                    router.push(`/collection${qs}`);
                  }}
                  onSelectSuggestion={(s: SearchSuggestion) => {
                    if (s.type === 'release') {
                      router.push(`/releases/${s.id}`);
                      return;
                    }
                    if (s.type === 'artist') {
                      router.push(`/collection?artistId=${s.id}`);
                      return;
                    }
                    if (s.type === 'label') {
                      router.push(`/collection?labelId=${s.id}`);
                      return;
                    }
                    router.push(`/collection?search=${encodeURIComponent(s.name)}`);
                  }}
                />
              </div>
              <button
                type="button"
                onClick={() => {
                  // Preserve relevant filter/sort query params and merge header search
                  const preserveKeys = [
                    'search','artistId','genreId','labelId','countryId','formatId',
                    'live','yearFrom','yearTo','sortBy','sortOrder'
                  ];

                  const incoming = searchParams ?? new URLSearchParams();
                  const params = new URLSearchParams();
                  preserveKeys.forEach((k) => {
                    const v = incoming.get(k);
                    if (v !== null && v !== undefined) params.set(k, v);
                  });

                  // header input should override any existing search param
                  if (headerQuery) params.set('search', headerQuery);

                  if (pathname === '/collection') {
                    // toggle `showAdvanced`
                    const currentlyOpen = incoming.get('showAdvanced') === 'true';
                    // toggle: if currently open -> remove, else set to true
                    if (currentlyOpen) params.delete('showAdvanced');
                    else params.set('showAdvanced', 'true');

                    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
                    router.replace(newUrl, { scroll: false });
                    return;
                  }

                  // not on collection — open collection with filters and preserved search
                  params.set('showAdvanced', 'true');
                  const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
                  router.push(newUrl);
                }}
                className={`inline-flex items-center gap-2 px-3 py-2 rounded-md text-white cursor-pointer ${filtersOpen ? 'bg-[#F28A2E]/50 hover:bg-[#F28A2E]/40' : 'bg-white/10 hover:bg-white/20'}`}
                aria-label="Open filters"
                title="Open filters"
              >
                <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5h18M6 12h12M10 19h4" />
                </svg>
                <span className="hidden sm:inline">Filters</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}
