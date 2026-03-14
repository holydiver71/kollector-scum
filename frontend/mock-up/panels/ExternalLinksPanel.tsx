"use client";

import type { MockFormData, MockLink, ValidationErrors } from "../types";
import { LINK_TYPES } from "../fixtures";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/**
 * Panel 9 – External Links (optional).
 * Allows multiple URL + type + description triples.
 */
export default function ExternalLinksPanel({ data, onChange, errors }: Props) {
  const links: MockLink[] = data.links ?? [];

  const updateLinks = (updated: MockLink[]) => onChange({ links: updated });

  const addLink = () => {
    updateLinks([...links, { url: "", type: "Discogs", description: "" }]);
  };

  const removeLink = (index: number) => {
    updateLinks(links.filter((_, i) => i !== index));
  };

  const updateLink = (index: number, patch: Partial<MockLink>) => {
    updateLinks(
      links.map((l, i) => (i === index ? { ...l, ...patch } : l))
    );
  };

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500">
        Link this release to external services such as Discogs, Spotify,
        MusicBrainz, YouTube and more. All fields are optional.
      </p>

      {/* Link entries */}
      <div className="space-y-4">
        {links.map((link, index) => (
          <div
            key={index}
            className="bg-[#0F0F1A] border border-[#2A2A3C] rounded-xl p-4 space-y-3"
          >
            {/* URL + Remove button */}
            <div className="flex gap-2 items-start">
              <div className="flex-1">
                <label
                  htmlFor={`mock-link-url-${index}`}
                  className="block text-xs font-semibold text-[#A78BFA]/60 uppercase tracking-wider mb-1.5"
                >
                  URL
                </label>
                <input
                  id={`mock-link-url-${index}`}
                  type="url"
                  value={link.url}
                  onChange={(e) => updateLink(index, { url: e.target.value })}
                  placeholder="https://www.discogs.com/…"
                  className={`w-full bg-[#0A0A12] border rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-700 font-mono focus:outline-none focus:ring-1 transition-colors ${
                    errors[`link${index}`]
                      ? "border-red-500 focus:ring-red-500"
                      : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
                  }`}
                />
                {errors[`link${index}`] && (
                  <p className="mt-1 text-xs text-red-400" role="alert">
                    {errors[`link${index}`]}
                  </p>
                )}
              </div>
              <button
                type="button"
                onClick={() => removeLink(index)}
                className="mt-6 text-gray-600 hover:text-red-400 transition-colors"
                aria-label={`Remove link ${index + 1}`}
              >
                <svg
                  className="w-5 h-5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Type + Description */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label
                  htmlFor={`mock-link-type-${index}`}
                  className="block text-xs font-semibold text-[#A78BFA]/60 uppercase tracking-wider mb-1.5"
                >
                  Type
                </label>
                <select
                  id={`mock-link-type-${index}`}
                  value={link.type}
                  onChange={(e) =>
                    updateLink(index, { type: e.target.value })
                  }
                  className="w-full bg-[#0A0A12] border border-[#2A2A3C] rounded-lg px-3 py-2.5 text-sm text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
                >
                  {LINK_TYPES.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label
                  htmlFor={`mock-link-desc-${index}`}
                  className="block text-xs font-semibold text-[#A78BFA]/60 uppercase tracking-wider mb-1.5"
                >
                  Description{" "}
                  <span className="text-gray-700 normal-case font-normal">(optional)</span>
                </label>
                <input
                  id={`mock-link-desc-${index}`}
                  type="text"
                  value={link.description}
                  onChange={(e) =>
                    updateLink(index, { description: e.target.value })
                  }
                  placeholder="e.g. Stream on Spotify"
                  className="w-full bg-[#0A0A12] border border-[#2A2A3C] rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-700 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
                />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Add link button */}
      <button
        type="button"
        onClick={addLink}
        className="flex items-center gap-2 text-sm font-semibold text-[#A78BFA] hover:text-[#8B5CF6] border border-dashed border-[#8B5CF6]/40 hover:border-[#8B5CF6] rounded-lg px-4 py-3 w-full justify-center transition-colors"
      >
        <svg
          className="w-4 h-4"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2}
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M12 4.5v15m7.5-7.5h-15"
          />
        </svg>
        Add a link
      </button>

      {links.length === 0 && (
        <p className="text-center text-xs text-gray-700">
          No links added. Click the button above to add one.
        </p>
      )}
    </div>
  );
}
