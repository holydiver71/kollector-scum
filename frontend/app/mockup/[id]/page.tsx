/**
 * Design Mockup Page — /mockup/1
 *
 * Midnight redesign proposal for Kollector Sküm.
 * A fully self-contained static page using shared mock data.
 * All four key pages (Dashboard, Music Collection, Release Detail, Add Release)
 * are accessible via the tab navigation at the top of the mockup.
 *
 * Design:
 *  1 – Midnight:  Dark bg, purple accents, streaming-platform aesthetic
 *
 * No API calls are made – all data is hardcoded for visual review only.
 */
"use client";

import { useParams } from "next/navigation";
import { useState } from "react";
import Link from "next/link";

/* ── Shared mock data ───────────────────────────────────────────────────── */

/** Collection statistics shown on the Dashboard */
const STATS = { releases: 247, artists: 89, genres: 34, labels: 67 };

const RELEASES = [
  { id: 1, title: "OK Computer", artist: "Radiohead", year: "1997", format: "Vinyl", label: "Parlophone", genre: "Alternative Rock", country: "UK" },
  { id: 2, title: "Random Access Memories", artist: "Daft Punk", year: "2013", format: "CD", label: "Columbia", genre: "Electronic", country: "France" },
  { id: 3, title: "Blonde", artist: "Frank Ocean", year: "2016", format: "Vinyl", label: "Boys Don't Cry", genre: "R&B/Soul", country: "USA" },
  { id: 4, title: "Rumours", artist: "Fleetwood Mac", year: "1977", format: "Vinyl", label: "Warner Bros.", genre: "Rock", country: "UK" },
  { id: 5, title: "To Pimp A Butterfly", artist: "Kendrick Lamar", year: "2015", format: "Vinyl", label: "Top Dawg", genre: "Hip-Hop", country: "USA" },
  { id: 6, title: "Kind of Blue", artist: "Miles Davis", year: "1959", format: "CD", label: "Columbia", genre: "Jazz", country: "USA" },
  { id: 7, title: "Purple Rain", artist: "Prince", year: "1984", format: "Vinyl", label: "Warner Bros.", genre: "Pop/Rock", country: "USA" },
  { id: 8, title: "Nevermind", artist: "Nirvana", year: "1991", format: "Vinyl", label: "DGC Records", genre: "Grunge", country: "USA" },
  { id: 9, title: "Blue", artist: "Joni Mitchell", year: "1971", format: "Vinyl", label: "Reprise Records", genre: "Folk", country: "Canada" },
  { id: 10, title: "The Dark Side of the Moon", artist: "Pink Floyd", year: "1973", format: "Vinyl", label: "Harvest Records", genre: "Progressive Rock", country: "UK" },
  { id: 11, title: "Illmatic", artist: "Nas", year: "1994", format: "CD", label: "Columbia", genre: "Hip-Hop", country: "USA" },
  { id: 12, title: "Hounds of Love", artist: "Kate Bush", year: "1985", format: "Vinyl", label: "EMI", genre: "Art Pop", country: "UK" },
];

const RD = {
  title: "OK Computer", year: "1997", origYear: "1997", artist: "Radiohead",
  format: '12" Vinyl LP', label: "Parlophone", labelNumber: "NODATA 02",
  country: "UK", genres: ["Alternative Rock", "Art Rock"], upc: "0724384568924",
  tracks: [
    { pos: "A1", title: "Airbag", dur: "4:44" },
    { pos: "A2", title: "Paranoid Android", dur: "6:23" },
    { pos: "A3", title: "Subterranean Homesick Alien", dur: "4:27" },
    { pos: "A4", title: "Exit Music (For a Film)", dur: "4:24" },
    { pos: "A5", title: "Let Down", dur: "4:59" },
    { pos: "A6", title: "Karma Police", dur: "4:22" },
    { pos: "B1", title: "Fitter Happier", dur: "1:57" },
    { pos: "B2", title: "Electioneering", dur: "3:50" },
    { pos: "B3", title: "Climbing Up the Walls", dur: "4:45" },
    { pos: "B4", title: "No Surprises", dur: "3:48" },
    { pos: "B5", title: "Lucky", dur: "4:19" },
    { pos: "B6", title: "The Tourist", dur: "5:24" },
  ],
  purchase: { store: "Rough Trade", price: "GBP 22.99", date: "2023-01-15", condition: "Near Mint", notes: "Original UK press, with inner sleeve" },
  dateAdded: "2023-01-15", lastModified: "2023-06-20", lastPlayed: "2024-02-10",
};

const RP = [
  { id: 1,  title: "OK Computer",             artist: "Radiohead",       relative: "Today",        playCount: 3, color: "#6366F1" },
  { id: 2,  title: "Random Access Memories",  artist: "Daft Punk",        relative: "Today",        playCount: 1, color: "#F59E0B" },
  { id: 5,  title: "To Pimp A Butterfly",     artist: "Kendrick Lamar",   relative: "Yesterday",     playCount: 2, color: "#10B981" },
  { id: 3,  title: "Blonde",                  artist: "Frank Ocean",      relative: "Yesterday",     playCount: 1, color: "#F97316" },
  { id: 10, title: "The Dark Side of the Moon", artist: "Pink Floyd",     relative: "3 days ago",    playCount: 5, color: "#1E293B" },
  { id: 12, title: "Hounds of Love",           artist: "Kate Bush",       relative: "3 days ago",    playCount: 1, color: "#EC4899" },
  { id: 4,  title: "Rumours",                  artist: "Fleetwood Mac",   relative: "1 week ago",    playCount: 2, color: "#8B5CF6" },
  { id: 6,  title: "Kind of Blue",             artist: "Miles Davis",     relative: "1 week ago",    playCount: 4, color: "#3B82F6" },
  { id: 7,  title: "Purple Rain",              artist: "Prince",          relative: "1 week ago",    playCount: 1, color: "#A855F7" },
  { id: 8,  title: "Nevermind",                artist: "Nirvana",         relative: "2 weeks ago",   playCount: 1, color: "#06B6D4" },
  { id: 9,  title: "Blue",                     artist: "Joni Mitchell",   relative: "2 weeks ago",   playCount: 1, color: "#0EA5E9" },
  { id: 11, title: "Illmatic",                 artist: "Nas",             relative: "1 month ago",   playCount: 3, color: "#EF4444" },
];

