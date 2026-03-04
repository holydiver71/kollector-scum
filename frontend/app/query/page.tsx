"use client";
import { NaturalLanguageQuery } from '../components/NaturalLanguageQuery';

export default function QueryPage() {
  return (
    <div className="min-h-screen bg-transparent">
      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-8 mb-6">
          <NaturalLanguageQuery showSql={true} />
        </div>

        {/* Tips Section */}
        <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-6">
          <h2 className="text-lg font-bold text-white mb-4 flex items-center gap-2">
            <span className="text-xl">💡</span>
            Tips for Better Results
          </h2>
          <ul className="space-y-2 text-gray-400">
            <li className="flex items-start gap-2">
              <span className="text-emerald-400">✓</span>
              <span>Be specific about what you&apos;re looking for (e.g., &quot;vinyl releases from the 1980s&quot; instead of just &quot;old records&quot;)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-400">✓</span>
              <span>You can ask about counts, lists, or specific details</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-400">✓</span>
              <span>Try questions like &quot;How many&quot;, &quot;Show me all&quot;, &quot;List&quot;, or &quot;Which&quot;</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-emerald-400">✓</span>
              <span>Filter by format (CD, Vinyl, Cassette), year, artist, label, country, or genre</span>
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
}
