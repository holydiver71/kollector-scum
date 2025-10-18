"use client";
import { useState, useEffect } from 'react';
import Image from 'next/image';
import { getCollectionStatistics, CollectionStatistics } from '../lib/api';
import { LoadingSpinner } from '../components/LoadingComponents';
import { StatCard, BarChart, LineChart, DonutChart } from '../components/StatisticsCharts';
import Link from 'next/link';

export default function StatisticsPage() {
  const [statistics, setStatistics] = useState<CollectionStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStatistics = async () => {
      try {
        setLoading(true);
        const data = await getCollectionStatistics();
        setStatistics(data);
        setError(null);
      } catch (err) {
        console.error('Failed to fetch statistics:', err);
        setError('Failed to load collection statistics. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchStatistics();
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="bg-white border-b border-gray-200">
          <div className="max-w-7xl mx-auto px-4 py-6">
            <h1 className="text-2xl font-bold text-gray-900">Collection Statistics</h1>
            <p className="text-gray-600 mt-1">Analyze your music collection</p>
          </div>
        </div>
        <div className="max-w-7xl mx-auto px-4 py-12">
          <LoadingSpinner />
        </div>
      </div>
    );
  }

  if (error || !statistics) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="bg-white border-b border-gray-200">
          <div className="max-w-7xl mx-auto px-4 py-6">
            <h1 className="text-2xl font-bold text-gray-900">Collection Statistics</h1>
          </div>
        </div>
        <div className="max-w-7xl mx-auto px-4 py-12">
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
            {error}
          </div>
        </div>
      </div>
    );
  }

  // Format currency
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP'
    }).format(value);
  };

  // Export data as JSON
  const exportData = () => {
    const dataStr = JSON.stringify(statistics, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `kollector-skum-statistics-${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    URL.revokeObjectURL(url);
  };

  // Export data as CSV
  const exportCSV = () => {
    let csv = 'Collection Statistics Report\n\n';
    csv += `Total Releases,${statistics.totalReleases}\n`;
    csv += `Total Artists,${statistics.totalArtists}\n`;
    csv += `Total Genres,${statistics.totalGenres}\n`;
    csv += `Total Labels,${statistics.totalLabels}\n`;
    if (statistics.totalValue) csv += `Total Value,${statistics.totalValue}\n`;
    if (statistics.averagePrice) csv += `Average Price,${statistics.averagePrice}\n`;
    
    csv += '\n\nReleases by Year\n';
    csv += 'Year,Count\n';
    statistics.releasesByYear.forEach(item => {
      csv += `${item.year},${item.count}\n`;
    });

    csv += '\n\nReleases by Genre\n';
    csv += 'Genre,Count,Percentage\n';
    statistics.releasesByGenre.forEach(item => {
      csv += `${item.genreName},${item.count},${item.percentage}%\n`;
    });

    csv += '\n\nReleases by Format\n';
    csv += 'Format,Count,Percentage\n';
    statistics.releasesByFormat.forEach(item => {
      csv += `${item.formatName},${item.count},${item.percentage}%\n`;
    });

    const csvBlob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(csvBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `kollector-skum-statistics-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Collection Statistics</h1>
              <p className="text-gray-600 mt-1">Comprehensive analysis of your music collection</p>
            </div>
            <div className="flex gap-2">
              <button
                onClick={exportCSV}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors flex items-center gap-2"
              >
                <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Export CSV
              </button>
              <button
                onClick={exportData}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors flex items-center gap-2"
              >
                <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Export JSON
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Overview Stats */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
          <StatCard
            title="Total Releases"
            value={statistics.totalReleases.toLocaleString()}
            icon="💿"
          />
          <StatCard
            title="Unique Artists"
            value={statistics.totalArtists.toLocaleString()}
            icon="🎤"
          />
          <StatCard
            title="Genres"
            value={statistics.totalGenres.toLocaleString()}
            icon="🎵"
          />
          <StatCard
            title="Labels"
            value={statistics.totalLabels.toLocaleString()}
            icon="🏷️"
          />
        </div>

        {/* Value Stats */}
        {(statistics.totalValue || statistics.averagePrice) && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            {statistics.totalValue && (
              <StatCard
                title="Collection Value"
                value={formatCurrency(statistics.totalValue)}
                subtitle="Total purchase price"
                icon="💰"
              />
            )}
            {statistics.averagePrice && (
              <StatCard
                title="Average Price"
                value={formatCurrency(statistics.averagePrice)}
                subtitle="Per release"
                icon="💵"
              />
            )}
            {statistics.mostExpensiveRelease && (
              <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
                <p className="text-sm font-medium text-gray-600 mb-2">Most Expensive</p>
                <Link
                  href={`/releases/${statistics.mostExpensiveRelease.id}`}
                  className="text-lg font-semibold text-blue-600 hover:text-blue-700 line-clamp-2"
                >
                  {statistics.mostExpensiveRelease.title}
                </Link>
                {statistics.mostExpensiveRelease.artistNames && statistics.mostExpensiveRelease.artistNames.length > 0 && (
                  <p className="text-sm text-gray-500 mt-1">
                    {statistics.mostExpensiveRelease.artistNames.join(', ')}
                  </p>
                )}
              </div>
            )}
          </div>
        )}

        {/* Charts Row 1 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
          <LineChart
            data={statistics.releasesByYear}
            title="Releases by Year"
          />
          <DonutChart
            data={statistics.releasesByFormat.map(f => ({
              label: f.formatName,
              value: f.count,
              percentage: f.percentage
            }))}
            title="Formats Distribution"
          />
        </div>

        {/* Charts Row 2 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
          <BarChart
            data={statistics.releasesByGenre.map(g => ({
              label: g.genreName,
              value: g.count,
              percentage: g.percentage
            }))}
            title="Top Genres"
            maxBars={10}
          />
          <BarChart
            data={statistics.releasesByCountry.map(c => ({
              label: c.countryName,
              value: c.count,
              percentage: c.percentage
            }))}
            title="Top Countries"
            maxBars={10}
          />
        </div>

        {/* Recently Added */}
        {statistics.recentlyAdded.length > 0 && (
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Recently Added</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
              {statistics.recentlyAdded.map((release) => {
                const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
                const imageUrl = release.coverImageUrl
                  ? `${apiBaseUrl}/api/images/${release.coverImageUrl}`
                  : '/placeholder-album.svg';

                return (
                  <Link
                    key={release.id}
                    href={`/releases/${release.id}`}
                    className="group"
                  >
                    <div className="aspect-square mb-2 overflow-hidden rounded-md border border-gray-200 relative">
                      <Image
                        src={imageUrl}
                        alt={release.title}
                        fill
                        sizes="(max-width: 768px) 50vw, 200px"
                        className="object-cover group-hover:scale-105 transition-transform"
                        loading="lazy"
                      />
                    </div>
                    <p className="text-sm font-medium text-gray-900 line-clamp-1 group-hover:text-blue-600">
                      {release.title}
                    </p>
                    {release.artistNames && release.artistNames.length > 0 && (
                      <p className="text-xs text-gray-500 line-clamp-1">
                        {release.artistNames.join(', ')}
                      </p>
                    )}
                  </Link>
                );
              })}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
