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

  const statCards = [
    { key: "releases", label: "Releases", value: stats?.totalReleases || 0, color: "blue", icon: "🎵" },
    { key: "artists", label: "Artists", value: stats?.totalArtists || 0, color: "green", icon: "👤" },
    { key: "genres", label: "Genres", value: stats?.totalGenres || 0, color: "purple", icon: "🏷️" },
    { key: "labels", label: "Labels", value: stats?.totalLabels || 0, color: "orange", icon: "🏢" }
  ];

  const actions = [
    { title: "Browse Collection", href: "/collection", desc: "Explore your music library", icon: "📻", color: "gray" },
    { title: "Search Music", href: "/search", desc: "Find specific releases", icon: "🔍", color: "blue" },
    { title: "Add Release", href: "/add", desc: "Add new music to collection", icon: "➕", color: "green" },
    { title: "Genres", href: "/genres", desc: "Browse by genre", icon: "⚡", color: "purple" },
    { title: "Artists", href: "/artists", desc: "Browse artists", icon: "👤", color: "indigo" },
    { title: "Settings", href: "/settings", desc: "Configure application", icon: "⚙️", color: "gray" }
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero / Banner */}
      <section className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-12">
          <h1 className="text-4xl md:text-5xl font-bold text-gray-900 mb-2">
            KOLLECTOR SKÜM
          </h1>
          <p className="text-lg text-gray-600 mb-6">
            Organize and discover your music library
          </p>
          <div className="flex items-center gap-4">
            <div className="flex items-center text-sm">
              <span className="mr-2 text-gray-500">Status:</span>
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
                  <div className="text-2xl font-bold text-gray-900 mb-1">{card.value.toLocaleString()}</div>
                  <div className="text-sm font-medium text-gray-600">
                    {card.label}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Quick Actions */}
        <h2 className="text-xl font-semibold text-gray-900 mb-6">
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
                <h3 className="font-medium text-gray-900 mb-2">
                  {a.title}
                </h3>
                <p className="text-sm text-gray-600">{a.desc}</p>
              </div>
            </Link>
          ))}
        </div>

        {/* Recent Activity placeholder */}
        <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
          <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center gap-2">
            <span className="text-xl">📈</span> Recent Activity
          </h3>
          <div className="text-center py-8 text-gray-500">
            <div className="text-4xl mb-4">⏱️</div>
            <p className="font-medium mb-2">Activity tracking coming soon</p>
            <p className="text-sm">View your recent collection updates and changes here.</p>
          </div>
        </div>

        {/* System Info */}
        <div className="mt-8 text-center">
          <div className="inline-block px-4 py-2 rounded border border-gray-200 bg-gray-50">
            <p className="text-xs text-gray-600">
              Powered by <span className="font-medium">{health?.service}</span> v{health?.version}
            </p>
            <p className="text-xs mt-1 text-gray-500">
              Last sync: {health?.timestamp ? new Date(health.timestamp).toLocaleString() : "Unknown"}
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}
