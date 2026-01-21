"use client";
import { useEffect, useState, memo, useMemo } from "react";
import Link from "next/link";
import Image from "next/image";
import { getRecentlyPlayed, RecentlyPlayedItemDto, API_BASE_URL } from "../lib/api";

/**
 * Formats a date as a relative date string (Today, Yesterday, X days ago, etc.)
 * @param date - The date to format
 * @returns A relative date string
 */
function formatRelativeDate(date: Date): string {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const targetDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  
  const diffTime = today.getTime() - targetDate.getTime();
  const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
  
  if (diffDays === 0) {
    return "Today";
  } else if (diffDays === 1) {
    return "Yesterday";
  } else if (diffDays < 7) {
    return `${diffDays} days ago`;
  } else if (diffDays < 14) {
    return "1 week ago";
  } else if (diffDays < 30) {
    const weeks = Math.floor(diffDays / 7);
    return `${weeks} weeks ago`;
  } else if (diffDays < 60) {
    return "1 month ago";
  } else if (diffDays < 365) {
    // Approximation: using 30 days per month for simplicity in relative date display
    const months = Math.floor(diffDays / 30);
    return `${months} months ago`;
  } else if (diffDays < 730) {
    return "1 year ago";
  } else {
    // Approximation: using 365 days per year for simplicity in relative date display
    const years = Math.floor(diffDays / 365);
    return `${years} years ago`;
  }
}

/**
 * Gets the image URL for a cover image
 * @param coverFront - The cover front filename
 * @returns The full URL to the image
 */
function getImageUrl(coverFront?: string): string {
  if (!coverFront) {
    return "/placeholder-album.svg";
  }
  
  // If it's already a full URL, return it as-is
  if (coverFront.startsWith("http://") || coverFront.startsWith("https://")) {
    return coverFront;
  }
  
  // Otherwise, prepend the API images path
  const apiBaseUrl = API_BASE_URL || "http://localhost:5072";
  return `${apiBaseUrl}/api/images/${coverFront}`;
}

interface RecentlyPlayedProps {
  maxItems?: number;
}

function RecentlyPlayedComponent({ maxItems = 24 }: RecentlyPlayedProps) {
  const [items, setItems] = useState<RecentlyPlayedItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRecentlyPlayed = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await getRecentlyPlayed(maxItems);
        setItems(data);
      } catch (e) {
        console.error("Failed to fetch recently played:", e);
        setError(e instanceof Error ? e.message : "Failed to load recently played");
      } finally {
        setLoading(false);
      }
    };

    fetchRecentlyPlayed();
  }, [maxItems]);

  // Group items by relative date and determine which ones show date headings
  // Only the first item for each relative date period should show the date heading
  // Memoize to prevent recalculation on every render
  const itemsWithDateInfo = useMemo(() => {
    let lastRelativeDateString = "";
    return items.map((item) => {
      const playedDate = new Date(item.playedAt);
      const relativeDate = formatRelativeDate(playedDate);
      const showDate = relativeDate !== lastRelativeDateString;
      lastRelativeDateString = relativeDate;
      
      return {
        ...item,
        showDate,
        relativeDate,
      };
    });
  }, [items]);

  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
        <h3 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
          <span className="text-xl">🎵</span> Recently Played
        </h3>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {[...Array(6)].map((_, i) => (
            <div key={i}>
              <div className="h-6 mb-2" />
              <div className="aspect-square bg-gray-200 animate-pulse rounded-lg" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
        <h3 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
          <span className="text-xl">🎵</span> Recently Played
        </h3>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-4">⚠️</div>
          <p className="font-bold mb-2">Unable to load recently played</p>
          <p className="text-sm font-medium">{error}</p>
        </div>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
        <h3 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
          <span className="text-xl">🎵</span> Recently Played
        </h3>
        <div className="text-center py-8 text-gray-500">
          <div className="text-4xl mb-4">🎧</div>
          <p className="font-bold mb-2">No recently played albums</p>
          <p className="text-sm font-medium">Mark albums as &quot;Now Playing&quot; to see them here.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
      <h3 className="text-lg font-bold text-gray-900 mb-6 flex items-center gap-2">
        <span className="text-xl">🎵</span> Recently Played
      </h3>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
        {itemsWithDateInfo.map((item) => (
          <div key={`${item.id}-${item.playedAt}`} className="flex flex-col">
            {/* Date heading - only shown for first item of each date */}
            <div className="h-6 mb-2">
              {item.showDate && (
                <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  {item.relativeDate}
                </span>
              )}
            </div>
            
            {/* Clickable cover image with play count badge */}
            <Link
              href={`/releases/${item.id}`}
              className="block aspect-square rounded-lg overflow-hidden shadow-sm hover:shadow-md transition-shadow border border-gray-200 relative"
              title={`Played on ${new Date(item.playedAt).toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              })}`}
            >
              <Image
                src={getImageUrl(item.coverFront)}
                alt="Album cover"
                fill
                sizes="(max-width: 640px) 50vw, (max-width: 768px) 33vw, (max-width: 1024px) 25vw, 16vw"
                className="object-cover"
                loading="lazy"
              />
              {/* Play count badge - only shown if played more than once */}
              {item.playCount > 1 && (
                <div className="absolute bottom-2 right-2 bg-red-500 text-white text-xs font-bold px-2 py-1 rounded-full shadow-md">
                  x{item.playCount}
                </div>
              )}
            </Link>
          </div>
        ))}
      </div>
    </div>
  );
}

// Export memoized component to prevent unnecessary re-renders
export const RecentlyPlayed = memo(RecentlyPlayedComponent);
