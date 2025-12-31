"use client";

import React from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { QuickSearch } from './SearchAndFilter';
import type { SearchSuggestion } from '../lib/api';
import { getKollections, type KollectionDto } from '../lib/api';
import { GoogleSignIn } from './GoogleSignIn';
import { isAuthenticated, type UserProfile } from '../lib/auth';

/**
 * Simple header: logo and site title aligned with the page content container.
 * Kept intentionally minimal so alignment is handled by the shared `.max-w-7xl.mx-auto` wrapper.
 */
export default function Header() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const normalizedPath = pathname && pathname.endsWith('/') ? pathname.slice(0, -1) : pathname;
  const isMusicCollection = normalizedPath === '/collection';
  const [headerQuery, setHeaderQuery] = React.useState('');
  const filtersOpen = normalizedPath === '/collection' && (searchParams?.get('showAdvanced') === 'true');
  const sortsOpen = normalizedPath === '/collection' && (searchParams?.get('showSort') === 'true');
  const [kollections, setKollections] = React.useState<KollectionDto[]>([]);
  const [loadingKollections, setLoadingKollections] = React.useState(true);

  // Load kollections
  React.useEffect(() => {
    const loadKollections = async () => {
      if (!isAuthenticated()) {
        console.log('[Header] Not authenticated, skipping kollections load');
        setLoadingKollections(false);
        return;
      }

      try {
        console.log('[Header] Loading kollections...');
        const response = await getKollections();
        console.log('[Header] Kollections loaded:', response.items.length, 'items', response);
        setKollections(response.items);
      } catch (err) {
        console.error('[Header] Failed to load kollections:', err);
      } finally {
        setLoadingKollections(false);
      }
    };
    loadKollections();
  }, []);

  const selectedKollectionId = searchParams?.get('kollectionId');

  const handleKollectionChange = (kollectionId: string) => {
    const params = new URLSearchParams(Array.from(searchParams?.entries() || []));
    
    if (kollectionId === 'all') {
      params.delete('kollectionId');
    } else {
      params.set('kollectionId', kollectionId);
    }
    
    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
    router.push(newUrl);
    try {
      if (kollectionId === 'all') localStorage.removeItem('kollectionId');
      else localStorage.setItem('kollectionId', kollectionId);
    } catch (e) {
      // ignore storage errors (e.g., SSR or strict privacy)
    }
  };

  const handleSignIn = (profile: UserProfile) => {
    console.log('User signed in:', profile);
    // Dispatch event to notify other components
    window.dispatchEvent(new Event('authChanged'));
    // Redirect to home page so welcome screen shows for empty collections
    router.push('/');
  };

  return (
    <header
      className="relative bg-cover bg-center shadow-sm"
      style={{ backgroundImage: "url('/images/Kollector-Skum-bg.png')" }}
    >
      {/* dark overlay for legibility */}
      <div className="absolute inset-0 bg-black/40" />

      <div className="relative z-10 max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-4">
        <div className="absolute top-4 right-4 sm:right-6 lg:right-8">
             <GoogleSignIn onSignIn={handleSignIn} />
        </div>

        {/* Kollection selector (moved next to search input) — rendered inline with QuickSearch */}
        <div className="flex flex-col md:flex-row items-start gap-2 md:gap-4">
          <Link href="/" aria-label="Home" className="block">
            <img
              src="/images/Kollector-Skum-v2.png"
              alt="Kollector Sküm"
              className="h-36 w-auto object-contain shadow-md"
              style={{ display: 'block', backgroundColor: 'rgba(255,255,255,0.02)' }}
            />
          </Link>
          <div className="flex flex-col flex-1 min-w-0">
            <p className="text-[1.3125rem] text-white/90 font-semibold mt-2 md:mt-8">
              Organise and discover your music library
            </p>
            {/* Kollection selector moved to top-right of header; no label */}
            {/* QuickSearch, left-justified under the subtitle with a Filters button to the right */}
            <div className="mt-1 w-full flex items-center gap-2">
              <div className="flex-1 min-w-0">
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
              {isMusicCollection && !loadingKollections && kollections.length > 0 && (
                <div className="flex-shrink-0">
                  <select
                    id="kollection-select"
                    value={selectedKollectionId || 'all'}
                    onChange={(e) => handleKollectionChange(e.target.value)}
                    className="w-40 md:w-48 px-3 py-2 bg-white/10 backdrop-blur-sm text-white border border-white/20 rounded-md focus:outline-none focus:ring-2 focus:ring-white/50"
                  >
                    <option value="all" className="text-gray-900">All Music</option>
                    {kollections.map((kollection) => (
                      <option key={kollection.id} value={kollection.id} className="text-gray-900">
                        {kollection.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              {/* Filters button moved into collection header controls */}
              {/* Sort button removed — sort is now controlled inside the collection list */}
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}
