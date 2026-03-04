'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { isAuthenticated } from '../lib/auth';
import { useCollection } from '../contexts/CollectionContext';
import { 
  Home, 
  Music, 
  PlusCircle, 
  User, 
  List, 
  BarChart3, 
  Settings, 
  UserCircle,
  Menu,
  FolderOpen
} from 'lucide-react';
import { Shuffle } from 'lucide-react';
import { getRandomReleaseId, fetchJson } from '../lib/api';

interface NavigationItem {
  name: string;
  href: string;
  icon: React.ElementType;
}

const Sidebar: React.FC = () => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  // Hooks must be called unconditionally at the top level.
  const _pathname = usePathname();
  // Call hooks unconditionally at top level to satisfy React Hooks rules.
  const router = useRouter();
  const pathname: string = _pathname ?? (typeof window !== 'undefined' ? window.location.pathname : '/');
  const { hasCollection, setHasCollection } = useCollection();

  // Check auth state on mount and route changes
  useEffect(() => {
    setIsLoggedIn(isAuthenticated());
    
    // Listen for auth changes
    const handleAuthChange = () => {
      setIsLoggedIn(isAuthenticated());
    };
    
    window.addEventListener('authChanged', handleAuthChange);
    
    return () => {
      window.removeEventListener('authChanged', handleAuthChange);
    };
  }, [pathname]);

  // Initialize collection status if not already set
  useEffect(() => {
    const checkCollection = async () => {
      if (isLoggedIn && hasCollection === null) {
        try {
          // Request may return 401 if token is invalid; swallow non-OK responses
          // so auth handling can proceed without a noisy console error.
          const data = await fetchJson<{ totalCount: number }>(
            '/api/musicreleases?Pagination.PageNumber=1&Pagination.PageSize=1',
            { swallowErrors: true }
          );

          if (data) {
            setHasCollection(data.totalCount > 0);
          }
          // If data is null (e.g. 401), `fetchJson` already cleared auth and
          // dispatched `authChanged`; we'll let the auth listener update state.
        } catch (err) {
          console.error('Failed to check collection status:', err);
          // Assume collection exists on error to avoid blocking UI
          setHasCollection(true);
        }
      }
    };
    checkCollection();
  }, [isLoggedIn, hasCollection, setHasCollection]);

  // Keep a global CSS variable so Header (a sibling) can read the sidebar offset
  useEffect(() => {
    if (!isLoggedIn || hasCollection === false) {
      document.documentElement.style.setProperty('--sidebar-offset', '0px');
      return;
    }
    const offset = isExpanded ? '240px' : '64px';
    document.documentElement.style.setProperty('--sidebar-offset', offset);
  }, [isExpanded, isLoggedIn, hasCollection]);

  // Don't show sidebar if not logged in or collection is empty
  if (!isLoggedIn || hasCollection === false) return null;

  const navigationItems: NavigationItem[] = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Collection', href: '/collection', icon: Music },
    { name: 'Kollections', href: '/kollections', icon: FolderOpen },
    { name: 'Lists', href: '/lists', icon: List },
    { name: 'Add Music', href: '/add', icon: PlusCircle },
    { name: 'Artists', href: '/artists', icon: User },
    { name: 'Genres', href: '/genres', icon: List },
    { name: 'Statistics', href: '/statistics', icon: BarChart3 },
  ];

  const settingsItems: NavigationItem[] = [
    { name: 'Settings', href: '/settings', icon: Settings },
    { name: 'Profile', href: '/profile', icon: UserCircle },
  ];

  const isActiveLink = (href: string) => {
    if (href === '/') {
      return pathname === href;
    }
    return pathname.startsWith(href);
  };

  const cssVar = isExpanded ? '240px' : '64px';
  const inlineStyle = ({ ['--sidebar-offset']: cssVar } as unknown) as React.CSSProperties;

  return (
    <aside
      // reflect the current sidebar offset as a CSS variable used by Header
      style={inlineStyle}
      className={`sidebar ${isExpanded ? 'sidebar-expanded' : 'sidebar-collapsed'} bg-[#0A0A10] text-white flex flex-col shadow-2xl z-50 transition-all duration-300 ease-in-out`}
    >
      {/* Toggle Button */}
      <div className="p-4 flex justify-center border-b border-[#1C1C28]">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="sidebar-icon text-2xl hover:text-[#8B5CF6] focus:outline-none transition-colors"
        >
          <Menu size={24} />
        </button>
      </div>

      {/* Main Navigation */}
      <nav className="flex-1 py-6 overflow-y-auto overflow-x-hidden">
        <ul className="space-y-2">
          {navigationItems.map((item) => {
            const Icon = item.icon;
            const isActive = isActiveLink(item.href);
            
            return (
              <li key={item.name} className="relative group">
                <Link
                  href={item.href}
                  className={`flex items-center px-4 py-3 transition-colors ${
                    isActive
                      ? 'bg-[#8B5CF6]'
                      : 'hover:bg-[#1C1C28]'
                  }`}
                >
                  <Icon className="sidebar-icon w-6 h-6 min-w-6 text-center" />
                  <span
                    className={`ml-4 sidebar-text whitespace-nowrap overflow-hidden transition-opacity duration-300 ${
                      isExpanded ? 'opacity-100' : 'opacity-0'
                    }`}
                  >
                    {item.name}
                  </span>
                </Link>
                {!isExpanded && (
                  <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-[#1C1C28] text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
                    {item.name}
                  </div>
                )}
              </li>
            );
          })}

          {/* Random Album quick action */}
          <li className="relative group">
            <button
              onClick={async () => {
                try {
                  const id = await getRandomReleaseId();
                  if (id) {
                    router.push(`/releases/${id}`);
                  }
                } catch (err) {
                  console.error('Failed to get random release id', err);
                }
              }}
              className={`flex items-center w-full text-left px-4 py-3 transition-colors hover:bg-[#1C1C28]`}
            >
              <Shuffle className="sidebar-icon w-6 h-6 min-w-6 text-center" />
              <span
                className={`ml-4 sidebar-text whitespace-nowrap overflow-hidden transition-opacity duration-300 ${
                  isExpanded ? 'opacity-100' : 'opacity-0'
                }`}
              >
                Random Album
              </span>
            </button>
            {!isExpanded && (
              <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-[#1C1C28] text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
                Random Album
              </div>
            )}
          </li>
        </ul>
      </nav>

      {/* Settings Section */}
      <div className="border-t border-[#1C1C28] py-4">
        <ul className="space-y-2">
          {settingsItems.map((item) => {
            const Icon = item.icon;
            const isActive = isActiveLink(item.href);
            
            return (
              <li key={item.name} className="relative group">
                <Link
                  href={item.href}
                  className={`flex items-center px-4 py-3 transition-colors ${
                    isActive
                      ? 'bg-[#8B5CF6]'
                      : 'hover:bg-[#1C1C28]'
                  }`}
                >
                  <Icon className="sidebar-icon w-6 h-6 min-w-6 text-center" />
                  <span
                    className={`ml-4 sidebar-text whitespace-nowrap overflow-hidden transition-opacity duration-300 ${
                      isExpanded ? 'opacity-100' : 'opacity-0'
                    }`}
                  >
                    {item.name}
                  </span>
                </Link>
                {!isExpanded && (
                  <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-[#1C1C28] text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
                    {item.name}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      </div>
    </aside>
  );
};

export default Sidebar;
