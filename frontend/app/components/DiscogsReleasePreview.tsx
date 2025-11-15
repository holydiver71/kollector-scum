"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { getDiscogsRelease } from "../lib/api";
import type { DiscogsRelease, DiscogsSearchResult } from "../lib/discogs-types";

interface DiscogsReleasePreviewProps {
  searchResult: DiscogsSearchResult;
  onAddToCollection: (release: DiscogsRelease) => void;
  onEditManually: (release: DiscogsRelease) => void;
  onBack: () => void;
  existingArtists: string[];
  existingLabels: string[];
  existingGenres: string[];
  existingCountries: string[];
  existingFormats: string[];
}

export default function DiscogsReleasePreview({
  searchResult,
  onAddToCollection,
  onEditManually,
  onBack,
  existingArtists,
  existingLabels,
  existingGenres,
  existingCountries,
  existingFormats,
}: DiscogsReleasePreviewProps) {
  const [release, setRelease] = useState<DiscogsRelease | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newEntities, setNewEntities] = useState<{
    artists: string[];
    labels: string[];
    genres: string[];
    countries: string[];
    formats: string[];
  }>({
    artists: [],
    labels: [],
    genres: [],
    countries: [],
    formats: [],
  });

  useEffect(() => {
    const fetchReleaseDetails = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const data = await getDiscogsRelease(searchResult.id);
        setRelease(data);

        // Identify new entities that will be created
        const newArtistsList = data.artists
          .map((a) => a.name)
          .filter((name) => !existingArtists.some((ea) => ea.toLowerCase() === name.toLowerCase()));

        const newLabelsList = data.labels
          .map((l) => l.name)
          .filter((name) => !existingLabels.some((el) => el.toLowerCase() === name.toLowerCase()));

        const allGenres = [...data.genres, ...data.styles];
        const newGenresList = allGenres.filter(
          (name) => !existingGenres.some((eg) => eg.toLowerCase() === name.toLowerCase())
        );

        const newCountriesList = data.country && !existingCountries.some(
          (ec) => ec.toLowerCase() === data.country.toLowerCase()
        ) ? [data.country] : [];

        const newFormatsList = data.formats.filter(
          (name) => !existingFormats.some((ef) => ef.toLowerCase() === name.toLowerCase())
        );

        setNewEntities({
          artists: newArtistsList,
          labels: newLabelsList,
          genres: newGenresList,
          countries: newCountriesList,
          formats: newFormatsList,
        });
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to load release details";
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    };

    fetchReleaseDetails();
  }, [searchResult.id, existingArtists, existingLabels, existingGenres, existingCountries, existingFormats]);

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="flex items-center justify-center py-12">
          <svg className="animate-spin h-10 w-10 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span className="ml-3 text-lg text-gray-600">Loading release details...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex">
            <svg className="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-red-800">Error Loading Details</h3>
              <p className="mt-1 text-sm text-red-700">{error}</p>
            </div>
          </div>
        </div>
        <div className="mt-4 flex gap-3">
          <button onClick={onBack} className="px-4 py-2 border border-gray-300 rounded-md hover:bg-gray-50">
            Back to Results
          </button>
        </div>
      </div>
    );
  }

  if (!release) return null;

  const totalNewEntities =
    newEntities.artists.length +
    newEntities.labels.length +
    newEntities.genres.length +
    newEntities.countries.length +
    newEntities.formats.length;

  const primaryImage = release.images.find((img) => img.type === "primary") || release.images[0];

  return (
    <div className="bg-white rounded-lg shadow p-6 mb-6">
      {/* Header */}
      <div className="flex justify-between items-start mb-6">
        <div>
          <h2 className="text-2xl font-semibold mb-1">{release.title}</h2>
          <p className="text-lg text-gray-600">
            {release.artists.map((a) => a.name).join(", ")}
          </p>
        </div>
        <button
          onClick={onBack}
          className="text-gray-600 hover:text-gray-900"
          title="Back to results"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      {/* New Entities Alert */}
      {totalNewEntities > 0 && (
        <div className="mb-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex">
            <svg className="h-5 w-5 text-blue-400 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
            </svg>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-blue-800">New Entities Will Be Created</h3>
              <div className="mt-2 text-sm text-blue-700">
                <p className="mb-2">Adding this release will create {totalNewEntities} new {totalNewEntities === 1 ? "entry" : "entries"} in your database:</p>
                <ul className="list-disc list-inside space-y-1">
                  {newEntities.artists.length > 0 && (
                    <li>
                      <strong>{newEntities.artists.length}</strong> new artist{newEntities.artists.length > 1 ? "s" : ""}: {newEntities.artists.join(", ")}
                    </li>
                  )}
                  {newEntities.labels.length > 0 && (
                    <li>
                      <strong>{newEntities.labels.length}</strong> new label{newEntities.labels.length > 1 ? "s" : ""}: {newEntities.labels.join(", ")}
                    </li>
                  )}
                  {newEntities.genres.length > 0 && (
                    <li>
                      <strong>{newEntities.genres.length}</strong> new genre{newEntities.genres.length > 1 ? "s" : ""}: {newEntities.genres.join(", ")}
                    </li>
                  )}
                  {newEntities.countries.length > 0 && (
                    <li>
                      <strong>{newEntities.countries.length}</strong> new countr{newEntities.countries.length > 1 ? "ies" : "y"}: {newEntities.countries.join(", ")}
                    </li>
                  )}
                  {newEntities.formats.length > 0 && (
                    <li>
                      <strong>{newEntities.formats.length}</strong> new format{newEntities.formats.length > 1 ? "s" : ""}: {newEntities.formats.join(", ")}
                    </li>
                  )}
                </ul>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Album Cover */}
        <div className="md:col-span-1">
          {primaryImage ? (
            <div className="relative w-full aspect-square bg-gray-100 rounded-lg overflow-hidden">
              <Image
                src={primaryImage.uri}
                alt={`${release.title} cover`}
                fill
                sizes="(max-width: 768px) 100vw, 33vw"
                className="object-cover"
              />
            </div>
          ) : (
            <div className="w-full aspect-square bg-gray-100 rounded-lg flex items-center justify-center">
              <svg className="w-24 h-24 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3" />
              </svg>
            </div>
          )}
        </div>

        {/* Release Details */}
        <div className="md:col-span-2 space-y-4">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-500 font-medium">Year:</span>{" "}
              <span className="text-gray-900">{release.year}</span>
            </div>
            <div>
              <span className="text-gray-500 font-medium">Country:</span>{" "}
              <span className="text-gray-900">{release.country}</span>
              {newEntities.countries.includes(release.country) && (
                <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                  ✨ New
                </span>
              )}
            </div>
            <div className="col-span-2">
              <span className="text-gray-500 font-medium">Labels:</span>{" "}
              {release.labels.map((label, idx) => (
                <span key={idx} className="inline-flex items-center mr-2">
                  <span className="text-gray-900">{label.name}</span>
                  {label.catalogNumber && (
                    <span className="ml-1 text-gray-500 font-mono text-xs">({label.catalogNumber})</span>
                  )}
                  {newEntities.labels.includes(label.name) && (
                    <span className="ml-1 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                      ✨ New
                    </span>
                  )}
                </span>
              ))}
            </div>
            <div className="col-span-2">
              <span className="text-gray-500 font-medium">Format:</span>{" "}
              {release.formats.map((format, idx) => (
                <span key={idx} className="inline-flex items-center mr-2">
                  <span className="text-gray-900">{format}</span>
                  {newEntities.formats.includes(format) && (
                    <span className="ml-1 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                      ✨ New
                    </span>
                  )}
                </span>
              ))}
            </div>
            <div className="col-span-2">
              <span className="text-gray-500 font-medium">Genres:</span>{" "}
              {[...release.genres, ...release.styles].map((genre, idx) => (
                <span key={idx} className="inline-flex items-center mr-2">
                  <span className="text-gray-900">{genre}</span>
                  {newEntities.genres.includes(genre) && (
                    <span className="ml-1 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                      ✨ New
                    </span>
                  )}
                </span>
              ))}
            </div>
          </div>

          {/* Track Listing */}
          {release.tracklist && release.tracklist.length > 0 && (
            <div>
              <h3 className="text-lg font-semibold mb-2">Tracklist</h3>
              <div className="bg-gray-50 rounded-lg p-3 max-h-64 overflow-y-auto">
                <ol className="space-y-1 text-sm">
                  {release.tracklist.map((track, idx) => (
                    <li key={idx} className="flex justify-between">
                      <span>
                        <span className="text-gray-500 font-mono">{track.position}</span> {track.title}
                      </span>
                      {track.duration && <span className="text-gray-500">{track.duration}</span>}
                    </li>
                  ))}
                </ol>
              </div>
            </div>
          )}

          {/* Notes */}
          {release.notes && (
            <div>
              <h3 className="text-lg font-semibold mb-2">Notes</h3>
              <p className="text-sm text-gray-700 bg-gray-50 rounded-lg p-3">{release.notes}</p>
            </div>
          )}
        </div>
      </div>

      {/* Action Buttons */}
      <div className="mt-6 flex gap-3 justify-end">
        <button
          onClick={onBack}
          className="px-6 py-2 border border-gray-300 rounded-md hover:bg-gray-50 font-medium transition-colors"
        >
          Back to Results
        </button>
        <button
          onClick={() => onEditManually(release)}
          className="px-6 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 font-medium transition-colors"
        >
          Edit Before Adding
        </button>
        <button
          onClick={() => onAddToCollection(release)}
          className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium transition-colors"
        >
          Add to Collection
        </button>
      </div>
    </div>
  );
}
