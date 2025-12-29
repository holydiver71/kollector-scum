"use client";
import { useState } from "react";
import { DiscogsImportDialog } from "./DiscogsImportDialog";

interface WelcomeScreenProps {
  onDismiss: () => void;
  onStartFresh: () => void;
}

export function WelcomeScreen({ onDismiss, onStartFresh }: WelcomeScreenProps) {
  const [showImportDialog, setShowImportDialog] = useState(false);

  const handleStartEmpty = () => {
    // Enable the app for users starting fresh
    onStartFresh();
    onDismiss();
  };

  const handleImportFromDiscogs = () => {
    setShowImportDialog(true);
  };

  const handleImportComplete = () => {
    setShowImportDialog(false);
    // Enable app access and dismiss welcome screen after successful import
    onStartFresh();
    onDismiss();
  };

  return (
    <>
      <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 flex items-center justify-center p-4">
        <div className="max-w-4xl w-full">
          {/* Hero Section */}
          <div className="text-center mb-12">
            <h1 className="text-6xl md:text-7xl font-black text-gray-900 mb-4">
              KOLLECTOR SKÜM
            </h1>
            <p className="text-xl md:text-2xl text-gray-600 font-medium">
              Welcome to your music collection manager
            </p>
          </div>

          {/* Options */}
          <div className="grid md:grid-cols-2 gap-6 mb-8">
            {/* Import from Discogs */}
            <button
              onClick={handleImportFromDiscogs}
              className="group bg-white rounded-2xl border-2 border-gray-200 p-8 shadow-lg hover:shadow-xl hover:border-blue-400 transition-all text-left"
            >
              <div className="flex items-start gap-4">
                <div className="text-5xl group-hover:scale-110 transition-transform">
                  📀
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-black text-gray-900 mb-3 group-hover:text-blue-600 transition-colors">
                    Import from Discogs
                  </h2>
                  <p className="text-gray-600 font-medium mb-4">
                    Already have a collection on Discogs? Import it instantly and sync all your releases, including cover art and details.
                  </p>
                  <div className="flex items-center text-blue-600 font-bold text-sm">
                    <span>Get Started</span>
                    <span className="ml-2 group-hover:translate-x-1 transition-transform">→</span>
                  </div>
                </div>
              </div>
            </button>

            {/* Start Empty */}
            <button
              onClick={handleStartEmpty}
              className="group bg-white rounded-2xl border-2 border-gray-200 p-8 shadow-lg hover:shadow-xl hover:border-green-400 transition-all text-left"
            >
              <div className="flex items-start gap-4">
                <div className="text-5xl group-hover:scale-110 transition-transform">
                  ✨
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-black text-gray-900 mb-3 group-hover:text-green-600 transition-colors">
                    Start Fresh
                  </h2>
                  <p className="text-gray-600 font-medium mb-4">
                    Build your collection from scratch. Add releases manually or search the Discogs database as you go.
                  </p>
                  <div className="flex items-center text-green-600 font-bold text-sm">
                    <span>Start Empty</span>
                    <span className="ml-2 group-hover:translate-x-1 transition-transform">→</span>
                  </div>
                </div>
              </div>
            </button>
          </div>

          {/* Features Preview */}
          <div className="bg-white rounded-2xl border border-gray-200 p-8 shadow-md">
            <h3 className="text-lg font-black text-gray-900 mb-6 text-center">
              What you can do with Kollector Sküm
            </h3>
            <div className="grid sm:grid-cols-3 gap-6">
              <div className="text-center">
                <div className="text-3xl mb-3">🎵</div>
                <h4 className="font-bold text-gray-900 mb-2">Organize</h4>
                <p className="text-sm text-gray-600 font-medium">
                  Catalog your physical music collection with detailed metadata
                </p>
              </div>
              <div className="text-center">
                <div className="text-3xl mb-3">📊</div>
                <h4 className="font-bold text-gray-900 mb-2">Analyze</h4>
                <p className="text-sm text-gray-600 font-medium">
                  View statistics and insights about your collection
                </p>
              </div>
              <div className="text-center">
                <div className="text-3xl mb-3">🔍</div>
                <h4 className="font-bold text-gray-900 mb-2">Discover</h4>
                <p className="text-sm text-gray-600 font-medium">
                  Search and filter through your music library
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Import Dialog */}
      {showImportDialog && (
        <DiscogsImportDialog
          isOpen={showImportDialog}
          onClose={() => setShowImportDialog(false)}
          onSuccess={handleImportComplete}
        />
      )}
    </>
  );
}
