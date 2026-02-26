"use client";
import { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { LoadingSpinner } from "./components/LoadingComponents";
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleDismissWelcome = () => {
    setShowWelcome(false);
  };

  const handleStartFresh = () => {
    // Mark that user has chosen to start fresh (allow access to app)
    setHasCollection(true);
  };

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

  if (!isLoggedIn && !loading) {
    return (
      <div className="min-h-screen bg-[#0A0A10] flex flex-col items-center justify-center p-4">
        <div className="text-center max-w-2xl">
          <h1 className="text-6xl font-black text-white mb-6">KOLLECTOR SKÜM</h1>
          <p className="text-xl text-gray-400 mb-8">
            Your personal music collection manager.
            <br/>
            Organize, discover, and track your physical media.
          </p>
          <div className="p-8 bg-[#13131F] rounded-2xl border border-[#1C1C28]">
            <p className="text-lg font-medium text-white mb-4">Please sign in to access your collection</p>
            <p className="text-sm text-gray-500">Use the Google Sign-In button in the top right corner.</p>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-[#0A0A10] flex items-center justify-center p-6">
        <div className="max-w-md w-full text-center bg-[#13131F] border border-[#1C1C28] rounded-2xl p-8">
          <div className="text-6xl mb-4">⚠️</div>
          <h1 className="text-white font-semibold text-xl mb-2">Connection Error</h1>
          <p className="text-gray-400 text-sm mb-6">{error}</p>
          <button
            onClick={() => location.reload()}
            className="px-6 py-2 font-medium rounded-xl bg-[#8B5CF6] hover:bg-[#7C3AED] text-white transition-colors"
          >Reload SKÜM</button>
        </div>
      </div>
    );
  }

  // Show welcome screen for new users with empty collections
  if (showWelcome && !loading) {
    return <WelcomeScreen onDismiss={handleDismissWelcome} onStartFresh={handleStartFresh} />;
  }

  return (
    <div className="min-h-screen bg-transparent text-white">
      {/* Page title section */}
      <div className="max-w-7xl mx-auto px-6 pt-8 pb-6">
        <div className="flex items-start justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-3xl font-black tracking-tight text-white">Your Collection</h1>
            <p className="text-gray-400 mt-1 text-sm">Organise and discover your music library</p>
            <div className="flex items-center gap-2 mt-3">
              {health?.status === "Healthy" ? (
                <>
                  <span className="w-2 h-2 rounded-full bg-emerald-400" />
                  <span className="text-xs text-emerald-400 font-semibold">Online</span>
                </>
              ) : (
                <>
                  <span className="w-2 h-2 rounded-full bg-red-400" />
                  <span className="text-xs text-red-400 font-semibold">Offline</span>
                </>
              )}
              {loading && <LoadingSpinner />}
            </div>
          </div>
          <div className="text-right text-xs text-gray-600">
            Powered by Kollector API · Last sync: {health?.timestamp ? new Date(health.timestamp).toLocaleString() : "Unknown"}
          </div>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-6 pb-12 space-y-10">
        {/* Stats */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {loading ? (
            [...Array(4)].map((_, i) => (
              <div key={i} className="bg-[#13131F] rounded-2xl p-5 border border-[#1C1C28]">
                <div className="h-8 w-16 bg-[#1C1C28] animate-pulse rounded mb-2" />
                <div className="h-3 w-20 bg-[#1C1C28] animate-pulse rounded" />
              </div>
            ))
          ) : (
            statCards.map((card, i) => {
              const colors = ["#8B5CF6", "#06B6D4", "#10B981", "#F59E0B"];
              const color = colors[i % colors.length];
              return (
                <div key={card.key} className="bg-[#13131F] rounded-2xl p-5 border border-[#1C1C28] relative overflow-hidden">
                  <div className="absolute -top-4 -right-4 w-20 h-20 rounded-full opacity-10" style={{ background: color, filter: "blur(20px)" }} />
                  <div className="text-3xl font-black mb-1" style={{ color }}>{card.value.toLocaleString()}</div>
                  <div className="text-xs text-gray-400 font-medium uppercase tracking-wider">{card.label}</div>
                </div>
              );
            })
          )}
        </div>

        {/* Quick Actions */}
        <div>
          <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4">Quick Actions</h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
            {actions.map(a => (
              <Link
                key={a.title}
                href={a.href}
                className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] hover:border-[#8B5CF6]/40 cursor-pointer group transition-all"
              >
                <div className="text-2xl mb-2">{a.icon}</div>
                <div className="text-sm font-semibold text-white group-hover:text-[#8B5CF6] transition-colors">{a.title}</div>
                <div className="text-xs text-gray-500 mt-1">{a.desc}</div>
              </Link>
            ))}
          </div>
        </div>

        {/* Recently Played */}
        <div>
          <RecentlyPlayed maxItems={24} />
        </div>

        {/* Recent Activity placeholder */}
        <div className="bg-[#13131F] rounded-2xl p-6 border border-[#1C1C28] text-center">
          <div className="text-4xl mb-3">⏱️</div>
          <p className="font-semibold text-gray-400">Activity tracking coming soon</p>
          <p className="text-sm text-gray-600 mt-1">View your recent collection updates and changes here.</p>
        </div>
      </main>
    </div>
  );
}
