'use client';
import React, { useEffect, useState } from 'react';
import DbConnectionStatus from './DbConnectionStatus';
import { getHealth } from '../lib/api';

/**
 * Footer component displaying copyright, navigation links, DB status, and live API health indicator.
 */
const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();
  const [apiHealthy, setApiHealthy] = useState<boolean | null>(null);
  const [lastDeploy, setLastDeploy] = useState<string | null>(null);

  useEffect(() => {
    /** Fetch API health, update indicator, and capture deploy timestamp. */
    getHealth()
      .then((h) => {
        setApiHealthy(h?.status === 'Healthy');
        if (h?.timestamp) {
          setLastDeploy(new Date(h.timestamp).toLocaleString());
        }
      })
      .catch(() => setApiHealthy(false));
  }, []);

  return (
    <footer className="bg-gray-50 border-t mt-auto">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          {/* Left side - Copyright */}
          <div className="text-sm text-gray-600">
            © {currentYear} Kollector Sküm. Music Collection Manager.
          </div>

          {/* Right side - Links and status indicators */}
          <div className="flex items-center space-x-6">
            <a
              href="/about"
              className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
            >
              About
            </a>
            <span
              className="flex items-center gap-1.5 text-sm text-gray-600"
              data-testid="api-health-status"
              title={apiHealthy === null ? 'Checking API…' : apiHealthy ? 'API Online' : 'API Offline'}
            >
              <span
                className={`w-2.5 h-2.5 rounded-full ${
                  apiHealthy === null
                    ? 'bg-gray-400'
                    : apiHealthy
                    ? 'bg-green-500'
                    : 'bg-red-500'
                }`}
              />
              API
            </span>
            <DbConnectionStatus />
            {lastDeploy && (
              <span className="text-xs text-gray-400" data-testid="last-deploy">
                Last Deploy: {lastDeploy}
              </span>
            )}
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
