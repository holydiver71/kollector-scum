"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { LoadingSpinner, Skeleton } from "./components/LoadingComponents";
import { ErrorMessage } from "./components/ErrorBoundary";
import { getHealth, getPagedCount, API_BASE_URL, ApiError } from "./lib/api";

// Data contracts
interface HealthData { status: string; timestamp: string; service: string; version: string; }
interface CollectionStats { totalReleases: number; totalArtists: number; totalGenres: number; totalLabels: number; }

export default function Dashboard() {
  const [health, setHealth] = useState<HealthData | null>(null);
  const [stats, setStats] = useState<CollectionStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAll = async () => {
      try {
        setLoading(true);
        setError(null);
        const healthJson = await getHealth();
        setHealth(healthJson);

        const [totalReleases, totalArtists, totalGenres, totalLabels] = await Promise.all([
          getPagedCount('/api/musicreleases'),
          getPagedCount('/api/artists'),
          getPagedCount('/api/genres'),
          getPagedCount('/api/labels')
        ]);

        setStats({ totalReleases, totalArtists, totalGenres, totalLabels });
      } catch (e) {
        console.error(e);
        if ((e as ApiError)?.url) {
          setError(`${(e as Error).message} -> ${(e as ApiError).url}`);
        } else {
          setError(e instanceof Error ? e.message : 'Unknown error');
        }
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, []);

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-black via-gray-950 to-red-950 flex items-center justify-center p-6">
        <div className="max-w-md w-full text-center bg-black/70 backdrop-blur border border-red-700 rounded-xl p-8 shadow-2xl shadow-red-900/50">
          <div className="text-6xl mb-4">💀</div>
          <h1 className="text-red-400 font-black tracking-widest text-2xl mb-2">SYSTEM ERROR</h1>
          <p className="text-gray-300 text-sm mb-6">{error}</p>
          <button
            onClick={() => location.reload()}
            className="px-6 py-3 font-bold tracking-wide rounded-md bg-gradient-to-r from-red-700 to-red-900 hover:from-red-600 hover:to-red-800 text-white shadow-lg shadow-red-900/40 transition-transform hover:scale-105"
          >RELOAD SKÜM</button>
        </div>
      </div>
    );
  }

  const statCards = [
    { key: "releases", label: "RELEASES", value: stats?.totalReleases || 0, gradient: "from-red-700 to-red-900", icon: "🎵" },
    { key: "artists", label: "ARTISTS", value: stats?.totalArtists || 0, gradient: "from-orange-600 to-red-800", icon: "🤘" },
    { key: "genres", label: "GENRES", value: stats?.totalGenres || 0, gradient: "from-yellow-600 to-orange-700", icon: "⚡" },
    { key: "labels", label: "LABELS", value: stats?.totalLabels || 0, gradient: "from-red-950 to-black", icon: "🔥" }
  ];

  const actions = [
    { title: "BROWSE COLLECTION", href: "/collection", desc: "Explore your metal archives", icon: "📻", gradient: "from-gray-800 to-black" },
    { title: "SEEK & DESTROY", href: "/search", desc: "Find specific releases", icon: "🔍", gradient: "from-red-900 to-black" },
    { title: "ADD TO SKÜM", href: "/add", desc: "Expand your collection", icon: "➕", gradient: "from-orange-900 to-red-900" },
    { title: "METAL GENRES", href: "/genres", desc: "Browse by style", icon: "⚡", gradient: "from-yellow-900 to-orange-900" },
    { title: "METAL GODS", href: "/artists", desc: "Browse artists", icon: "🤘", gradient: "from-red-950 to-black" },
  { title: "SKÜM SETTINGS", href: "/settings", desc: "Configure collection", icon: "⚙️", gradient: "from-gray-900 to-black" }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-black via-gray-950 to-red-950 text-gray-100">
      {/* Hero / Banner */}
      <section className="relative overflow-hidden border-b border-red-800/50">
        <div className="absolute inset-0 bg-[url('/images/blood-splatter.png')] bg-cover opacity-10 pointer-events-none" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(185,28,28,0.25),transparent_70%)]" />
        <div className="max-w-7xl mx-auto px-4 py-14 relative">
          <h1
            aria-label="Kollector Sküm"
            className="text-5xl md:text-7xl tracking-wider text-transparent bg-clip-text bg-gradient-to-r from-red-500 via-orange-400 to-red-600 drop-shadow-[0_0_8px_rgba(220,38,38,0.45)] -skew-x-6 font-metal select-none"
          >
            <span aria-hidden="true">KOLLECTOR SKÜM</span>
          </h1>
          <p className="mt-4 text-sm md:text-base tracking-widest text-gray-300 font-semibold">
            CURATING THE SONIC ARMORY
          </p>
          <div className="mt-6 flex items-center gap-4">
            <div className="flex items-center text-xs font-mono tracking-wider">
              <span className="mr-2 text-gray-400">API:</span>
              {health?.status === "Healthy" ? (
                <span className="flex items-center text-green-400 font-bold">
                  <span className="w-2.5 h-2.5 rounded-full bg-green-500 animate-pulse mr-2" />
                  LIVE & BRUTAL
                </span>
              ) : (
                <span className="text-red-400 font-bold">OFFLINE</span>
              )}
            </div>
            {loading && <LoadingSpinner />}
          </div>
        </div>
      </section>

      <main className="max-w-7xl mx-auto px-4 py-12">
        {/* Stats */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-14">
          {loading ? (
            [...Array(4)].map((_, i) => (
              <div key={i} className="rounded-xl border border-red-900/40 bg-black/40 p-6">
                <Skeleton lines={4} />
              </div>
            ))
          ) : (
            statCards.map(card => (
              <div
                key={card.key}
                className={`group relative rounded-xl border-2 border-gray-900 bg-gradient-to-br ${card.gradient} p-6 shadow-xl shadow-black/50 overflow-hidden transition-all hover:scale-[1.03] hover:border-red-600`}
              >
                <div className="absolute -top-6 -right-6 w-24 h-24 bg-red-800/20 rounded-full blur-2xl group-hover:bg-red-600/30 transition-colors" />
                <div className="relative text-center">
                  <div className="text-4xl mb-2 drop-shadow">{card.icon}</div>
                  <div className="text-3xl font-black tracking-wider">{card.value.toLocaleString()}</div>
                  <div className="mt-1 text-xs font-semibold tracking-[0.35em] text-gray-200">
                    {card.label}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Quick Actions */}
        <h2 className="text-xl font-black tracking-wider text-red-400 mb-6 flex items-center gap-3">
          <span className="text-2xl">⚔️</span> OPERATIONS CONSOLE
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-16">
          {actions.map(a => (
            <Link
              key={a.title}
              href={a.href}
              className={`group relative rounded-xl border border-gray-800 bg-gradient-to-br ${a.gradient} p-6 overflow-hidden shadow-lg shadow-black/40 transition-all hover:shadow-red-900/40 hover:border-red-600 hover:scale-[1.03]`}
            >
              <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity bg-[radial-gradient(circle_at_center,rgba(255,0,0,0.15),transparent_70%)]" />
              <div className="relative text-center">
                <div className="text-4xl mb-3 group-hover:animate-pulse">{a.icon}</div>
                <h3 className="font-black tracking-wider text-sm text-gray-100 group-hover:text-red-300 transition-colors">
                  {a.title}
                </h3>
                <p className="mt-2 text-xs text-gray-300 tracking-wide">{a.desc}</p>
              </div>
            </Link>
          ))}
        </div>

        {/* Recent Activity placeholder */}
        <div className="rounded-xl border border-red-900/40 bg-black/50 p-8 backdrop-blur shadow-lg shadow-black/50">
          <h3 className="font-black tracking-wider text-sm text-red-300 mb-4 flex items-center gap-2">
            <span className="text-lg">🩸</span> RECENT ACTIVITY
          </h3>
          <div className="text-center py-10 text-gray-400">
            <div className="text-5xl mb-4">⏱️</div>
            <p className="font-semibold tracking-wide">Activity tracking coming soon</p>
            <p className="text-xs mt-2 text-gray-500">This module will chronicle your latest Sküm operations.</p>
          </div>
        </div>

        {/* System Info */}
        <div className="mt-14 text-center">
          <div className="inline-block px-6 py-4 rounded-lg border border-red-800/60 bg-black/60 backdrop-blur-sm shadow-inner shadow-red-900/20">
            <p className="text-[10px] tracking-[0.35em] font-semibold text-gray-400">
              POWERED BY <span className="text-red-400">{health?.service}</span> v{health?.version}
            </p>
            <p className="text-[10px] mt-2 tracking-widest text-gray-500">
              LAST SYNC: {health?.timestamp ? new Date(health.timestamp).toLocaleString() : "UNKNOWN"}
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}
