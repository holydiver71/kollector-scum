"use client";
import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { getKollection, removeReleaseFromKollection, KollectionDto } from "../../lib/api";
import { LoadingSpinner } from "../../components/LoadingComponents";
import { MusicReleaseCard } from "../../components/MusicReleaseList";
import { ArrowLeft, Trash2 } from "lucide-react";

export default function KollectionDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = parseInt(params.id as string);

  const [kollection, setKollection] = useState<KollectionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [removingReleaseId, setRemovingReleaseId] = useState<number | null>(null);

  useEffect(() => {
    loadKollection();
  }, [id]);

  const loadKollection = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getKollection(id);
      setKollection(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load kollection");
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveRelease = async (releaseId: number) => {
    try {
      await removeReleaseFromKollection(id, releaseId);
      setRemovingReleaseId(null);
      loadKollection();
    } catch (err) {
      console.error("Failed to remove release:", err);
      alert(err instanceof Error ? err.message : "Failed to remove release");
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !kollection) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-600 text-xl mb-4">Error loading kollection</div>
          <p className="text-gray-600 mb-4">{error || "Kollection not found"}</p>
          <button
            onClick={() => router.push("/kollections")}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Back to Kollections
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <div className="flex items-center gap-4 mb-4">
            <Link
              href="/kollections"
              className="text-gray-400 hover:text-gray-700 flex items-center gap-2 transition-colors"
            >
              <ArrowLeft className="w-5 h-5" />
              <span className="text-sm uppercase tracking-wider font-bold">Back</span>
            </Link>
          </div>
          <h1 className="text-2xl font-black text-gray-900">{kollection.name}</h1>
          <p className="text-gray-600 mt-1 font-medium">
            {kollection.releases.length} {kollection.releases.length === 1 ? 'release' : 'releases'}
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {kollection.releases.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
            <p className="text-gray-600">No releases in this kollection yet</p>
            <Link
              href="/collection"
              className="inline-block mt-4 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors font-semibold"
            >
              Browse Collection
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
            {kollection.releases.map((release) => (
              <div key={release.id} className="relative group">
                <MusicReleaseCard 
                  release={{
                    id: release.id,
                    title: release.title,
                    releaseYear: release.releaseYear || '',
                    origReleaseYear: release.origReleaseYear,
                    artistNames: release.artistNames,
                    genreNames: release.genreNames,
                    labelName: release.labelName,
                    countryName: release.countryName,
                    formatName: release.formatName,
                    coverImageUrl: release.coverImageUrl,
                    dateAdded: release.dateAdded
                  }} 
                />
                {/* Remove button overlay */}
                <button
                  onClick={() => setRemovingReleaseId(release.id)}
                  className="absolute top-2 left-2 z-10 p-2 bg-red-500 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-600"
                  title="Remove from kollection"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Remove Confirmation Dialog */}
      {removingReleaseId !== null && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
            <h2 className="text-xl font-bold text-gray-900 mb-4">Remove from Kollection?</h2>
            <p className="text-gray-600 mb-6">
              Are you sure you want to remove this release from the kollection?
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setRemovingReleaseId(null)}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => handleRemoveRelease(removingReleaseId)}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-semibold"
              >
                Remove
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
