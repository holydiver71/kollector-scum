'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { 
  Home, 
  Music, 
  PlusCircle, 
  User, 
  List, 
  BarChart3, 
  FileUp, 
  FileDown, 
  Settings, 
  UserCircle,
  Menu,
  FolderOpen
} from 'lucide-react';

interface NavigationItem {
  name: string;
  href: string;
  icon: React.ElementType;
}

const Sidebar: React.FC = () => {
  const [isExpanded, setIsExpanded] = useState(false);
    // Keep a global CSS variable so Header (a sibling) can read the sidebar offset
    React.useEffect(() => {
      const offset = isExpanded ? '240px' : '64px';
      document.documentElement.style.setProperty('--sidebar-offset', offset);
      return () => {
        // preserve last known value (do not remove) - tests may expect a value
      };
    }, [isExpanded]);
  const pathname = usePathname();

  const navigationItems: NavigationItem[] = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Collection', href: '/collection', icon: Music },
    { name: 'Kollections', href: '/kollections', icon: FolderOpen },
    { name: 'Add Music', href: '/add', icon: PlusCircle },
    { name: 'Artists', href: '/artists', icon: User },
    { name: 'Genres', href: '/genres', icon: List },
    { name: 'Statistics', href: '/statistics', icon: BarChart3 },
  ];

  const bottomItems: NavigationItem[] = [
    { name: 'Import', href: '/import', icon: FileUp },
    { name: 'Export', href: '/export', icon: FileDown },
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

  return (
    <aside
      // reflect the current sidebar offset as a CSS variable used by Header
      style={{
        // keep a stable CSS variable for header offset - used by Header
        // when collapsed -> 64px, when expanded -> 240px
        ['--sidebar-offset' as any]: isExpanded ? '240px' : '64px',
      }}
      className={`sidebar ${isExpanded ? 'sidebar-expanded' : 'sidebar-collapsed'} bg-gray-900 text-white flex flex-col shadow-2xl z-50 transition-all duration-300 ease-in-out`}
    >
      {/* Toggle Button */}
      <div className="p-4 flex justify-center border-b border-gray-800">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="sidebar-icon text-2xl hover:text-orange-400 focus:outline-none transition-colors"
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
                      ? 'bg-[#D93611]'
                      : 'hover:bg-gray-800'
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
                  <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-gray-800 text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
                    {item.name}
                  </div>
                )}
              </li>
            );
          })}

          {/* Divider */}
          <li className="my-4 border-t border-gray-800"></li>

          {/* Bottom Navigation Items */}
          {bottomItems.map((item) => {
            const Icon = item.icon;
            const isActive = isActiveLink(item.href);
            
            return (
              <li key={item.name} className="relative group">
                <Link
                  href={item.href}
                  className={`flex items-center px-4 py-3 transition-colors ${
                    isActive
                      ? 'bg-[#D93611]'
                      : 'hover:bg-gray-800'
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
                  <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-gray-800 text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
                    {item.name}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Settings Section */}
      <div className="border-t border-gray-800 py-4">
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
                      ? 'bg-[#D93611]'
                      : 'hover:bg-gray-800'
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
                  <div className="tooltip absolute left-full ml-2 top-1/2 -translate-y-1/2 bg-gray-800 text-white px-3 py-1 rounded text-sm whitespace-nowrap pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity">
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
