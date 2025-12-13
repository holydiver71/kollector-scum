"use client";

import { GoogleSignIn } from "./GoogleSignIn";
import { type UserProfile } from "../lib/auth";

interface LandingPageProps {
  onSignIn: (profile: UserProfile) => void;
}

export function LandingPage({ onSignIn }: LandingPageProps) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 flex items-center justify-center p-6">
      <div className="max-w-2xl w-full">
        <div className="text-center mb-12">
          <h1 className="text-6xl font-black text-white mb-4">
            KOLLECTOR SKÜM
          </h1>
          <p className="text-2xl text-gray-300 font-medium mb-2">
            Your Personal Music Collection Manager
          </p>
          <p className="text-lg text-gray-400">
            Organize, track, and discover your music library
          </p>
        </div>

        <div className="bg-white/10 backdrop-blur-lg border border-white/20 rounded-2xl p-8 shadow-2xl">
          <div className="text-center mb-8">
            <h2 className="text-2xl font-bold text-white mb-4">
              Sign in to get started
            </h2>
            <p className="text-gray-300 mb-6">
              Access your personal music collection from anywhere
            </p>
          </div>

          <div className="flex justify-center">
            <GoogleSignIn onSignIn={onSignIn} />
          </div>

          <div className="mt-8 pt-8 border-t border-white/20">
            <h3 className="text-lg font-semibold text-white mb-4">Features</h3>
            <ul className="space-y-3 text-gray-300">
              <li className="flex items-start">
                <span className="text-orange-400 mr-2">•</span>
                <span>Catalog your entire music collection</span>
              </li>
              <li className="flex items-start">
                <span className="text-orange-400 mr-2">•</span>
                <span>Create custom collections and lists</span>
              </li>
              <li className="flex items-start">
                <span className="text-orange-400 mr-2">•</span>
                <span>Track statistics and listening history</span>
              </li>
              <li className="flex items-start">
                <span className="text-orange-400 mr-2">•</span>
                <span>Search and filter your music</span>
              </li>
              <li className="flex items-start">
                <span className="text-orange-400 mr-2">•</span>
                <span>Sync across all your devices</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-8 text-center text-gray-400 text-sm">
          <p>Your data is private and secure. Only you can access your collection.</p>
        </div>
      </div>
    </div>
  );
}
