import React from 'react';
import DbConnectionStatus from './DbConnectionStatus';

const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="bg-gray-50 border-t mt-auto">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          {/* Left side - App info */}
          <div className="flex flex-col md:flex-row items-center space-y-2 md:space-y-0 md:space-x-6">
            <div className="text-sm text-gray-600">
              © {currentYear} Kollector Sküm. Music Collection Manager.
            </div>
            <div className="text-xs text-gray-500">
              Built with Next.js & .NET Core API
            </div>
          </div>

          {/* Right side - Links */}
          <div className="flex items-center space-x-6">
            <a 
              href="/about" 
              className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
            >
              About
            </a>
            <a 
              href="/api/health" 
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
            >
              API Status
            </a>
            <DbConnectionStatus />
            <a 
              href="/swagger" 
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-blue-600 hover:text-blue-800 transition-colors font-medium"
            >
              API Docs
            </a>
          </div>
        </div>

        {/* Stats section */}
        <div className="mt-4 pt-4 border-t border-gray-200">
          <div className="text-xs text-gray-500 text-center">
            Phase 5 - Frontend Core Components | Backend API Ready
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
