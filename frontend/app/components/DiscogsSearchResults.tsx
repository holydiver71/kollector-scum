"use client";

import Image from "next/image";
import { useState } from "react";
import type { DiscogsSearchResult, DiscogsRelease } from "../lib/discogs-types";
import { toDiscogsProxyUrl, getDiscogsRelease } from "../lib/api";

interface DiscogsSearchResultsProps {
  results: DiscogsSearchResult[];
  onSelectResult: (result: DiscogsSearchResult) => void;
}

export default function DiscogsSearchResults({
  results,
  onSelectResult,
}: DiscogsSearchResultsProps) {
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [expandedData, setExpandedData] = useState<Record<number, DiscogsRelease | null>>({});
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [errorId, setErrorId] = useState<Record<number, string>>({});

  if (results.length === 0) {
    return null;
  }

  const handleToggleExpand = async (result: DiscogsSearchResult) => {
    const id = Number(result.id);
    // if already expanded, collapse
    if (expandedId === id) {
      setExpandedId(null);
      return;
    }

    setExpandedId(id);

    // If we already fetched data, reuse it
    if (expandedData[id]) return;

    try {
      setLoadingId(id);
      setErrorId((s) => ({ ...s, [id]: "" }));
      const data = await getDiscogsRelease(id);
      setExpandedData((s) => ({ ...s, [id]: data }));
    } catch (err: any) {
      setErrorId((s) => ({ ...s, [id]: err?.message ?? "Failed to load details" }));
    } finally {
      setLoadingId((s) => (s === id ? null : s));
    }
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 mb-6">
      <h2 className="text-xl font-semibold mb-4">
        Search Results ({results.length} {results.length === 1 ? "match" : "matches"})
      </h2>

      <div className="space-y-4">
        {results.map((result) => (
          <div key={result.id}>
            <div
              className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 hover:shadow-md transition-all"
            >
            <div className="flex gap-4">
              {/* Album Cover */}
              <div className="flex-shrink-0">
                {result.thumbUrl || result.coverImageUrl ? (
                  <div className="relative w-24 h-24 bg-gray-100 rounded overflow-hidden">
                    <Image
                      src={toDiscogsProxyUrl(result.thumbUrl || result.coverImageUrl) ?? ""}
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

          {/* Inline expandable details (purple box) */}
          {expandedId === Number(result.id) && (
            <div className="mt-3 p-4 rounded-lg border border-purple-700 bg-gradient-to-br from-purple-900/6 to-purple-800/4 text-white">
              {loadingId === Number(result.id) && (
                <div className="py-6 text-center text-sm text-gray-300">Loading details…</div>
              )}

              {errorId[Number(result.id)] && (
                <div className="py-4 text-sm text-red-300">{errorId[Number(result.id)]}</div>
              )}

              {expandedData[Number(result.id)] && (
                <div className="flex flex-col md:flex-row gap-4">
                  {/* Left: Tracklist */}
                  <div className="md:w-2/3 bg-transparent">
                    <h4 className="text-lg font-semibold text-purple-200 mb-2">Tracklist</h4>
                    <div className="bg-transparent rounded-md p-2 max-h-64 overflow-y-auto">
                      <table className="w-full text-sm table-fixed">
                        <thead>
                          <tr className="text-left text-purple-300 text-xs">
                            <th className="w-20">Pos</th>
                            <th>Title</th>
                            <th className="w-24">Duration</th>
                          </tr>
                        </thead>
                        <tbody>
                          {expandedData[Number(result.id)]!.tracklist.map((t, i) => (
                            <tr key={i} className="border-t border-purple-700/30">
                              <td className="py-2 text-purple-100 font-mono">{t.position}</td>
                              <td className="py-2 text-purple-100 truncate">{t.title}</td>
                              <td className="py-2 text-purple-200">{t.duration}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>

                  {/* Right: Formats + Notes */}
                  <div className="md:w-1/3 bg-transparent">
                    <h4 className="text-lg font-semibold text-purple-200 mb-2">Details</h4>
                    <div className="mb-4 text-sm text-purple-100">
                      {expandedData[Number(result.id)]!.formats.map((f, idx) => (
                        <div key={idx} className="mb-2">
                          <div className="font-medium">{f.name} {f.qty ? `×${f.qty}` : ""}</div>
                          {f.descriptions && f.descriptions.length > 0 && (
                            <div className="text-xs text-purple-300">{f.descriptions.join(", ")}</div>
                          )}
                        </div>
                      ))}
                    </div>

                    {expandedData[Number(result.id)]!.notes && (
                      <>
                        <h4 className="text-lg font-semibold text-purple-200 mb-2">Notes</h4>
                        <div className="text-sm text-purple-100 bg-purple-900/10 rounded p-2 max-h-40 overflow-y-auto whitespace-pre-wrap">
                          {expandedData[Number(result.id)]!.notes}
                        </div>
                      </>
                    )}
                  </div>
                </div>
              )}

              <div className="mt-3 flex justify-end">
                <button
                  className="px-3 py-1 text-sm text-purple-100 border border-purple-600 rounded hover:bg-purple-800/20"
                  onClick={() => handleToggleExpand(result)}
                >
                  {expandedId === Number(result.id) ? "Collapse" : "Expand"}
                </button>
              </div>
            </div>
          )}

          {/* Toggle expansion control when not expanded */}
          {expandedId !== Number(result.id) && (
            <div className="mt-2 flex justify-end">
              <button
                className="px-3 py-1 text-sm text-gray-700 border border-gray-300 rounded hover:bg-gray-50"
                onClick={() => handleToggleExpand(result)}
              >
                Expand Details
              </button>
            </div>
          )}

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
