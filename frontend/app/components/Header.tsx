'use client';

import React, { useEffect, useState } from 'react';
import { usePathname, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { QuickSearch } from './SearchAndFilter';
import { Plus } from 'lucide-react';
import { getPagedCount, SearchSuggestion } from '../lib/api';

const Header: React.FC = () => {
  const pathname = usePathname();
  const [totalReleases, setTotalReleases] = useState<number | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const count = await getPagedCount('/api/musicreleases');
        setTotalReleases(count);
      } catch (error) {
        console.error('Failed to fetch release count:', error);
      }
    };
    fetchStats();
  }, []);

  // Determine page title based on pathname
  const getPageInfo = () => {
    switch (pathname) {
      case '/':
        return { title: 'Dashboard', subtitle: 'Your music collection at a glance', showSearch: false };
      case '/collection':
        return { 
          title: 'Music Collection', 
          subtitle: totalReleases !== null ? `${totalReleases.toLocaleString()} releases in your collection` : 'Loading...',
          showSearch: true 
        };
      case '/add':
        return { title: 'Add Release', subtitle: 'Add new music to your collection', showSearch: false };
      case '/search':
        return { title: 'Search', subtitle: 'Find specific releases', showSearch: false };
      case '/query':
        return { title: 'Ask Questions', subtitle: 'Query your collection with natural language', showSearch: false };
      case '/statistics':
        return { title: 'Statistics', subtitle: 'Analyze your music collection', showSearch: false };
      case '/artists':
        return { title: 'Artists', subtitle: 'Browse all artists in your collection', showSearch: false };
      case '/genres':
        return { title: 'Genres', subtitle: 'Browse music by genre', showSearch: false };
      case '/import':
        return { title: 'Import', subtitle: 'Import your music collection', showSearch: false };
      case '/export':
        return { title: 'Export', subtitle: 'Export your collection data', showSearch: false };
      case '/settings':
        return { title: 'Settings', subtitle: 'Configure your preferences', showSearch: false };
      case '/profile':
        return { title: 'Profile', subtitle: 'Manage your profile', showSearch: false };
      default:
        if (pathname.startsWith('/releases/')) {
          return { title: 'Release Details', subtitle: 'View release information', showSearch: false };
        }
        return { title: 'Music Collection', subtitle: 'Manage your music library', showSearch: false };
    }
  };

  const { title, subtitle, showSearch } = getPageInfo();
  const headerRef = React.useRef<HTMLElement | null>(null);
  const logoRef = React.useRef<HTMLImageElement | null>(null);
  const searchRef = React.useRef<HTMLDivElement | null>(null);
  const filtersRef = React.useRef<HTMLDivElement | null>(null);
  const [stackSearch, setStackSearch] = useState(false);
  const [searchStyle, setSearchStyle] = useState<React.CSSProperties | undefined>(undefined);
  const [compact, setCompact] = useState(false);

  // Keep a CSS variable with the header's height so layout can add top padding
  useEffect(() => {
    const measure = () => {
      const height = headerRef.current?.offsetHeight ?? 0;
      document.documentElement.style.setProperty('--app-header-height', `${height}px`);

      // Also decide if we can safely stack the search bar under the logo inside the banner
      // and compute a safe max-width/margin for the search bar when it sits inline.
      const headerRect = headerRef.current?.getBoundingClientRect();
      const logoRect = logoRef.current?.getBoundingClientRect();
      const searchRect = searchRef.current?.getBoundingClientRect();
      const filtersRect = filtersRef.current?.getBoundingClientRect();

      if (!headerRect || !logoRect || !searchRect) {
        setStackSearch(false);
        setSearchStyle(undefined);
        return;
      }

      const gap = 12; // spacing between logo and search

      // Determine if stacking under the logo will fit within the banner (header) height
      const logoBottomRel = logoRect.bottom - headerRect.top; // pixels from header top to logo bottom
      const availableBelowLogo = headerRect.height - logoBottomRel;

      // If the search control would fit below the logo inside the banner, allow stacking
      const canStack = searchRect.height + gap <= availableBelowLogo;

      if (canStack) {
        // Make search occupy full width when stacked under the logo
        setStackSearch(true);
        setSearchStyle({ marginLeft: 0, width: '100%', maxWidth: '100%' });
      } else {
        // Keep search inline (never allow it to drop under the logo) — compute available horizontal space
        setStackSearch(false);

        const leftOffset = Math.max(logoRect.right - headerRect.left + gap, 12); // ensure at least a small left offset
        const rightReserved = filtersRect ? filtersRect.width + gap * 2 : 320; // reserve space for the buttons area
        const availableWidth = Math.max(headerRect.width - leftOffset - rightReserved, 160);

        setSearchStyle({ marginLeft: leftOffset, maxWidth: `${availableWidth}px` });
      }
    };

    // Measure immediately and whenever the header size changes (visible state/content/compact)
    measure();
    window.addEventListener('resize', measure);
    return () => window.removeEventListener('resize', measure);
  }, [showSearch, totalReleases, pathname, compact]);

  // Shrink header when scrolling down (smooth, no layout jump because --app-header-height is updated)
  useEffect(() => {
    let rAF = -1 as number | undefined;

    const handleScroll = () => {
      const y = window.scrollY ?? window.pageYOffset ?? 0;
      // threshold in pixels before header compacts
      const shouldCompact = y > 80;

      // schedule update in rAF for better perf
      if (rAF !== -1) cancelAnimationFrame(rAF as number);
      rAF = requestAnimationFrame(() => {
        setCompact((prev) => {
          if (prev === shouldCompact) return prev;
          return shouldCompact;
        });
      });
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', handleScroll);
      if (rAF !== -1) cancelAnimationFrame(rAF as number);
    };
  }, []);

  const router = useRouter();
  const searchParams = useSearchParams();

  const handleHeaderSearch = (query: string) => {
    const qs = query ? `?search=${encodeURIComponent(query)}` : '';
    // navigate to collection with query -> collection page reads URL params
    router.push(`/collection${qs}`);
  };

  const handleSuggestionSelect = (s: SearchSuggestion) => {
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

    // default: search by name on collection page
    router.push(`/collection?search=${encodeURIComponent(s.name)}`);
  };

  const handleFiltersButton = () => {
    // If not on collection page, open collection and show advanced filters
    if (!pathname.startsWith('/collection')) {
      router.push('/collection?showAdvanced=true');
      return;
    }

    // Toggle the showAdvanced param on the collection page
    const params = new URLSearchParams(Array.from(searchParams.entries()));
    const isOpen = searchParams.get('showAdvanced') === 'true';
    if (isOpen) params.delete('showAdvanced'); else params.set('showAdvanced', 'true');
    const newUrl = params.toString() ? `/collection?${params.toString()}` : '/collection';
    router.replace(newUrl, { scroll: false });
  };

  return (
    <header ref={headerRef} className={`header-with-bg shadow-2xl ${compact ? 'is-compact' : ''}`}>
        <div className="header-overlay">
            {/* top bar sits flush with the top of the hero/banner and contains logo, title, and action */}
            {/* fixed top bar: sticks to the viewport top and is offset to the right of the sidebar */}
            <div className="fixed top-0 right-0 z-10" style={{ left: 'var(--sidebar-offset)' }}>
              <div className="w-full px-6 py-0 flex items-start justify-between">
                <div className="flex items-center gap-4">
                            <img
                              ref={logoRef}
                              src="/images/Kollector-Skum-logo2.png"
                              alt="Kollector Sküm logo"
                              // make logo always match the header/banner height
                              // height uses the measured --app-header-height variable so it will resize with the header
                              // show at natural (intrinsic) image size — do not force height
                              style={{ maxWidth: '40%' }}
                              className={`object-contain transition-all duration-200 border-2 border-white`}
                            />
                  <div>
                    <h1 className={`font-black text-white drop-shadow-lg transition-all duration-200 ${compact ? 'text-2xl' : 'text-4xl'}`}>{title}</h1>
                    <p className={`text-white/90 mt-2 font-bold drop-shadow-md transition-opacity duration-200 ${compact ? 'opacity-60 text-sm' : ''}`}>{subtitle}</p>
                  </div>
                </div>
                <Link 
                  href="/add"
                  className="text-white px-6 py-3 rounded-lg font-bold transition-all hover:shadow-2xl flex items-center gap-2 shadow-lg bg-[#D93611] hover:bg-[#B82E0E]"
                >
                  <Plus size={20} />
                  <span>Add Release</span>
                </Link>
              </div>
            </div>

            <div className="max-w-7xl mx-auto px-6 py-6 pt-[7.5rem] lg:pt-[12rem]">

          {/* Search and Filter Bar - Only show on collection page */}
          {showSearch && (
            <div className={`flex ${stackSearch ? 'flex-col' : 'flex-wrap'} gap-3`}>
              {/* QuickSearch in header */}
              <div ref={searchRef} style={searchStyle} className="search-bar flex-1 min-w-0 sm:min-w-[300px] relative">
                <QuickSearch onSearch={handleHeaderSearch} onSelectSuggestion={handleSuggestionSelect} placeholder="Search releases, artists, albums..." />
              </div>

              {/* Quick Filters */}
              <div ref={filtersRef} className="flex gap-2 flex-wrap">
                {(() => {
                  const isAdvancedOpen = pathname.startsWith('/collection') && searchParams.get('showAdvanced') === 'true';
                  const base = 'px-4 py-2 rounded-lg border-2 transition-all font-bold flex items-center gap-2 shadow-md';
                  // Use brand color when filters are hidden so it matches the other action buttons
                  const unpressed = 'text-white bg-[#8C240D] border-[#8C240D] hover:opacity-90';
                  // Keep Add Release as-is; use #8C240D for the Filters "pressed" state per request
                  // When advanced filters are visible, use the requested orange accent (#F28A2E)
                  const pressed = 'text-white bg-[#F28A2E] border-[#F28A2E]';

                  return (
                    <button
                      onClick={handleFiltersButton}
                      aria-pressed={isAdvancedOpen}
                      className={`${base} ${isAdvancedOpen ? pressed : unpressed}`}
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
                      </svg>
                      <span className={""}>Filters</span>
                    </button>
                  );
                })()}
                <button className="px-4 py-2 rounded-lg border-2 border-[#8C240D] transition-all font-bold text-white flex items-center gap-2 shadow-md bg-[#8C240D] hover:opacity-90">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4" />
                  </svg>
                  <span>Sort</span>
                </button>
                <button className="px-4 py-2 rounded-lg border-2 border-[#8C240D] transition-all font-bold text-white shadow-md bg-[#8C240D] hover:opacity-90">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                  </svg>
                </button>
                <button className="px-4 py-2 rounded-lg border-2 border-[#8C240D] transition-all font-bold text-white shadow-md bg-[#8C240D] hover:opacity-90">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
                  </svg>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;
