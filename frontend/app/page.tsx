"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { LoadingSpinner } from "./components/LoadingComponents";
import { WelcomeScreen } from "./components/WelcomeScreen";
import { RecentlyPlayed } from "./components/RecentlyPlayed";
import { IntroPage } from "./components/IntroPage";
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

        // Validate profile first before querying collection data
        const profile = await getUserProfile();
        
        if (!profile) {
          setIsLoggedIn(false);
          setLoading(false);
          return;
        }

        // We have a valid user profile, they are indeed logged in
        setIsLoggedIn(true);

        // Now fetch stats in parallel
        const [healthJson, totalReleases, totalArtists, totalGenres, totalLabels] = await Promise.all([
          getHealth(),
          getPagedCount('/api/musicreleases'),
          getPagedCount('/api/artists'),
          getPagedCount('/api/genres'),
          getPagedCount('/api/labels')
        ]);

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

  // Memoize actions array (static content)

  if (loading) {
    return <IntroPage loading />;
  }

  if (!isLoggedIn) {
    return <IntroPage />;
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

  const QA = [
    { title: "Browse Collection", icon: "📻", desc: "Explore your music library", link: "/collection" },
    { title: "Search Music", icon: "🔍", desc: "Find specific releases", link: "/search" },
    { title: "Ask a Question", icon: "🔮", desc: "Natural language queries", link: "/query" },
    { title: "View Statistics", icon: "📊", desc: "Analyse your collection", link: "/statistics" },
    { title: "Add Release", icon: "➕", desc: "Add new music", link: "/add" },
    { title: "Genres", icon: "⚡", desc: "Browse by genre", link: "/genres" },
    { title: "Artists", icon: "👤", desc: "Browse artists", link: "/artists" },
  ];


  return (
    <div className="min-h-screen bg-transparent text-white">
      <main className="max-w-7xl mx-auto px-6 py-8">
        <div className="space-y-8">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { label: "Releases", value: stats?.totalReleases || 0, color: "#8B5CF6" },
              { label: "Artists", value: stats?.totalArtists || 0, color: "#06B6D4" },
              { label: "Genres", value: stats?.totalGenres || 0, color: "#10B981" },
              { label: "Labels", value: stats?.totalLabels || 0, color: "#F59E0B" },
            ].map((s) => (
              <div key={s.label} className="bg-[#13131F] rounded-2xl p-5 border border-[#1C1C28] relative overflow-hidden">
                <div className="absolute -top-4 -right-4 w-20 h-20 rounded-full opacity-10" style={{ background: s.color, filter: "blur(20px)" }} />
                <div className="text-3xl font-black mb-1" style={{ color: s.color }}>{s.value.toLocaleString()}</div>
                <div className="text-xs text-gray-400 font-medium uppercase tracking-wider">{s.label}</div>
              </div>
            ))}
          </div>

          <div>
            <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4">Quick Actions</h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
              {QA.map((a) => (
                <Link href={a.link} key={a.title} className="block">
                  <div className="h-full bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] hover:border-[#8B5CF6]/40 cursor-pointer group transition-all">
                    <div className="text-2xl mb-2">{a.icon}</div>
                    <div className="text-sm font-semibold text-white group-hover:text-[#8B5CF6] transition-colors">{a.title}</div>
                    <div className="text-xs text-gray-500 mt-1">{a.desc}</div>
                  </div>
                </Link>
              ))}
            </div>
          </div>

        <div>
          <RecentlyPlayed maxItems={24} />
        </div>

          <div className="bg-[#13131F] rounded-2xl p-6 border border-[#1C1C28] text-center">
            <div className="text-4xl mb-3">⏱️</div>
            <p className="font-semibold text-gray-400">Activity tracking coming soon</p>
            <p className="text-sm text-gray-600 mt-1">View your recent collection updates and changes here.</p>
          </div>
        </div>
      </main>
    </div>
  );
}
