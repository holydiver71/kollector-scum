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
  const [compact, setCompact] = useState(false);

  // Keep a CSS variable with the header's height so layout can add top padding
  useEffect(() => {
    const measure = () => {
      const height = headerRef.current?.offsetHeight ?? 0;
      document.documentElement.style.setProperty('--app-header-height', `${height}px`);
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
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between mb-6">
            <div className="flex items-center gap-4">
              {/* App logo to the left of the title */}
              <img
                src="/images/Kollector-Skum-logo2.png"
                alt="Kollector Sküm logo"
                // add a visible white border so the logo's bounding area is easy to see
                className={`object-contain transition-all duration-200 ${compact ? 'w-[7.5rem] h-[7.5rem]' : 'w-[20rem] h-[10rem]'} border-2 border-white`}
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

          {/* Search and Filter Bar - Only show on collection page */}
          {showSearch && (
            <div className="flex flex-wrap gap-3">
              {/* QuickSearch in header */}
              <div className="search-bar flex-1 min-w-0 sm:min-w-[300px] relative">
                <QuickSearch onSearch={handleHeaderSearch} onSelectSuggestion={handleSuggestionSelect} placeholder="Search releases, artists, albums..." />
              </div>

              {/* Quick Filters */}
              <div className="flex gap-2 flex-wrap">
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
