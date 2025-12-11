"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
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
    loadListAndReleases();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

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
          pageSize: listData.releaseIds.length.toString()
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

  if (loading) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !list) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-600 text-xl mb-4">
            {error || "List not found"}
          </div>
          <Link
            href="/lists"
            className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] transition-colors"
          >
            Back to Lists
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <div className="border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-8 py-6">
          <Link
            href="/lists"
            className="inline-flex items-center gap-2 text-gray-600 hover:text-[#D93611] transition-colors mb-4"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to Lists
          </Link>
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 mb-2">{list.name}</h1>
              <p className="text-gray-600">
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
            <p className="text-gray-500 text-lg mb-4">This list is empty.</p>
            <p className="text-gray-400 mb-8">
              Add releases to this list from the collection or release detail pages.
            </p>
            <Link
              href="/collection"
              className="inline-flex items-center gap-2 px-6 py-3 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] transition-colors font-semibold"
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