const QA = [
  { title: "Browse Collection", icon: "📻", desc: "Explore your music library" },
  { title: "Search Music", icon: "🔍", desc: "Find specific releases" },
  { title: "Ask a Question", icon: "🔮", desc: "Natural language queries" },
  { title: "View Statistics", icon: "📊", desc: "Analyse your collection" },
  { title: "Add Release", icon: "➕", desc: "Add new music" },
  { title: "Genres", icon: "⚡", desc: "Browse by genre" },
  { title: "Artists", icon: "👤", desc: "Browse artists" },
];

const DESIGN_NAMES: Record<string, string> = {
  "1": "Midnight — Dark streaming platform aesthetic",
};

type PageTab = "dashboard" | "collection" | "release" | "add";
const PAGE_TABS: { key: PageTab; label: string }[] = [
  { key: "dashboard", label: "Dashboard" },
  { key: "collection", label: "Music Collection" },
  { key: "release", label: "Release Detail" },
  { key: "add", label: "Add Release" },
];

/** Shared release metadata form fields for the Add-Release mockup. */
const RELEASE_FORM_FIELDS: [string, string][] = [
  ["Title *", "OK Computer"],
  ["Artist *", "Radiohead"],
  ["Release Year", "1997"],
  ["Original Year", "1997"],
  ["Label", "Parlophone"],
  ["Catalogue #", "NODATA 02"],
  ["Country", "United Kingdom"],
  ["UPC", "0724384568924"],
];

/** Shared purchase info fields for the Add-Release mockup. */
const PURCHASE_FORM_FIELDS: [string, string][] = [
  ["Store", "Rough Trade"],
  ["Price", "22.99"],
  ["Date", ""],
  ["Condition", "Near Mint"],
];

/* ─── MAIN PAGE ─── */
export default function MockupPage() {
  const params = useParams();
  const id = (params?.id as string) ?? "1";
  const [activeTab, setActiveTab] = useState<PageTab>("dashboard");
  const [addTab, setAddTab] = useState<"manual" | "discogs">("manual");
  const designNum = parseInt(id, 10);

  if (isNaN(designNum) || designNum !== 1) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-4">Mockup not found</h1>
          <p className="mb-6">Valid mockup: /mockup/1 (Midnight)</p>
          <Link href="/mockup/1" className="px-4 py-2 bg-blue-600 text-white rounded">Go to Mockup 1</Link>
        </div>
      </div>
    );
  }

  const mockupNav = (
    <div className="flex items-center gap-1 flex-wrap px-4 py-2 bg-gray-900 text-xs z-50 relative">
      <span className="text-gray-400 mr-2 font-semibold">Design Mockup:</span>
      <span className="px-3 py-1 rounded font-semibold bg-white text-gray-900">#1</span>
      <span className="ml-4 text-gray-500 italic hidden sm:inline">{DESIGN_NAMES["1"]}</span>
      <span className="ml-auto">
        <Link href="/" className="text-gray-400 hover:text-white transition-colors">← Back to app</Link>
      </span>
    </div>
  );

  const tabProps = { activeTab, setActiveTab, addTab, setAddTab };

  return (
    <div>
      {mockupNav}
      <Design1 {...tabProps} />
    </div>
  );
}

type DesignProps = {
  activeTab: PageTab;
  setActiveTab: (t: PageTab) => void;
  addTab: "manual" | "discogs";
  setAddTab: (t: "manual" | "discogs") => void;
};

/* ══════════════════════════════════════════════
   DESIGN 1 — MIDNIGHT (Dark / Streaming platform)
   Dark bg, purple accents, Spotify-inspired
══════════════════════════════════════════════ */
function Design1({ activeTab, setActiveTab, addTab, setAddTab }: DesignProps) {
  return (
    <div className="min-h-screen bg-[#0A0A10] text-white" style={{ fontFamily: "Inter, sans-serif" }}>
      <header className="bg-[#0A0A10] border-b border-[#1C1C28] px-6 py-4 flex items-center justify-between sticky top-0 z-10">
        <div className="flex items-center gap-4">
          <span className="text-xl font-black tracking-tight text-white">KOLLECTOR SKÜM</span>
          <span className="text-xs text-[#8B5CF6] font-semibold uppercase tracking-widest">v2.1.0</span>
        </div>
        <div className="flex items-center gap-3">
          <input readOnly type="text" placeholder="Search..." className="bg-[#1C1C28] border border-[#2E2E3E] rounded-full px-4 py-2 text-sm text-gray-300 w-48 focus:outline-none placeholder-gray-600" />
          <div className="w-8 h-8 rounded-full bg-[#8B5CF6] flex items-center justify-center text-sm font-bold">U</div>
        </div>
      </header>
      <div className="bg-[#0A0A10] border-b border-[#1C1C28] px-6">
        <nav className="flex">
          {PAGE_TABS.map((t) => (
            <button key={t.key} onClick={() => setActiveTab(t.key)}
              className={`px-5 py-4 text-sm font-medium border-b-2 transition-colors ${activeTab === t.key ? "border-[#8B5CF6] text-[#8B5CF6]" : "border-transparent text-gray-400 hover:text-gray-200"}`}>
              {t.label}
            </button>
          ))}
        </nav>
      </div>
      <main className="max-w-7xl mx-auto px-6 py-8">
        {activeTab === "dashboard" && <D1Dashboard />}
        {activeTab === "collection" && <D1Collection />}
        {activeTab === "release" && <D1Release />}
        {activeTab === "add" && <D1Add addTab={addTab} setAddTab={setAddTab} />}
      </main>
    </div>
  );
}

