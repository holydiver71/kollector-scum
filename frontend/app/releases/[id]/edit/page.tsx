"use client";
export const runtime = 'edge';

import { useParams, useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import { fetchJson } from "../../../lib/api";
import { fromCreateDto } from "../../../components/wizard/types";
import type { CreateMusicReleaseDto } from "../../../components/AddReleaseForm";
import AddReleaseWizard from "../../../components/wizard/AddReleaseWizard";
import { LoadingSpinner } from "../../../components/LoadingComponents";

// Type definitions matching the release detail page
interface Artist {
  id: number;
  name: string;
}

interface Genre {
  id: number;
  name: string;
}

interface Label {
  id: number;
  name: string;
}

interface Country {
  id: number;
  name: string;
}

interface Format {
  id: number;
  name: string;
}

interface Packaging {
  id: number;
  name: string;
}

interface PurchaseInfo {
  storeId?: number;
  storeName?: string;
  price?: number;
  currency?: string;
  purchaseDate?: string;
  notes?: string;
}

interface ReleaseImages {
  coverFront?: string;
  coverBack?: string;
  thumbnail?: string;
}

interface ReleaseLink {
  url?: string;
  type?: string;
  description?: string;
}

interface Track {
  title: string;
  releaseYear?: string;
  artists: string[];
  genres: string[];
  live: boolean;
  lengthSecs?: number;
  index: number;
}

interface Media {
  name?: string;
  tracks?: Track[];
}

interface DetailedMusicRelease {
  id: number;
  title: string;
  releaseYear?: string;
  origReleaseYear?: string;
  artists?: Artist[];
  genres?: Genre[];
  live: boolean;
  label?: Label;
  country?: Country;
  labelNumber?: string;
  upc?: string;
  lengthInSeconds?: number;
  format?: Format;
  packaging?: Packaging;
  purchaseInfo?: PurchaseInfo;
  images?: ReleaseImages;
  links?: ReleaseLink[];
  media?: Media[];
  dateAdded: string;
  lastModified: string;
}

export default function EditReleasePage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;

  const [release, setRelease] = useState<DetailedMusicRelease | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRelease = async () => {
      try {
        setLoading(true);
        setError(null);
        const response: DetailedMusicRelease = await fetchJson(`/api/musicreleases/${id}`);
        setRelease(response);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load release details");
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchRelease();
    }
  }, [id]);

  const handleSuccess = (releaseId: number) => {
    // Navigate back to the release detail page after successful edit
    router.push(`/releases/${releaseId}`);
  };

  const handleCancel = () => {
    // Navigate back to the release detail page
    router.push(`/releases/${id}`);
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[var(--theme-body-bg-mid)] text-[var(--theme-foreground)] flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !release) {
    return (
      <div className="min-h-screen bg-[var(--theme-body-bg-mid)] text-[var(--theme-foreground)] flex items-center justify-center">
        <div className="text-center">
          <div className="text-[var(--theme-error-text)] text-xl mb-4">Error loading release</div>
          <p className="text-[var(--theme-muted-text)] mb-4">{error || "Release not found"}</p>
          <button
            onClick={() => router.back()}
            className="bg-[var(--theme-accent)] text-white px-4 py-2 rounded hover:bg-[var(--theme-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--theme-accent)]"
          >
            Go Back
          </button>
        </div>
      </div>
    );
  }

  // Build a CreateMusicReleaseDto from the fetched release.
  // Only IDs are sent for artists/genres/label/etc. so the API can look them up.
  // Names are NOT included in artistNames/genreNames because those fields mean
  // "create new entities" – they are only needed for the display layer below.
  const dto: Partial<CreateMusicReleaseDto> = {
    title: release.title,
    releaseYear: release.releaseYear,
    origReleaseYear: release.origReleaseYear,
    artistIds: release.artists?.map(a => a.id) ?? [],
    genreIds: release.genres?.map(g => g.id) ?? [],
    live: release.live,
    labelId: release.label?.id,
    labelName: release.label?.name,
    countryId: release.country?.id,
    countryName: release.country?.name,
    labelNumber: release.labelNumber,
    upc: release.upc,
    lengthInSeconds: release.lengthInSeconds,
    formatId: release.format?.id,
    formatName: release.format?.name,
    packagingId: release.packaging?.id,
    packagingName: release.packaging?.name,
    purchaseInfo: release.purchaseInfo,
    images: release.images,
    links: release.links?.map(link => ({
      url: link.url ?? "",
      type: link.type ?? "",
      description: link.description,
    })),
    media: release.media?.map(m => ({
      name: m.name,
      tracks: m.tracks?.map(t => ({
        title: t.title,
        index: t.index,
        lengthSecs: t.lengthSecs,
        artists: t.artists,
        genres: t.genres,
        live: t.live,
      })) ?? [],
    })),
  };

  // Convert the DTO to WizardFormData and override the display-only
  // artistDisplayNames so the DraftPreviewPanel shows correct names without
  // requiring the user to interact with the Basic Information step first.
  const initialFormData = fromCreateDto(dto);
  initialFormData.artistDisplayNames = release.artists?.map(a => a.name) ?? [];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-[var(--theme-foreground)]">Edit Release</h1>
        <p className="mt-2 text-[var(--theme-muted-text)]">
          Update the details for &quot;{release.title}&quot;
        </p>
      </div>

      <AddReleaseWizard
        prebuiltFormData={initialFormData}
        releaseId={parseInt(id)}
        onSuccess={handleSuccess}
        onCancel={handleCancel}
      />
    </div>
  );
}
