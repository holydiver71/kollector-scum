import React from 'react';
import Link from 'next/link';
import { RandomPickButton } from './RandomPickButton';

interface HeaderProps {
  title?: string;
}

const Header: React.FC<HeaderProps> = ({ title = "Kollector Sküm" }) => {
  return (
    <header className="bg-white shadow-md border-b">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo and Title */}
          <div className="flex items-center space-x-3">
            <div className="bg-blue-600 text-white p-2 rounded-lg">
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <h1 className="text-2xl font-black text-gray-900">{title}</h1>
          </div>

          {/* Navigation */}
          <nav className="hidden md:flex space-x-8 items-center">
            <Link 
              href="/" 
              className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-bold transition-colors"
            >
              Dashboard
            </Link>
            <Link 
              href="/collection" 
              className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-bold transition-colors"
            >
              Collection
            </Link>
            <Link 
              href="/search" 
              className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-bold transition-colors"
            >
              Search
            </Link>
            <Link 
              href="/statistics" 
              className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-bold transition-colors"
            >
              Statistics
            </Link>
            <RandomPickButton />
          </nav>

          {/* Mobile menu button */}
          <div className="md:hidden flex items-center space-x-2">
            <RandomPickButton />
            <button className="text-gray-600 hover:text-gray-900 p-2">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header;
