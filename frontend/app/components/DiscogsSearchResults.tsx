"use client";

import Image from "next/image";
import type { DiscogsSearchResult } from "../lib/discogs-types";

interface DiscogsSearchResultsProps {
  results: DiscogsSearchResult[];
  onSelectResult: (result: DiscogsSearchResult) => void;
}

export default function DiscogsSearchResults({
  results,
  onSelectResult,
}: DiscogsSearchResultsProps) {
  if (results.length === 0) {
    return null;
  }

  return (
    <div className="bg-white rounded-lg shadow p-6 mb-6">
      <h2 className="text-xl font-semibold mb-4">
        Search Results ({results.length} {results.length === 1 ? "match" : "matches"})
      </h2>

      <div className="space-y-4">
        {results.map((result) => (
          <div
            key={result.id}
            className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 hover:shadow-md transition-all cursor-pointer"
            onClick={() => onSelectResult(result)}
          >
            <div className="flex gap-4">
              {/* Album Cover */}
              <div className="flex-shrink-0">
                {result.thumbUrl || result.coverImageUrl ? (
                  <div className="relative w-24 h-24 bg-gray-100 rounded overflow-hidden">
                    <Image
                      src={result.thumbUrl || result.coverImageUrl || ""}
                      alt={`${result.title} cover`}
                      fill
                      sizes="96px"
                      className="object-cover"
                      onError={(e) => {
                        // Hide image on error
                        e.currentTarget.style.display = "none";
                      }}
                    />
                  </div>
                ) : (
                  <div className="w-24 h-24 bg-gray-100 rounded flex items-center justify-center">
                    <svg
                      className="w-12 h-12 text-gray-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3"
                      />
                    </svg>
                  </div>
                )}
              </div>

              {/* Release Details */}
              <div className="flex-1 min-w-0">
                <h3 className="text-lg font-semibold text-gray-900 truncate">
                  {result.title}
                </h3>
                <p className="text-gray-600 truncate">{result.artist}</p>

                <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                  <div>
                    <span className="text-gray-500">Format:</span>{" "}
                    <span className="text-gray-900">{result.format}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Country:</span>{" "}
                    <span className="text-gray-900">{result.country}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Year:</span>{" "}
                    <span className="text-gray-900">{result.year}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Label:</span>{" "}
                    <span className="text-gray-900">{result.label}</span>
                  </div>
                  <div className="col-span-2">
                    <span className="text-gray-500">Cat#:</span>{" "}
                    <span className="text-gray-900 font-mono">
                      {result.catalogNumber}
                    </span>
                  </div>
                </div>
              </div>

              {/* View Details Button */}
              <div className="flex-shrink-0 flex items-center">
                <button
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium transition-colors"
                  onClick={(e) => {
                    e.stopPropagation();
                    onSelectResult(result);
                  }}
                >
                  View Details
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {results.length > 5 && (
        <p className="mt-4 text-sm text-gray-500 text-center">
          Showing all {results.length} results. Click on a release to view full details.
        </p>
      )}
    </div>
  );
}
