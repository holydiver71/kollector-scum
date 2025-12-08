"use client";

import React from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { QuickSearch } from './SearchAndFilter';
import type { SearchSuggestion } from '../lib/api';

/**
 * Simple header: logo and site title aligned with the page content container.
 * Kept intentionally minimal so alignment is handled by the shared `.max-w-7xl.mx-auto` wrapper.
 */
export default function Header() {
  const router = useRouter();
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
            <p className="text-[1.3125rem] text-white/90 font-semibold" style={{ marginTop: '1.5rem' }}>
              Organise and discover your music library
            </p>
            {/* QuickSearch, left-justified under the subtitle */}
            <div className="mt-3 w-full max-w-md">
              <QuickSearch
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
          </div>
        </div>
      </div>
    </header>
  );
}