function D1Dashboard() {
  return (
    <div className="space-y-8">
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-black tracking-tight">Your Collection</h1>
          <p className="text-gray-400 mt-1 text-sm">Organise and discover your music library</p>
          <div className="flex items-center gap-2 mt-3">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-xs text-emerald-400 font-semibold">System Online</span>
          </div>
        </div>
        <div className="text-right text-xs text-gray-600">Powered by Kollector API · Last sync: just now</div>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: "Releases", value: STATS.releases, color: "#8B5CF6" },
          { label: "Artists", value: STATS.artists, color: "#06B6D4" },
          { label: "Genres", value: STATS.genres, color: "#10B981" },
          { label: "Labels", value: STATS.labels, color: "#F59E0B" },
        ].map((s) => (
          <div key={s.label} className="bg-[#13131F] rounded-2xl p-5 border border-[#1C1C28] relative overflow-hidden">
            <div className="absolute -top-4 -right-4 w-20 h-20 rounded-full opacity-10" style={{ background: s.color, filter: "blur(20px)" }} />
            <div className="text-3xl font-black mb-1" style={{ color: s.color }}>{s.value.toLocaleString()}</div>
            <div className="text-xs text-gray-400 font-medium uppercase tracking-wider">{s.label}</div>
          </div>
        ))}
      </div>
      <div>
        <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4">Quick Actions</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          {QA.map((a) => (
            <div key={a.title} className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] hover:border-[#8B5CF6]/40 cursor-pointer group transition-all">
              <div className="text-2xl mb-2">{a.icon}</div>
              <div className="text-sm font-semibold text-white group-hover:text-[#8B5CF6] transition-colors">{a.title}</div>
              <div className="text-xs text-gray-500 mt-1">{a.desc}</div>
            </div>
          ))}
        </div>
      </div>
      <div>
        <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4 flex items-center gap-2">
          <span className="text-base">🎵</span> Recently Played
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {RP.map((item, i) => {
            /* Show date header only for first item of each relative date group */
            const showDate = i === 0 || RP[i - 1].relative !== item.relative;
            return (
              <div key={`${item.id}-${i}`} className="flex flex-col">
                {/* Date heading row — fixed height so covers stay aligned */}
                <div className="h-6 mb-2">
                  {showDate && (
                    <span className="text-[10px] font-bold text-[#8B5CF6] uppercase tracking-widest">
                      {item.relative}
                    </span>
                  )}
                </div>

                {/* Cover art tile with tooltip */}
                <div
                  className="group relative aspect-square rounded-xl overflow-hidden border border-[#1C1C28] hover:border-[#8B5CF6]/50 transition-all cursor-pointer shadow-sm hover:shadow-lg hover:shadow-[#8B5CF6]/10"
                  title={`${item.title} — ${item.artist}\nPlayed: ${item.relative}${item.playCount > 1 ? ` (×${item.playCount})` : ''}`}
                >
                  {/* Placeholder cover with unique colour per album */}
                  <div className="w-full h-full flex items-center justify-center text-4xl" style={{ background: `linear-gradient(135deg, ${item.color}40, ${item.color}15)` }}>
                    💿
                  </div>

                  {/* Hover overlay with title + artist */}
                  <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex flex-col justify-end p-2.5">
                    <div className="text-xs font-semibold text-white truncate leading-tight">{item.title}</div>
                    <div className="text-[10px] text-gray-300 truncate">{item.artist}</div>
                  </div>

                  {/* Play count badge — only when > 1 */}
                  {item.playCount > 1 && (
                    <div className="absolute bottom-2 right-2 bg-[#8B5CF6] text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full shadow-lg z-10">
                      ×{item.playCount}
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <div className="bg-[#13131F] rounded-2xl p-6 border border-[#1C1C28] text-center">
        <div className="text-4xl mb-3">⏱️</div>
        <p className="font-semibold text-gray-400">Activity tracking coming soon</p>
        <p className="text-sm text-gray-600 mt-1">View your recent collection updates and changes here.</p>
      </div>
    </div>
  );
}

function D1Collection() {
  const [filtersOpen, setFiltersOpen] = useState(true);

  /* Static filter options for the mockup */
  const GENRE_OPTIONS = ["Alternative Rock", "Electronic", "R&B/Soul", "Rock", "Hip-Hop", "Jazz", "Pop/Rock", "Grunge", "Folk", "Progressive Rock", "Art Pop"];
  const FORMAT_OPTIONS = ["Vinyl", "CD", "Cassette", "Digital"];
  const ARTIST_OPTIONS = ["Radiohead", "Daft Punk", "Frank Ocean", "Fleetwood Mac", "Kendrick Lamar", "Miles Davis", "Prince", "Nirvana", "Joni Mitchell", "Pink Floyd", "Nas", "Kate Bush"];
  const LABEL_OPTIONS = ["Parlophone", "Columbia", "Boys Don't Cry", "Warner Bros.", "Top Dawg", "DGC Records", "Reprise Records", "Harvest Records", "EMI"];
  const COUNTRY_OPTIONS = ["UK", "France", "USA", "Canada", "Germany", "Japan"];

  return (
    <div className="space-y-6">
      {/* Search bar + sort + Filters toggle */}
      <div className="flex gap-3 flex-wrap">
        <input readOnly type="text" placeholder="Search releases, artists, albums..." className="flex-1 min-w-64 bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none" />
        <select className="bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-gray-300 text-sm focus:outline-none">
          <option>Sort: Date Added</option><option>Title</option><option>Artist</option><option>Year</option>
        </select>
        <button
          onClick={() => setFiltersOpen((o) => !o)}
          className={`px-5 rounded-xl text-sm font-medium flex items-center gap-2 transition-all ${
            filtersOpen
              ? "bg-[#8B5CF6] text-white shadow-lg shadow-[#8B5CF6]/25"
              : "bg-[#8B5CF6] text-white"
          }`}
        >
          <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" /></svg>
          Filters
          <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor" className={`transition-transform ${filtersOpen ? "rotate-180" : ""}`}><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round" /></svg>
        </button>
      </div>

      {/* ── Expanded filter panel ── */}
      {filtersOpen && (
        <div className="bg-[#13131F] border border-[#1C1C28] rounded-2xl p-6 space-y-5 animate-in slide-in-from-top-2 duration-200">
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2">
              <svg width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2" className="text-[#8B5CF6]"><path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" /></svg>
              Advanced Filters
            </h3>
            <button className="text-xs text-[#8B5CF6] hover:text-[#A78BFA] font-medium transition-colors">Clear All</button>
          </div>

          {/* Row 1: Genre, Format, Artist */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Genre */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Genre</label>
              <div className="relative">
                <select defaultValue="Alternative Rock" className="w-full appearance-none bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-[#8B5CF6] transition-colors cursor-pointer">
                  <option value="">All Genres</option>
                  {GENRE_OPTIONS.map((g) => <option key={g} value={g}>{g}</option>)}
                </select>
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" width="14" height="14" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
              </div>
            </div>

            {/* Format */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Format</label>
              <div className="relative">
                <select className="w-full appearance-none bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-4 py-2.5 text-sm text-gray-400 focus:outline-none focus:border-[#8B5CF6] transition-colors cursor-pointer">
                  <option value="">All Formats</option>
                  {FORMAT_OPTIONS.map((f) => <option key={f} value={f}>{f}</option>)}
                </select>
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" width="14" height="14" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
              </div>
            </div>

            {/* Artist */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Artist</label>
              <div className="relative">
                <select className="w-full appearance-none bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-4 py-2.5 text-sm text-gray-400 focus:outline-none focus:border-[#8B5CF6] transition-colors cursor-pointer">
                  <option value="">All Artists</option>
                  {ARTIST_OPTIONS.map((a) => <option key={a} value={a}>{a}</option>)}
                </select>
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" width="14" height="14" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
              </div>
            </div>
          </div>

          {/* Row 2: Label, Country, Year Range */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {/* Label */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Label</label>
              <div className="relative">
                <select className="w-full appearance-none bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-4 py-2.5 text-sm text-gray-400 focus:outline-none focus:border-[#8B5CF6] transition-colors cursor-pointer">
                  <option value="">All Labels</option>
                  {LABEL_OPTIONS.map((l) => <option key={l} value={l}>{l}</option>)}
                </select>
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" width="14" height="14" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
              </div>
            </div>

            {/* Country */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Country</label>
              <div className="relative">
                <select className="w-full appearance-none bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-4 py-2.5 text-sm text-gray-400 focus:outline-none focus:border-[#8B5CF6] transition-colors cursor-pointer">
                  <option value="">All Countries</option>
                  {COUNTRY_OPTIONS.map((c) => <option key={c} value={c}>{c}</option>)}
                </select>
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 pointer-events-none" width="14" height="14" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
              </div>
            </div>

            {/* Year Range */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Year Range</label>
              <div className="flex gap-2">
                <input readOnly type="text" placeholder="From" defaultValue="" className="w-1/2 bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-3 py-2.5 text-sm text-gray-400 placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] transition-colors" />
                <span className="flex items-center text-gray-600 text-xs">–</span>
                <input readOnly type="text" placeholder="To" defaultValue="" className="w-1/2 bg-[#0A0A10] border border-[#2E2E3E] rounded-xl px-3 py-2.5 text-sm text-gray-400 placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] transition-colors" />
              </div>
            </div>
          </div>

          {/* Action row */}
          <div className="flex items-center justify-between pt-2 border-t border-[#1C1C28]">
            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 cursor-pointer group">
                <div className="w-4 h-4 rounded border border-[#2E2E3E] group-hover:border-[#8B5CF6] flex items-center justify-center transition-colors">
                  <div className="w-2.5 h-2.5 rounded-sm bg-[#8B5CF6]" />
                </div>
                <span className="text-xs text-gray-400 group-hover:text-gray-300 font-medium">Live / Bootleg only</span>
              </label>
            </div>
            <div className="flex items-center gap-3">
              <button className="text-xs text-gray-500 hover:text-gray-300 transition-colors px-3 py-1.5">Reset</button>
              <button className="bg-[#8B5CF6] hover:bg-[#7C3AED] text-white text-xs font-semibold px-5 py-2 rounded-lg transition-colors shadow-lg shadow-[#8B5CF6]/20">Apply Filters</button>
            </div>
          </div>
        </div>
      )}

      {/* Active filter chips */}
      <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap">
        <span>247 releases</span>
        <span className="bg-[#8B5CF6]/15 text-[#A78BFA] px-3 py-1 rounded-full border border-[#8B5CF6]/20 flex items-center gap-1.5 cursor-pointer hover:bg-[#8B5CF6]/25 transition-colors">Genre: Alternative Rock <span className="opacity-60">×</span></span>
        <span className="bg-[#8B5CF6]/15 text-[#A78BFA] px-3 py-1 rounded-full border border-[#8B5CF6]/20 flex items-center gap-1.5 cursor-pointer hover:bg-[#8B5CF6]/25 transition-colors">Live / Bootleg <span className="opacity-60">×</span></span>
      </div>

      {/* Release grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {RELEASES.map((r) => (
          <div key={r.id} className="group cursor-pointer">
            <div className="aspect-square bg-[#13131F] rounded-xl border border-[#1C1C28] group-hover:border-[#8B5CF6]/50 transition-all mb-2 flex items-center justify-center text-4xl">💿</div>
            <div className="text-xs font-semibold text-white truncate">{r.title}</div>
            <div className="text-xs text-gray-400 truncate">{r.artist}</div>
            <div className="text-xs text-gray-600">{r.year} · {r.format}</div>
          </div>
        ))}
      </div>

      {/* Pagination */}
      <div className="flex justify-center items-center gap-2">
        <button className="px-4 py-2 rounded-lg bg-[#13131F] border border-[#1C1C28] text-gray-400 text-sm">← Prev</button>
        {[1,2,3,4,5].map((n) => (
          <button key={n} className={`w-9 h-9 rounded-lg text-sm font-medium ${n===1 ? "bg-[#8B5CF6] text-white" : "bg-[#13131F] border border-[#1C1C28] text-gray-400"}`}>{n}</button>
        ))}
        <button className="px-4 py-2 rounded-lg bg-[#13131F] border border-[#1C1C28] text-gray-400 text-sm">Next →</button>
      </div>
    </div>
  );
}

function D1Release() {
  const [activeDisc, setActiveDisc] = useState(1);

  /* Multi-disc tracklist data — 2×LP pressing of OK Computer */
  const DISCS = [
    {
      label: "Disc 1",
      sides: [
        { side: "A", tracks: [
          { pos: "A1", title: "Airbag", dur: "4:44" },
          { pos: "A2", title: "Paranoid Android", dur: "6:23" },
          { pos: "A3", title: "Subterranean Homesick Alien", dur: "4:27" },
        ]},
        { side: "B", tracks: [
          { pos: "B1", title: "Exit Music (For a Film)", dur: "4:24" },
          { pos: "B2", title: "Let Down", dur: "4:59" },
          { pos: "B3", title: "Karma Police", dur: "4:22" },
        ]},
      ],
    },
    {
      label: "Disc 2",
      sides: [
        { side: "C", tracks: [
          { pos: "C1", title: "Fitter Happier", dur: "1:57" },
          { pos: "C2", title: "Electioneering", dur: "3:50" },
          { pos: "C3", title: "Climbing Up the Walls", dur: "4:45" },
        ]},
        { side: "D", tracks: [
          { pos: "D1", title: "No Surprises", dur: "3:48" },
          { pos: "D2", title: "Lucky", dur: "4:19" },
          { pos: "D3", title: "The Tourist", dur: "5:24" },
        ]},
      ],
    },
  ];

  const currentDisc = DISCS[activeDisc - 1];
  const totalTracks = DISCS.reduce((sum, d) => sum + d.sides.reduce((s2, side) => s2 + side.tracks.length, 0), 0);

  return (
    <div className="space-y-6">
      <button className="flex items-center gap-1 text-gray-400 hover:text-white text-sm transition-colors">← Back to Collection</button>
      <div className="grid lg:grid-cols-3 gap-8">
        <div className="space-y-4">
          <div className="aspect-square bg-[#13131F] rounded-2xl border border-[#1C1C28] flex items-center justify-center text-8xl relative overflow-hidden">
            💿
            {/* Multi-disc badge */}
            <div className="absolute top-3 right-3 bg-[#8B5CF6] text-white text-[10px] font-bold px-2 py-0.5 rounded-full shadow-lg shadow-[#8B5CF6]/30">
              2×LP
            </div>
          </div>
          <div className="flex gap-2">
            <button className="flex-1 bg-[#8B5CF6] text-white py-3 rounded-xl text-sm font-semibold">▶ Mark as Played</button>
            <button className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-[#8B5CF6] transition-colors" title="Add to List">
              <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 10h16M4 14h10m-4 4h4m2 0h.01M16 18a1 1 0 11-2 0 1 1 0 012 0z" /><path strokeLinecap="round" strokeLinejoin="round" d="M19 11v4m0 0v4m0-4h4m-4 0h-4" /></svg>
            </button>
            <a href="https://www.discogs.com/release/844254" target="_blank" rel="noopener noreferrer" className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-white transition-colors" title="View on Discogs">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M12 0C5.372 0 0 5.372 0 12s5.372 12 12 12 12-5.372 12-12S18.628 0 12 0zm0 21.6c-5.292 0-9.6-4.308-9.6-9.6S6.708 2.4 12 2.4s9.6 4.308 9.6 9.6-4.308 9.6-9.6 9.6zm0-17.04A7.44 7.44 0 004.56 12 7.44 7.44 0 0012 19.44 7.44 7.44 0 0019.44 12 7.44 7.44 0 0012 4.56zm0 12.72A5.28 5.28 0 016.72 12 5.28 5.28 0 0112 6.72 5.28 5.28 0 0117.28 12 5.28 5.28 0 0112 17.28zm0-8.4A3.12 3.12 0 008.88 12 3.12 3.12 0 0012 15.12 3.12 3.12 0 0015.12 12 3.12 3.12 0 0012 8.88z" /></svg>
            </a>
            <button className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-white transition-colors">✎</button>
            <button className="w-12 h-12 bg-[#13131F] border border-red-900/30 rounded-xl flex items-center justify-center text-red-400/60 hover:text-red-400 transition-colors">🗑</button>
          </div>
          {[
            { title: "Release Info", items: [["Format", "2× 12\" Vinyl LP"], ["Discs", "2"], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "Purchase Info", items: [["Store", RD.purchase.store], ["Price", "GBP 34.99"], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
            { title: "Collection Data", items: [["Added", RD.dateAdded], ["Modified", RD.lastModified], ["Last Played", RD.lastPlayed]] },
          ].map(({ title, items }) => (
            <div key={title} className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28] space-y-2">
              <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-3">{title}</h3>
              {items.map(([k, v]) => (
                <div key={k} className="flex justify-between text-sm">
                  <span className="text-gray-500">{k}</span>
                  <span className="text-white font-medium text-right">{v}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="lg:col-span-2 space-y-6">
          <div>
            <p className="text-[#8B5CF6] font-semibold text-sm mb-1">{RD.artist}</p>
            <h1 className="text-4xl font-black tracking-tight leading-tight">{RD.title}</h1>
            <div className="flex items-center gap-3 mt-1">
              <p className="text-gray-400">{RD.year}</p>
              <span className="text-gray-600">·</span>
              <span className="text-xs text-gray-500">{totalTracks} tracks across {DISCS.length} discs</span>
            </div>
            <div className="flex gap-2 mt-2">
              {RD.genres.map((g) => <span key={g} className="text-xs bg-[#8B5CF6]/15 text-[#A78BFA] px-2 py-1 rounded">{g}</span>)}
              <span className="text-xs bg-[#06B6D4]/15 text-[#22D3EE] px-2 py-1 rounded">2×LP</span>
            </div>
          </div>

          {/* ── Disc selector tabs ── */}
          <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
            <div className="px-4 py-3 border-b border-[#1C1C28] flex items-center justify-between">
              <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest">Tracklist</h3>
              <div className="flex items-center gap-1 bg-[#0A0A10] rounded-lg p-0.5">
                {DISCS.map((d, i) => (
                  <button
                    key={d.label}
                    onClick={() => setActiveDisc(i + 1)}
                    className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all flex items-center gap-1.5 ${
                      activeDisc === i + 1
                        ? "bg-[#8B5CF6] text-white shadow-md shadow-[#8B5CF6]/20"
                        : "text-gray-500 hover:text-gray-300"
                    }`}
                  >
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor" className="opacity-60"><circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="none"/><circle cx="12" cy="12" r="3" /></svg>
                    {d.label}
                  </button>
                ))}
                <button
                  onClick={() => setActiveDisc(0)}
                  className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all ${
                    activeDisc === 0
                      ? "bg-[#8B5CF6] text-white shadow-md shadow-[#8B5CF6]/20"
                      : "text-gray-500 hover:text-gray-300"
                  }`}
                >
                  All
                </button>
              </div>
            </div>

            {/* Show either a single disc or all discs */}
            {activeDisc === 0 ? (
              /* All discs view */
              DISCS.map((disc, di) => (
                <div key={disc.label}>
                  {/* Disc header divider */}
                  <div className="flex items-center gap-3 px-4 py-2.5 bg-[#0E0E18] border-t border-[#1C1C28]">
                    <div className="flex items-center gap-1.5">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor" className="text-[#8B5CF6]"><circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="none"/><circle cx="12" cy="12" r="3" /></svg>
                      <span className="text-xs font-bold text-[#8B5CF6] uppercase tracking-wider">{disc.label}</span>
                    </div>
                    <div className="flex-1 h-px bg-[#1C1C28]" />
                    <span className="text-[10px] text-gray-600">{disc.sides.reduce((s, side) => s + side.tracks.length, 0)} tracks</span>
                  </div>
                  {disc.sides.map((side) => (
                    <div key={side.side}>
                      {/* Side header */}
                      <div className="flex items-center gap-2 px-4 py-2 bg-[#11111B]">
                        <span className="text-[10px] font-bold text-gray-600 uppercase tracking-widest">Side {side.side}</span>
                        <div className="flex-1 h-px bg-[#1C1C28]/50" />
                      </div>
                      {side.tracks.map((t, ti) => (
                        <div key={t.pos} className={`flex items-center px-4 py-3 hover:bg-[#1C1C28] cursor-pointer ${ti > 0 || di > 0 ? "border-t border-[#1C1C28]/30" : ""}`}>
                          <span className="text-xs text-gray-600 w-8 font-mono">{t.pos}</span>
                          <span className="flex-1 text-sm text-white">{t.title}</span>
                          <span className="text-xs text-gray-500 tabular-nums">{t.dur}</span>
                        </div>
                      ))}
                    </div>
                  ))}
                </div>
              ))
            ) : (
              /* Single-disc view */
              currentDisc.sides.map((side, si) => (
                <div key={side.side}>
                  {/* Side header */}
                  <div className={`flex items-center gap-2 px-4 py-2 bg-[#11111B] ${si > 0 ? "border-t border-[#1C1C28]" : ""}`}>
                    <span className="text-[10px] font-bold text-gray-600 uppercase tracking-widest">Side {side.side}</span>
                    <div className="flex-1 h-px bg-[#1C1C28]/50" />
                    <span className="text-[10px] text-gray-600">{side.tracks.length} tracks</span>
                  </div>
                  {side.tracks.map((t, ti) => (
                    <div key={t.pos} className={`flex items-center px-4 py-3 hover:bg-[#1C1C28] cursor-pointer ${ti > 0 ? "border-t border-[#1C1C28]/30" : ""}`}>
                      <span className="text-xs text-gray-600 w-8 font-mono">{t.pos}</span>
                      <span className="flex-1 text-sm text-white">{t.title}</span>
                      <span className="text-xs text-gray-500 tabular-nums">{t.dur}</span>
                    </div>
                  ))}
                </div>
              ))
            )}

            {/* Total duration footer */}
            <div className="px-4 py-3 border-t border-[#1C1C28] bg-[#0E0E18] flex items-center justify-between">
              <span className="text-xs text-gray-600">
                {activeDisc === 0 ? `${DISCS.length} discs · ${totalTracks} tracks` : `${currentDisc.label} · ${currentDisc.sides.reduce((s, side) => s + side.tracks.length, 0)} tracks`}
              </span>
              <span className="text-xs text-gray-500 tabular-nums">Total: 53:02</span>
            </div>
          </div>

          <div className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28]">
            <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-2">Notes</h3>
            <p className="text-sm text-gray-400 italic">Original UK 2×LP pressing on 180g vinyl. Gatefold sleeve with printed inner sleeves. {RD.purchase.notes}</p>
          </div>

          {/* ── Links section ── */}
          <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
            <div className="px-4 py-3 border-b border-[#1C1C28] flex items-center justify-between">
              <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest flex items-center gap-2">
                <svg width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2" className="text-[#8B5CF6]"><path strokeLinecap="round" strokeLinejoin="round" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" /></svg>
                Links
              </h3>
              <span className="text-[10px] text-gray-600">6 links</span>
            </div>
            {[
              { type: "Discogs", url: "https://www.discogs.com/release/844254", desc: "Discogs release page", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className="text-gray-400 group-hover:text-white transition-colors"><path d="M12 0C5.372 0 0 5.372 0 12s5.372 12 12 12 12-5.372 12-12S18.628 0 12 0zm0 21.6c-5.292 0-9.6-4.308-9.6-9.6S6.708 2.4 12 2.4s9.6 4.308 9.6 9.6-4.308 9.6-9.6 9.6zm0-17.04A7.44 7.44 0 004.56 12 7.44 7.44 0 0012 19.44 7.44 7.44 0 0019.44 12 7.44 7.44 0 0012 4.56zm0 12.72A5.28 5.28 0 016.72 12 5.28 5.28 0 0112 6.72 5.28 5.28 0 0117.28 12 5.28 5.28 0 0112 17.28zm0-8.4A3.12 3.12 0 008.88 12 3.12 3.12 0 0012 15.12 3.12 3.12 0 0015.12 12 3.12 3.12 0 0012 8.88z" /></svg>
              )},
              { type: "Spotify", url: "https://open.spotify.com/album/6dVIqQ8qmQ5GBnJ9shOYGE", desc: "Listen on Spotify", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className="text-[#1DB954] group-hover:text-[#1ed760] transition-colors"><path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15 10.561 18.72 12.84c.361.181.54.78.241 1.2zm.12-3.36C15.24 8.4 8.82 8.16 5.16 9.301c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 4.26-1.26 11.28-1.02 15.721 1.621.539.3.719 1.02.419 1.56-.299.421-1.02.599-1.559.3z" /></svg>
              )},
              { type: "YouTube", url: "https://www.youtube.com/playlist?list=OLAK5uy_nEg_Hed3JqojOwsSaXJk9Fk7YBe5iamM8", desc: "Full album on YouTube", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className="text-[#FF0000] group-hover:text-[#ff3333] transition-colors"><path d="M23.498 6.186a3.016 3.016 0 00-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 00.502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 002.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 002.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z" /></svg>
              )},
              { type: "MusicBrainz", url: "https://musicbrainz.org/release/a1c42b12-14e6-3407-a5e4-c4bae54bf50f", desc: "MusicBrainz entry", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-[#BA478F] group-hover:text-[#d45daa] transition-colors"><circle cx="12" cy="12" r="10" /><path strokeLinecap="round" d="M8 8l8 8M16 8l-8 8" /></svg>
              )},
              { type: "Bandcamp", url: "https://radiohead.bandcamp.com/album/ok-computer", desc: "Buy on Bandcamp", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className="text-[#629aa9] group-hover:text-[#7bc0d2] transition-colors"><path d="M0 18.75l7.437-13.5H24l-7.438 13.5H0z" /></svg>
              )},
              { type: "Last.fm", url: "https://www.last.fm/music/Radiohead/OK+Computer", desc: "Scrobbles & stats", icon: (
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" className="text-[#D51007] group-hover:text-[#ff2a20] transition-colors"><path d="M10.584 17.21l-.88-2.392s-1.43 1.594-3.573 1.594c-1.897 0-3.244-1.649-3.244-4.288 0-3.382 1.704-4.591 3.381-4.591 2.42 0 3.189 1.567 3.849 3.574l.88 2.749c.88 2.666 2.529 4.81 7.284 4.81 3.409 0 5.718-1.044 5.718-3.793 0-2.227-1.265-3.381-3.63-3.933l-1.758-.385c-1.21-.275-1.567-.77-1.567-1.594 0-.935.742-1.484 1.952-1.484 1.32 0 2.034.495 2.144 1.677l2.749-.33c-.22-2.474-1.924-3.492-4.729-3.492-2.474 0-4.893.935-4.893 3.932 0 1.87.907 3.051 3.189 3.602l1.87.44c1.402.33 1.869.825 1.869 1.648 0 1.044-.99 1.484-2.86 1.484-2.776 0-3.933-1.457-4.591-3.464l-.907-2.749c-1.155-3.575-2.997-4.894-6.653-4.894C1.731 6.328 0 8.878 0 12.944c0 3.878 1.731 6.126 5.088 6.126 2.694 0 4.344-1.209 5.496-1.86z" /></svg>
              )},
            ].map((link, i) => (
              <a
                key={link.type}
                href={link.url}
                target="_blank"
                rel="noopener noreferrer"
                className={`flex items-center gap-4 px-4 py-3 hover:bg-[#1C1C28] cursor-pointer group transition-all ${i > 0 ? "border-t border-[#1C1C28]/50" : ""}`}
              >
                <div className="w-8 h-8 rounded-lg bg-[#0A0A10] flex items-center justify-center flex-shrink-0">
                  {link.icon}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-semibold text-white group-hover:text-[#8B5CF6] transition-colors">{link.type}</div>
                  <div className="text-xs text-gray-500 truncate">{link.desc}</div>
                </div>
                <svg width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2" className="text-gray-600 group-hover:text-[#8B5CF6] transition-colors flex-shrink-0"><path strokeLinecap="round" strokeLinejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" /></svg>
              </a>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function D1Add({ addTab, setAddTab }: { addTab: "manual"|"discogs"; setAddTab: (t:"manual"|"discogs")=>void }) {
  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-black">Add Release</h1>
        <p className="text-gray-400 mt-1 text-sm">Add a new music release to your collection</p>
      </div>
      <div className="flex gap-1 bg-[#13131F] p-1 rounded-xl border border-[#1C1C28] w-fit">
        {(["discogs","manual"] as const).map((t) => (
          <button key={t} onClick={() => setAddTab(t)}
            className={`px-5 py-2 rounded-lg text-sm font-semibold transition-colors ${addTab===t ? "bg-[#8B5CF6] text-white" : "text-gray-400 hover:text-white"}`}>
            {t === "discogs" ? "🔎 Search Discogs" : "✏️ Manual Entry"}
          </button>
        ))}
      </div>
      {addTab === "discogs" ? (
        <div className="space-y-4">
          <div className="flex gap-3">
            <input readOnly type="text" placeholder="Search Discogs — artist, album, barcode..." className="flex-1 bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none" />
            <button className="bg-[#8B5CF6] text-white px-6 rounded-xl text-sm font-semibold">Search</button>
          </div>
          <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] p-12 text-center text-gray-600">
            <p className="text-4xl mb-3">🔍</p>
            <p className="font-semibold">Search Discogs to find and import releases</p>
          </div>
        </div>
      ) : (
        <div className="bg-[#13131F] rounded-2xl border border-[#1C1C28] p-6 space-y-5">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {RELEASE_FORM_FIELDS.map(([lbl, ph]) => (
              <div key={lbl}>
                <label className="block text-xs font-semibold text-gray-400 mb-1">{lbl}</label>
                <input readOnly type="text" placeholder={ph} className="w-full bg-[#0A0A10] border border-[#1C1C28] rounded-lg px-3 py-2.5 text-white placeholder-gray-700 text-sm focus:outline-none" />
              </div>
            ))}
            <div>
              <label className="block text-xs font-semibold text-gray-400 mb-1">Format</label>
              <select className="w-full bg-[#0A0A10] border border-[#1C1C28] rounded-lg px-3 py-2.5 text-white text-sm focus:outline-none">
                <option>12&quot; Vinyl LP</option><option>CD</option><option>Cassette</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-400 mb-1">Genre</label>
              <select className="w-full bg-[#0A0A10] border border-[#1C1C28] rounded-lg px-3 py-2.5 text-white text-sm focus:outline-none">
                <option>Alternative Rock</option><option>Electronic</option><option>Hip-Hop</option><option>Jazz</option>
              </select>
            </div>
          </div>
          <div className="pt-2 border-t border-[#1C1C28]">
            <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-3">Purchase Information</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[["Store / Purchased From","e.g. Rough Trade"],["Purchase Price","e.g. 22.99"],["Purchase Date",""],["Condition","e.g. Near Mint"]].map(([lbl,ph]) => (
                <div key={lbl}>
                  <label className="block text-xs font-semibold text-gray-400 mb-1">{lbl}</label>
                  <input readOnly type="text" placeholder={ph} className="w-full bg-[#0A0A10] border border-[#1C1C28] rounded-lg px-3 py-2.5 text-white placeholder-gray-700 text-sm focus:outline-none" />
                </div>
              ))}
            </div>
          </div>
          <div className="flex gap-3 pt-2">
            <button className="flex-1 bg-[#8B5CF6] text-white py-3 rounded-xl font-semibold text-sm">Add to Collection</button>
            <button className="px-6 py-3 bg-[#0A0A10] border border-[#1C1C28] text-gray-400 rounded-xl text-sm">Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}

