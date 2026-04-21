"use client";
export const runtime = 'edge';
import { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getList, fetchJson, type ListDto } from "../../lib/api";
import { LoadingSpinner } from "../../components/LoadingComponents";
import { MusicReleaseCard } from "../../components/MusicReleaseList";
import { ArrowLeft } from "lucide-react";

interface MusicRelease {
  id: number;
  title: string;
  releaseYear: string;
  origReleaseYear?: string;
  artistNames?: string[];
  genreNames?: string[];
  labelName?: string;
  countryName?: string;
  formatName?: string;
  coverImageUrl?: string;
  dateAdded: string;
}

export default function ListDetailPage() {
  const params = useParams();
  const id = params.id as string;

  const [list, setList] = useState<ListDto | null>(null);
  const [releases, setReleases] = useState<MusicRelease[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadListAndReleases = async () => {
      try {
        setLoading(true);
        setError(null);

        const listData = await getList(parseInt(id));
        setList(listData);

        // Fetch releases for the list
        if (listData.releaseIds.length > 0) {
          // Create a query string with all release IDs
          const idsParam = listData.releaseIds.join(',');
          const params = new URLSearchParams({
            ids: idsParam,
            "Pagination.PageSize": listData.releaseIds.length.toString(),
            "Pagination.PageNumber": "1"
          });
          
          const releasesData = await fetchJson<{ items: MusicRelease[] }>(`/api/musicreleases?${params.toString()}`);
          
          // Sort releases in the order they appear in the list
          const sortedReleases = listData.releaseIds
            .map(id => releasesData.items.find(r => r.id === id))
            .filter((r): r is MusicRelease => r !== undefined);
          
          setReleases(sortedReleases);
        } else {
          setReleases([]);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load list");
      } finally {
        setLoading(false);
      }
    };

    loadListAndReleases();
  }, [id]);

  if (loading) {
    return (
      <div className="min-h-screen bg-transparent flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !list) {
    return (
      <div className="min-h-screen bg-transparent flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-400 text-xl mb-4">
            {error || "List not found"}
          </div>
          <Link
            href="/lists"
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg btn-theme-accent transition-colors"
          >
            Back to Lists
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-transparent">
      {/* Header */}
      <div className="border-b" style={{ borderColor: 'var(--theme-card-border)' }}>
        <div className="max-w-7xl mx-auto px-8 py-6">
          <Link
            href="/lists"
            className="inline-flex items-center gap-2 mb-4 transition-colors hover:opacity-75"
            style={{ color: 'var(--theme-muted-text)' }}
          >
            <ArrowLeft className="w-4 h-4" />
            Back to Lists
          </Link>
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2" style={{ color: 'var(--theme-foreground)' }}>{list.name}</h1>
              <p style={{ color: 'var(--theme-muted-text)' }}>
                {releases.length} {releases.length === 1 ? "release" : "releases"}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        {releases.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-lg mb-4" style={{ color: 'var(--theme-muted-text)' }}>This list is empty.</p>
            <p className="mb-8" style={{ color: 'var(--theme-muted-text)', opacity: 0.7 }}>
              Add releases to this list from the collection or release detail pages.
            </p>
            <Link
              href="/collection"
              className="inline-flex items-center gap-2 px-6 py-3 rounded-lg btn-theme-accent font-semibold"
            >
              Browse Collection
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
            {releases.map((release) => (
              <MusicReleaseCard key={release.id} release={release} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
