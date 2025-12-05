"use client";
import { NaturalLanguageQuery } from '../components/NaturalLanguageQuery';

export default function QueryPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <section className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-12">
          <h1 className="text-4xl font-black text-gray-900 mb-2 flex items-center gap-3">
            <span className="text-3xl">🔮</span>
            Ask Your Collection
          </h1>
          <p className="text-lg text-gray-600 font-medium">
            Ask questions about your music collection in natural language
          </p>
        </div>
      </section>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 py-12">
        <div className="bg-white rounded-lg border border-gray-200 p-8 shadow-sm">
          <NaturalLanguageQuery showSql={true} />
        </div>

        {/* Tips Section */}
        <div className="mt-8 bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
          <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
            <span className="text-xl">💡</span>
            Tips for Better Results
          </h2>
          <ul className="space-y-2 text-gray-700">
            <li className="flex items-start gap-2">
              <span className="text-green-500">✓</span>
              <span>Be specific about what you&apos;re looking for (e.g., &quot;vinyl releases from the 1980s&quot; instead of just &quot;old records&quot;)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-green-500">✓</span>
              <span>You can ask about counts, lists, or specific details</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-green-500">✓</span>
              <span>Try questions like &quot;How many&quot;, &quot;Show me all&quot;, &quot;List&quot;, or &quot;Which&quot;</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-green-500">✓</span>
              <span>Filter by format (CD, Vinyl, Cassette), year, artist, label, country, or genre</span>
            </li>
          </ul>
        </div>
      </main>
    </div>
  );
}
