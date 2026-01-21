"use client";
import { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { LoadingSpinner, Skeleton } from "./components/LoadingComponents";
import { RecentlyPlayed } from "./components/RecentlyPlayed";
import { WelcomeScreen } from "./components/WelcomeScreen";
import { useCollection } from "./contexts/CollectionContext";

import { getHealth, getPagedCount, ApiError } from "./lib/api";
import { isAuthenticated, clearAuthToken, getUserProfile } from "./lib/auth";

// Data contracts
interface HealthData { status: string; timestamp: string; service: string; version: string; }
interface CollectionStats { totalReleases: number; totalArtists: number; totalGenres: number; totalLabels: number; }

export default function Dashboard() {
  const [health, setHealth] = useState<HealthData | null>(null);
  const [stats, setStats] = useState<CollectionStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [showWelcome, setShowWelcome] = useState(false);
  const { setHasCollection } = useCollection();

  useEffect(() => {
    const checkAuthAndFetch = async () => {
      const authenticated = isAuthenticated();
      // Do not immediately treat a token as a valid session.
      // First validate it against the backend to avoid flashing the dashboard for revoked users.
      if (!authenticated) {
        setIsLoggedIn(false);
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);

        // Parallelize all API calls for faster loading
        // Profile validation is still required but we can fetch health and stats concurrently
        const [profile, healthJson, totalReleases, totalArtists, totalGenres, totalLabels] = await Promise.all([
          getUserProfile(),
          getHealth(),
          getPagedCount('/api/musicreleases'),
          getPagedCount('/api/artists'),
          getPagedCount('/api/genres'),
          getPagedCount('/api/labels')
        ]);

        if (!profile) {
          setIsLoggedIn(false);
          setLoading(false);
          return;
        }

        setIsLoggedIn(true);
        setHealth(healthJson);
        setStats({ totalReleases, totalArtists, totalGenres, totalLabels });
        
        // Update collection context and show welcome screen for empty collections
        setHasCollection(totalReleases > 0);
        if (totalReleases === 0) {
          setShowWelcome(true);
        }
      } catch (e) {
        console.error(e);
        const apiError = e as ApiError;
        
        // If unauthorized, clear token and show landing page
        if (apiError?.status === 401) {
          clearAuthToken();
          setIsLoggedIn(false);
          setLoading(false);
          return;
        }

        if (apiError?.url) {
          setError(`${(e as Error).message} -> ${(e as ApiError).url}`);
        } else {
          setError(e instanceof Error ? e.message : 'Unknown error');
        }
      } finally {
        setLoading(false);
      }
    };
    
    checkAuthAndFetch();
    
    // Re-check authentication when token changes in localStorage
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'auth_token') {
        checkAuthAndFetch();
      }
    };
    
    window.addEventListener('storage', handleStorageChange);
    
    // Also listen for custom event fired after sign-in
    const handleAuthChange = () => {
      checkAuthAndFetch();
    };
    
    window.addEventListener('authChanged', handleAuthChange);
    
    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('authChanged', handleAuthChange);
    };
  }, []);

  const handleDismissWelcome = () => {
    setShowWelcome(false);
  };

  const handleStartFresh = () => {
    // Mark that user has chosen to start fresh (allow access to app)
    setHasCollection(true);
  };

  if (!isLoggedIn && !loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
        <div className="text-center max-w-2xl">
          <h1 className="text-6xl font-black text-gray-900 mb-6">KOLLECTOR SKÜM</h1>
          <p className="text-xl text-gray-600 mb-8">
            Your personal music collection manager.
            <br/>
            Organize, discover, and track your physical media.
          </p>
          <div className="p-8 bg-white rounded-xl shadow-lg border border-gray-200">
            <p className="text-lg font-medium text-gray-800 mb-4">Please sign in to access your collection</p>
            <p className="text-sm text-gray-500">Use the Google Sign-In button in the top right corner.</p>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-6">
        <div className="max-w-md w-full text-center bg-white border border-gray-200 rounded-lg p-8 shadow-lg">
          <div className="text-6xl mb-4">⚠️</div>
          <h1 className="text-gray-900 font-semibold text-xl mb-2">Connection Error</h1>
          <p className="text-gray-600 text-sm mb-6">{error}</p>
          <button
            onClick={() => location.reload()}
            className="px-6 py-2 font-medium rounded-md bg-blue-600 hover:bg-blue-700 text-white transition-colors"
          >Reload SKÜM</button>
        </div>
      </div>
    );
  }

  // Show welcome screen for new users with empty collections
  if (showWelcome && !loading) {
    return <WelcomeScreen onDismiss={handleDismissWelcome} onStartFresh={handleStartFresh} />;
  }

  // Memoize stat cards to prevent unnecessary recalculations
  const statCards = useMemo(() => [
    { key: "releases", label: "Releases", value: stats?.totalReleases || 0, color: "blue", icon: "🎵" },
    { key: "artists", label: "Artists", value: stats?.totalArtists || 0, color: "green", icon: "👤" },
    { key: "genres", label: "Genres", value: stats?.totalGenres || 0, color: "purple", icon: "🏷️" },
    { key: "labels", label: "Labels", value: stats?.totalLabels || 0, color: "orange", icon: "🏢" }
  ], [stats]);

  // Memoize actions array (static content)
  const actions = useMemo(() => [
    { title: "Browse Collection", href: "/collection", desc: "Explore your music library", icon: "📻", color: "gray" },
    { title: "Search Music", href: "/search", desc: "Find specific releases", icon: "🔍", color: "blue" },
    { title: "Ask a Question", href: "/query", desc: "Natural language queries", icon: "🔮", color: "purple" },
    { title: "View Statistics", href: "/statistics", desc: "Analyze your collection", icon: "📊", color: "green" },
    { title: "Add Release", href: "/add", desc: "Add new music to collection", icon: "➕", color: "green" },
    { title: "Genres", href: "/genres", desc: "Browse by genre", icon: "⚡", color: "purple" },
    { title: "Artists", href: "/artists", desc: "Browse artists", icon: "👤", color: "indigo" }
  ], []);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero / Banner */}
      <section className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-12">
          <h1 className="text-4xl md:text-5xl font-black text-gray-900 mb-2">
            KOLLECTOR SKÜM
          </h1>
          <p className="text-lg text-gray-600 mb-6 font-medium">
            Organise and discover your music library
          </p>
          <div className="flex items-center gap-4">
            <div className="flex items-center text-sm">
              <span className="mr-2 text-gray-500 font-semibold">Status:</span>
              {health?.status === "Healthy" ? (
                <span className="flex items-center text-green-600 font-medium">
                  <span className="w-2 h-2 rounded-full bg-green-500 mr-2" />
                  Online
                </span>
              ) : (
                <span className="flex items-center text-red-600 font-medium">
                  <span className="w-2 h-2 rounded-full bg-red-500 mr-2" />
                  Offline
                </span>
              )}
            </div>
            {loading && <LoadingSpinner />}
          </div>
        </div>
      </section>

      <main className="max-w-7xl mx-auto px-4 py-12">
        {/* Stats */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-12">
          {loading ? (
            [...Array(4)].map((_, i) => (
              <div key={i} className="rounded-lg border border-gray-200 bg-white p-6">
                <Skeleton lines={3} />
              </div>
            ))
          ) : (
            statCards.map(card => (
              <div
                key={card.key}
                className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm hover:shadow-md transition-shadow"
              >
                <div className="text-center">
                  <div className="text-3xl mb-3">{card.icon}</div>
                  <div className="text-2xl font-black text-gray-900 mb-1">{card.value.toLocaleString()}</div>
                  <div className="text-sm font-bold text-gray-600">
                    {card.label}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Quick Actions */}
        <h2 className="text-xl font-black text-gray-900 mb-6">
          Quick Actions
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-12">
          {actions.map(a => (
            <Link
              key={a.title}
              href={a.href}
              className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm hover:shadow-md transition-all hover:border-blue-300"
            >
              <div className="text-center">
                <div className="text-3xl mb-3">{a.icon}</div>
                <h3 className="font-bold text-gray-900 mb-2">
                  {a.title}
                </h3>
                <p className="text-sm text-gray-600 font-medium">{a.desc}</p>
              </div>
            </Link>
          ))}
        </div>

        {/* Recently Played */}
        <div className="mb-12">
          <RecentlyPlayed maxItems={24} />
        </div>

        {/* Recent Activity placeholder */}
        <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
          <h3 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
            <span className="text-xl">📈</span> Recent Activity
          </h3>
          <div className="text-center py-8 text-gray-500">
            <div className="text-4xl mb-4">⏱️</div>
            <p className="font-bold mb-2">Activity tracking coming soon</p>
            <p className="text-sm font-medium">View your recent collection updates and changes here.</p>
          </div>
        </div>

        {/* System Info */}
        <div className="mt-8 text-center">
          <div className="inline-block px-4 py-2 rounded border border-gray-200 bg-gray-50">
            <p className="text-xs text-gray-600 font-semibold">
              Powered by <span className="font-bold">{health?.service}</span> v{health?.version}
            </p>
            <p className="text-xs mt-1 text-gray-500 font-medium">
              Last sync: {health?.timestamp ? new Date(health.timestamp).toLocaleString() : "Unknown"}
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}
