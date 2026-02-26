/**
 * Design Mockup Pages — /mockup/1 through /mockup/5
 *
 * Five visually distinct redesign proposals for Kollector Sküm.
 * Each mockup is a fully self-contained static page using shared mock data.
 * All four key pages (Dashboard, Music Collection, Release Detail, Add Release)
 * are accessible via the tab navigation at the top of each mockup.
 *
 * Designs:
 *  1 – Midnight:       Dark bg, purple accents, streaming-platform aesthetic
 *  2 – Clean Pro:      White bg, ultra-minimal, Apple-inspired
 *  3 – Warm Crate:     Cream/amber, vintage vinyl record-store feel
 *  4 – Bold Editorial: High-contrast magazine style
 *  5 – Neo Glow:       Very dark bg, cyan/purple neon, glassmorphism cards
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
  { id: 1, title: "OK Computer", artist: "Radiohead", playedAt: "2024-02-10" },
  { id: 5, title: "To Pimp A Butterfly", artist: "Kendrick Lamar", playedAt: "2024-02-08" },
  { id: 10, title: "The Dark Side of the Moon", artist: "Pink Floyd", playedAt: "2024-02-07" },
  { id: 12, title: "Hounds of Love", artist: "Kate Bush", playedAt: "2024-02-06" },
  { id: 4, title: "Rumours", artist: "Fleetwood Mac", playedAt: "2024-02-05" },
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
  "2": "Clean Pro — Minimal, Apple-inspired",
  "3": "Warm Crate — Vintage vinyl record store",
  "4": "Bold Editorial — High-contrast magazine style",
  "5": "Neo Glow — Futuristic glassmorphism",
};

type PageTab = "dashboard" | "collection" | "release" | "add";
const PAGE_TABS: { key: PageTab; label: string }[] = [
  { key: "dashboard", label: "Dashboard" },
  { key: "collection", label: "Music Collection" },
  { key: "release", label: "Release Detail" },
  { key: "add", label: "Add Release" },
];

/** Shared release metadata form fields reused across all 5 Add-Release designs. */
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

/** Shared purchase info fields reused across all 5 Add-Release designs. */
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

  if (isNaN(designNum) || designNum < 1 || designNum > 5) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-4">Mockup not found</h1>
          <p className="mb-6">Valid mockup numbers are 1–5.</p>
          <Link href="/mockup/1" className="px-4 py-2 bg-blue-600 text-white rounded">Go to Mockup 1</Link>
        </div>
      </div>
    );
  }

  const mockupNav = (
    <div className="flex items-center gap-1 flex-wrap px-4 py-2 bg-gray-900 text-xs z-50 relative">
      <span className="text-gray-400 mr-2 font-semibold">Design Mockup:</span>
      {[1, 2, 3, 4, 5].map((n) => (
        <Link
          key={n}
          href={`/mockup/${n}`}
          className={`px-3 py-1 rounded font-semibold transition-colors ${
            n === designNum ? "bg-white text-gray-900" : "text-gray-300 hover:text-white hover:bg-gray-700"
          }`}
        >
          #{n}
        </Link>
      ))}
      <span className="ml-4 text-gray-500 italic hidden sm:inline">{DESIGN_NAMES[id]}</span>
      <span className="ml-auto">
        <Link href="/" className="text-gray-400 hover:text-white transition-colors">← Back to app</Link>
      </span>
    </div>
  );

  const tabProps = { activeTab, setActiveTab, addTab, setAddTab };

  return (
    <div>
      {mockupNav}
      {designNum === 1 && <Design1 {...tabProps} />}
      {designNum === 2 && <Design2 {...tabProps} />}
      {designNum === 3 && <Design3 {...tabProps} />}
      {designNum === 4 && <Design4 {...tabProps} />}
      {designNum === 5 && <Design5 {...tabProps} />}
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
        <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4">Recently Played</h2>
        <div className="space-y-2">
          {RP.map((item, i) => (
            <div key={item.id} className="flex items-center gap-4 bg-[#13131F] rounded-xl px-4 py-3 border border-[#1C1C28] hover:border-[#8B5CF6]/40 transition-all cursor-pointer">
              <span className="text-gray-600 text-xs w-5 text-center">{i + 1}</span>
              <div className="w-10 h-10 rounded-lg bg-[#1C1C28] flex items-center justify-center text-lg flex-shrink-0">💿</div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-semibold text-white truncate">{item.title}</div>
                <div className="text-xs text-gray-400">{item.artist}</div>
              </div>
              <div className="text-xs text-gray-600 flex-shrink-0">{item.playedAt}</div>
            </div>
          ))}
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
  return (
    <div className="space-y-6">
      <div className="flex gap-3 flex-wrap">
        <input readOnly type="text" placeholder="Search releases, artists, albums..." className="flex-1 min-w-64 bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none" />
        <select className="bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-gray-300 text-sm focus:outline-none">
          <option>Sort: Date Added</option><option>Title</option><option>Artist</option><option>Year</option>
        </select>
        <button className="bg-[#8B5CF6] text-white px-5 rounded-xl text-sm font-medium">Filters</button>
      </div>
      <div className="flex items-center gap-3 text-xs text-gray-500">
        <span>247 releases</span>
        <span className="bg-[#8B5CF6]/15 text-[#A78BFA] px-3 py-1 rounded-full border border-[#8B5CF6]/20">Genre: Alternative Rock ×</span>
      </div>
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
  return (
    <div className="space-y-6">
      <button className="flex items-center gap-1 text-gray-400 hover:text-white text-sm transition-colors">← Back to Collection</button>
      <div className="grid lg:grid-cols-3 gap-8">
        <div className="space-y-4">
          <div className="aspect-square bg-[#13131F] rounded-2xl border border-[#1C1C28] flex items-center justify-center text-8xl">💿</div>
          <div className="flex gap-2">
            <button className="flex-1 bg-[#8B5CF6] text-white py-3 rounded-xl text-sm font-semibold">▶ Mark as Played</button>
            <button className="w-12 h-12 bg-[#13131F] border border-[#1C1C28] rounded-xl flex items-center justify-center text-gray-400 hover:text-white transition-colors">✎</button>
            <button className="w-12 h-12 bg-[#13131F] border border-red-900/30 rounded-xl flex items-center justify-center text-red-400/60 hover:text-red-400 transition-colors">🗑</button>
          </div>
          {[
            { title: "Release Info", items: [["Format", RD.format], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "Purchase Info", items: [["Store", RD.purchase.store], ["Price", RD.purchase.price], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
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
            <p className="text-gray-400 mt-1">{RD.year}</p>
            <div className="flex gap-2 mt-2">
              {RD.genres.map((g) => <span key={g} className="text-xs bg-[#8B5CF6]/15 text-[#A78BFA] px-2 py-1 rounded">{g}</span>)}
            </div>
          </div>
          <div className="bg-[#13131F] rounded-xl border border-[#1C1C28] overflow-hidden">
            <div className="px-4 py-3 border-b border-[#1C1C28]">
              <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest">Tracklist</h3>
            </div>
            {RD.tracks.map((t, i) => (
              <div key={t.pos} className={`flex items-center px-4 py-3 hover:bg-[#1C1C28] cursor-pointer ${i>0 ? "border-t border-[#1C1C28]/50" : ""}`}>
                <span className="text-xs text-gray-600 w-8">{t.pos}</span>
                <span className="flex-1 text-sm text-white">{t.title}</span>
                <span className="text-xs text-gray-500">{t.dur}</span>
              </div>
            ))}
          </div>
          <div className="bg-[#13131F] rounded-xl p-4 border border-[#1C1C28]">
            <h3 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-2">Notes</h3>
            <p className="text-sm text-gray-400 italic">{RD.purchase.notes}</p>
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

/* ══════════════════════════════════════════════
   DESIGN 2 — CLEAN PRO (Minimal / Apple-inspired)
   White bg, black accents, generous whitespace
══════════════════════════════════════════════ */
function Design2({ activeTab, setActiveTab, addTab, setAddTab }: DesignProps) {
  return (
    <div className="min-h-screen bg-white text-gray-900" style={{ fontFamily: "Inter, sans-serif" }}>
      <header className="bg-white/95 backdrop-blur border-b border-gray-100 px-8 py-4 flex items-center justify-between sticky top-0 z-10">
        <span className="text-lg font-bold tracking-tight">Kollector Sküm</span>
        <nav className="hidden md:flex gap-1">
          {PAGE_TABS.map((t) => (
            <button key={t.key} onClick={() => setActiveTab(t.key)}
              className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${activeTab===t.key ? "bg-gray-900 text-white" : "text-gray-500 hover:text-gray-900"}`}>
              {t.label}
            </button>
          ))}
        </nav>
        <div className="flex items-center gap-3">
          <input readOnly type="text" placeholder="Search..." className="bg-gray-100 rounded-full px-4 py-2 text-sm text-gray-600 w-40 focus:outline-none placeholder-gray-400 border-0" />
          <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-xs font-semibold text-gray-600">U</div>
        </div>
      </header>
      <main className="max-w-6xl mx-auto px-8 py-12">
        {activeTab === "dashboard" && <D2Dashboard />}
        {activeTab === "collection" && <D2Collection />}
        {activeTab === "release" && <D2Release />}
        {activeTab === "add" && <D2Add addTab={addTab} setAddTab={setAddTab} />}
      </main>
    </div>
  );
}

function D2Dashboard() {
  return (
    <div className="space-y-12">
      <div>
        <h1 className="text-5xl font-bold tracking-tight text-gray-900">Good evening.</h1>
        <p className="text-gray-400 mt-2 text-lg">Your collection has {STATS.releases} releases.</p>
        <div className="flex items-center gap-2 mt-4">
          <span className="w-2 h-2 rounded-full bg-green-500" />
          <span className="text-xs text-green-600 font-semibold">Online · v2.1.0</span>
        </div>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: "Releases", value: STATS.releases },
          { label: "Artists", value: STATS.artists },
          { label: "Genres", value: STATS.genres },
          { label: "Labels", value: STATS.labels },
        ].map((s) => (
          <div key={s.label} className="bg-gray-50 rounded-2xl p-7 border border-gray-100">
            <div className="text-4xl font-bold text-gray-900 mb-1">{s.value.toLocaleString()}</div>
            <div className="text-sm text-gray-400 font-medium">{s.label}</div>
          </div>
        ))}
      </div>
      <div>
        <h2 className="text-xs font-semibold text-gray-400 tracking-widest uppercase mb-5">Quick Actions</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          {QA.map((a) => (
            <div key={a.title} className="rounded-2xl border border-gray-100 p-5 hover:border-gray-300 hover:shadow-sm cursor-pointer transition-all group">
              <div className="text-2xl mb-3">{a.icon}</div>
              <div className="text-sm font-semibold text-gray-800">{a.title}</div>
              <div className="text-xs text-gray-400 mt-1">{a.desc}</div>
            </div>
          ))}
        </div>
      </div>
      <div>
        <h2 className="text-xs font-semibold text-gray-400 tracking-widest uppercase mb-5">Recently Played</h2>
        <div className="rounded-2xl border border-gray-100 overflow-hidden divide-y divide-gray-50">
          {RP.map((item, i) => (
            <div key={item.id} className="flex items-center gap-4 px-5 py-4 hover:bg-gray-50 cursor-pointer transition-colors">
              <span className="text-gray-300 text-sm w-5 text-center">{i+1}</span>
              <div className="w-10 h-10 rounded-xl bg-gray-100 flex items-center justify-center flex-shrink-0">💿</div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-semibold text-gray-900 truncate">{item.title}</div>
                <div className="text-xs text-gray-400">{item.artist}</div>
              </div>
              <div className="text-xs text-gray-300 flex-shrink-0">{item.playedAt}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="rounded-2xl border border-gray-100 p-10 text-center">
        <div className="text-4xl mb-3">⏱️</div>
        <p className="text-sm font-semibold text-gray-500">Activity tracking coming soon</p>
        <p className="text-xs text-gray-300 mt-1">View your recent collection updates and changes here.</p>
      </div>
      <div className="text-center">
        <p className="text-xs text-gray-300">Powered by Kollector API v2.1.0 · Last sync: just now</p>
      </div>
    </div>
  );
}

function D2Collection() {
  return (
    <div className="space-y-6">
      <div className="flex gap-3 flex-wrap items-center">
        <input readOnly type="text" placeholder="Search releases, artists, albums..." className="flex-1 min-w-64 bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm placeholder-gray-400 focus:outline-none" />
        <select className="bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm text-gray-700 focus:outline-none">
          <option>Date Added</option><option>Title</option><option>Artist</option><option>Year</option>
        </select>
        <button className="bg-gray-900 text-white px-5 py-2.5 rounded-xl text-sm font-medium">Filters</button>
      </div>
      <div className="flex items-center gap-2 text-xs text-gray-400">
        <span>247 releases</span>
        <span className="bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full border border-gray-200">Genre: Alternative Rock ×</span>
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {RELEASES.map((r) => (
          <div key={r.id} className="group cursor-pointer">
            <div className="aspect-square bg-gray-50 rounded-xl border border-gray-100 group-hover:border-gray-300 group-hover:shadow-md transition-all mb-2 flex items-center justify-center text-4xl">💿</div>
            <div className="text-xs font-semibold text-gray-900 truncate">{r.title}</div>
            <div className="text-xs text-gray-500 truncate">{r.artist}</div>
            <div className="text-xs text-gray-300">{r.year} · {r.format}</div>
          </div>
        ))}
      </div>
      <div className="flex justify-center gap-2">
        <button className="px-4 py-2 rounded-xl border border-gray-200 text-sm text-gray-400">← Prev</button>
        {[1,2,3,4,5].map((n) => <button key={n} className={`w-9 h-9 rounded-xl text-sm ${n===1 ? "bg-gray-900 text-white" : "border border-gray-200 text-gray-500 hover:border-gray-400"}`}>{n}</button>)}
        <button className="px-4 py-2 rounded-xl border border-gray-200 text-sm text-gray-400">Next →</button>
      </div>
    </div>
  );
}

function D2Release() {
  return (
    <div className="space-y-8">
      <button className="text-sm text-gray-400 hover:text-gray-700 transition-colors">← Collection</button>
      <div className="grid lg:grid-cols-3 gap-10">
        <div className="space-y-5">
          <div className="aspect-square bg-gray-50 border border-gray-100 rounded-3xl flex items-center justify-center text-8xl shadow-sm">💿</div>
          <div className="flex gap-2">
            <button className="flex-1 bg-gray-900 text-white py-3 rounded-xl text-sm font-semibold">▶ Mark as Played</button>
            <button className="w-11 h-11 border border-gray-200 rounded-xl flex items-center justify-center text-gray-400 hover:text-gray-700 hover:border-gray-400 transition-colors">✎</button>
            <button className="w-11 h-11 border border-red-100 rounded-xl flex items-center justify-center text-red-300 hover:text-red-500 hover:border-red-200 transition-colors">🗑</button>
          </div>
          {[
            { title: "Release Info", items: [["Format", RD.format], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "Purchase", items: [["Store", RD.purchase.store], ["Price", RD.purchase.price], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
            { title: "Collection", items: [["Added", RD.dateAdded], ["Modified", RD.lastModified], ["Last Played", RD.lastPlayed]] },
          ].map(({ title, items }) => (
            <div key={title} className="rounded-xl border border-gray-100 p-4">
              <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">{title}</h3>
              {items.map(([k, v]) => (
                <div key={k} className="flex justify-between py-1.5 text-sm border-b border-gray-50 last:border-0">
                  <span className="text-gray-400">{k}</span>
                  <span className="text-gray-800 font-medium text-right max-w-xs truncate">{v}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="lg:col-span-2 space-y-6">
          <div>
            <p className="text-sm text-gray-400 font-medium mb-2">{RD.artist}</p>
            <h1 className="text-5xl font-bold text-gray-900 tracking-tight leading-none">{RD.title}</h1>
            <p className="text-gray-400 mt-2">{RD.year} · {RD.genres.join(", ")}</p>
          </div>
          <div className="rounded-2xl border border-gray-100 overflow-hidden">
            <div className="px-5 py-3 bg-gray-50 border-b border-gray-100">
              <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wide">Tracklist</h3>
            </div>
            {RD.tracks.map((t, i) => (
              <div key={t.pos} className={`flex items-center px-5 py-3 hover:bg-gray-50 cursor-pointer transition-colors ${i>0 ? "border-t border-gray-50" : ""}`}>
                <span className="text-xs text-gray-300 w-8">{t.pos}</span>
                <span className="flex-1 text-sm text-gray-800">{t.title}</span>
                <span className="text-xs text-gray-400">{t.dur}</span>
              </div>
            ))}
          </div>
          <div className="rounded-xl border border-gray-100 p-4">
            <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2">Notes</h3>
            <p className="text-sm text-gray-600 italic">{RD.purchase.notes}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function D2Add({ addTab, setAddTab }: { addTab: "manual"|"discogs"; setAddTab: (t:"manual"|"discogs")=>void }) {
  return (
    <div className="max-w-2xl space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Add Release</h1>
        <p className="text-gray-400 mt-1">Add a new music release to your collection</p>
      </div>
      <div className="border-b border-gray-100 flex gap-6">
        {(["discogs","manual"] as const).map((t) => (
          <button key={t} onClick={() => setAddTab(t)}
            className={`pb-3 text-sm font-medium border-b-2 -mb-px transition-colors ${addTab===t ? "border-gray-900 text-gray-900" : "border-transparent text-gray-400 hover:text-gray-600"}`}>
            {t === "discogs" ? "Search Discogs" : "Manual Entry"}
          </button>
        ))}
      </div>
      {addTab === "discogs" ? (
        <div className="space-y-4">
          <div className="flex gap-3">
            <input readOnly type="text" placeholder="Artist, album, barcode..." className="flex-1 bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-sm focus:outline-none" />
            <button className="bg-gray-900 text-white px-5 rounded-xl text-sm font-medium">Search</button>
          </div>
          <div className="bg-gray-50 rounded-2xl p-12 text-center text-gray-400">
            <p className="text-4xl mb-3">��</p>
            <p className="text-sm font-medium">Search Discogs to import a release</p>
          </div>
        </div>
      ) : (
        <div className="space-y-5">
          <div className="grid grid-cols-2 gap-4">
            {RELEASE_FORM_FIELDS.map(([lbl, ph]) => (
              <div key={lbl}>
                <label className="block text-xs font-medium text-gray-500 mb-1">{lbl}</label>
                <input readOnly type="text" placeholder={ph} className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none" />
              </div>
            ))}
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Format</label>
              <select className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm"><option>12&quot; Vinyl LP</option><option>CD</option><option>Cassette</option></select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Genre</label>
              <select className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm"><option>Alternative Rock</option><option>Electronic</option><option>Jazz</option></select>
            </div>
          </div>
          <div className="border-t border-gray-100 pt-5">
            <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-4">Purchase Information</h3>
            <div className="grid grid-cols-2 gap-4">
              {PURCHASE_FORM_FIELDS.map(([lbl, ph]) => (
                <div key={lbl}>
                  <label className="block text-xs font-medium text-gray-500 mb-1">{lbl}</label>
                  <input readOnly type="text" placeholder={ph} className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none" />
                </div>
              ))}
            </div>
          </div>
          <div className="flex gap-3">
            <button className="flex-1 bg-gray-900 text-white py-3 rounded-xl text-sm font-semibold">Add to Collection</button>
            <button className="px-5 py-3 border border-gray-200 text-gray-500 rounded-xl text-sm hover:border-gray-400 transition-colors">Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}

/* ══════════════════════════════════════════════
   DESIGN 3 — WARM CRATE (Vintage / Record store)
   Cream bg, amber accents, serif typography
══════════════════════════════════════════════ */
function Design3({ activeTab, setActiveTab, addTab, setAddTab }: DesignProps) {
  return (
    <div className="min-h-screen bg-[#F6F0E6]" style={{ fontFamily: "Georgia, 'Times New Roman', serif" }}>
      <header className="bg-[#1A1008] px-8 py-4 flex items-center justify-between">
        <div>
          <span className="text-xl font-bold tracking-widest text-[#E8C87A]">KOLLECTOR SKÜM</span>
          <span className="ml-3 text-xs text-[#A07840] font-sans uppercase tracking-widest">Record Collection</span>
        </div>
        <div className="flex items-center gap-3">
          <input readOnly type="text" placeholder="Search the crates..." className="bg-[#2A1C0A] border border-[#3A2A10] rounded px-3 py-2 text-sm text-[#E8C87A] placeholder-[#6A4A20] font-sans focus:outline-none w-48" />
          <div className="w-8 h-8 rounded-full bg-[#B45309] flex items-center justify-center text-sm font-bold text-white font-sans">U</div>
        </div>
      </header>
      <div className="bg-[#2A1C0A] border-b border-[#3A2A10] px-8">
        <nav className="flex">
          {PAGE_TABS.map((t) => (
            <button key={t.key} onClick={() => setActiveTab(t.key)}
              className={`px-5 py-3 text-sm border-b-2 transition-colors font-sans ${activeTab===t.key ? "border-[#E8C87A] text-[#E8C87A]" : "border-transparent text-[#8A6A30] hover:text-[#C8A850]"}`}>
              {t.label}
            </button>
          ))}
        </nav>
      </div>
      <main className="max-w-6xl mx-auto px-8 py-10">
        {activeTab === "dashboard" && <D3Dashboard />}
        {activeTab === "collection" && <D3Collection />}
        {activeTab === "release" && <D3Release />}
        {activeTab === "add" && <D3Add addTab={addTab} setAddTab={setAddTab} />}
      </main>
    </div>
  );
}

function D3Dashboard() {
  return (
    <div className="space-y-8">
      <div className="border-b-2 border-[#C8A850] pb-6">
        <h1 className="text-4xl font-bold text-[#1A1008]">Your Record Collection</h1>
        <p className="text-[#7A5A28] mt-2 font-sans text-sm">Organise and discover your music library</p>
        <div className="flex items-center gap-2 mt-3">
          <span className="w-2 h-2 rounded-full bg-green-600" />
          <span className="text-xs text-green-700 font-sans font-medium">System Online · v2.1.0</span>
        </div>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[{label:"Releases",value:STATS.releases,icon:"🎵"},{label:"Artists",value:STATS.artists,icon:"👤"},{label:"Genres",value:STATS.genres,icon:"🏷️"},{label:"Labels",value:STATS.labels,icon:"🏢"}].map((s) => (
          <div key={s.label} className="bg-[#FEFCF5] rounded-lg border-2 border-[#DDD0B0] p-6 text-center shadow-sm">
            <div className="text-3xl mb-2">{s.icon}</div>
            <div className="text-3xl font-bold text-[#1A1008]">{s.value.toLocaleString()}</div>
            <div className="text-xs text-[#8A6A30] font-sans uppercase tracking-widest mt-1">{s.label}</div>
          </div>
        ))}
      </div>
      <div>
        <h2 className="text-lg font-bold text-[#1A1008] mb-4 border-b border-[#DDD0B0] pb-2">Quick Actions</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          {QA.map((a) => (
            <div key={a.title} className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-4 hover:border-[#B45309] cursor-pointer transition-all group">
              <div className="text-2xl mb-2">{a.icon}</div>
              <div className="text-sm font-bold text-[#1A1008] font-sans group-hover:text-[#B45309] transition-colors">{a.title}</div>
              <div className="text-xs text-[#8A6A30] font-sans mt-1">{a.desc}</div>
            </div>
          ))}
        </div>
      </div>
      <div>
        <h2 className="text-lg font-bold text-[#1A1008] mb-4 border-b border-[#DDD0B0] pb-2">Recently Played</h2>
        <div className="space-y-2">
          {RP.map((item, i) => (
            <div key={item.id} className="flex items-center gap-4 bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] px-4 py-3 hover:border-[#B45309] cursor-pointer transition-colors">
              <span className="text-[#C8A850] font-bold w-5 text-center font-sans">{i+1}</span>
              <div className="w-10 h-10 rounded bg-[#EDE0C0] flex items-center justify-center text-lg flex-shrink-0">💿</div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-bold text-[#1A1008] truncate">{item.title}</div>
                <div className="text-xs text-[#7A5A28] font-sans">{item.artist}</div>
              </div>
              <div className="text-xs text-[#A09070] font-sans flex-shrink-0">{item.playedAt}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-8 text-center">
        <div className="text-4xl mb-3">⏱️</div>
        <p className="font-bold text-[#1A1008]">Activity tracking coming soon</p>
        <p className="text-sm text-[#8A6A30] font-sans mt-1">View your recent collection updates and changes here.</p>
      </div>
      <div className="text-center">
        <p className="text-xs text-[#A09070] font-sans">Powered by Kollector API v2.1.0 · Last sync: just now</p>
      </div>
    </div>
  );
}

function D3Collection() {
  return (
    <div className="space-y-5">
      <div className="flex gap-3 flex-wrap">
        <input readOnly type="text" placeholder="Search the crates..." className="flex-1 min-w-64 bg-[#FEFCF5] border-2 border-[#DDD0B0] rounded px-4 py-2.5 text-sm font-sans text-[#1A1008] placeholder-[#B0A080] focus:outline-none focus:border-[#B45309]" />
        <select className="bg-[#FEFCF5] border-2 border-[#DDD0B0] rounded px-4 py-2.5 text-sm font-sans text-[#1A1008] focus:outline-none">
          <option>Sort: Date Added</option><option>Title</option><option>Artist</option>
        </select>
        <button className="bg-[#B45309] text-white px-5 py-2.5 rounded text-sm font-sans font-medium">Filters</button>
      </div>
      <div className="flex items-center gap-2 text-xs font-sans text-[#8A6A30]">
        <span>247 releases</span>
        <span className="bg-[#EDE0C0] text-[#7A4A10] px-2 py-0.5 rounded font-medium">Genre: Alternative Rock ×</span>
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {RELEASES.map((r) => (
          <div key={r.id} className="group cursor-pointer">
            <div className="aspect-square bg-[#EDE0C0] rounded-lg border-2 border-[#DDD0B0] group-hover:border-[#B45309] transition-colors mb-2 flex items-center justify-center text-4xl">💿</div>
            <div className="text-xs font-bold text-[#1A1008] truncate">{r.title}</div>
            <div className="text-xs text-[#7A5A28] font-sans truncate">{r.artist}</div>
            <div className="text-xs text-[#A09070] font-sans">{r.year} · {r.format}</div>
          </div>
        ))}
      </div>
      <div className="flex justify-center gap-2">
        <button className="px-4 py-2 rounded border-2 border-[#DDD0B0] text-sm font-sans text-[#8A6A30]">← Prev</button>
        {[1,2,3,4,5].map((n) => <button key={n} className={`w-9 h-9 rounded font-sans text-sm border-2 ${n===1 ? "bg-[#B45309] border-[#B45309] text-white" : "border-[#DDD0B0] text-[#7A5A28]"}`}>{n}</button>)}
        <button className="px-4 py-2 rounded border-2 border-[#DDD0B0] text-sm font-sans text-[#8A6A30]">Next →</button>
      </div>
    </div>
  );
}

function D3Release() {
  return (
    <div className="space-y-6">
      <button className="text-sm font-sans text-[#7A5A28] hover:text-[#B45309] transition-colors">← Back to Collection</button>
      <div className="grid lg:grid-cols-3 gap-8">
        <div className="space-y-4">
          <div className="aspect-square bg-[#EDE0C0] rounded-lg border-2 border-[#DDD0B0] flex items-center justify-center text-8xl shadow-md">💿</div>
          <div className="flex gap-2">
            <button className="flex-1 bg-[#B45309] text-white py-3 rounded font-sans text-sm font-semibold">▶ Mark as Played</button>
            <button className="w-11 h-11 border-2 border-[#DDD0B0] rounded flex items-center justify-center text-[#7A5A28] font-sans">✎</button>
            <button className="w-11 h-11 border-2 border-red-200 rounded flex items-center justify-center text-red-400 font-sans">🗑</button>
          </div>
          {[
            { title: "Release Info", items: [["Format", RD.format], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "Purchase Info", items: [["Store", RD.purchase.store], ["Price", RD.purchase.price], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
            { title: "Collection Data", items: [["Added", RD.dateAdded], ["Modified", RD.lastModified], ["Last Played", RD.lastPlayed]] },
          ].map(({ title, items }) => (
            <div key={title} className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-4">
              <h3 className="text-xs font-sans font-bold text-[#8A6A30] uppercase tracking-widest mb-3">{title}</h3>
              {items.map(([k, v]) => (
                <div key={k} className="flex justify-between text-sm py-1.5 border-b border-[#EDE0C0] last:border-0">
                  <span className="text-[#7A5A28] font-sans">{k}</span>
                  <span className="text-[#1A1008] font-semibold font-sans text-right max-w-xs truncate">{v}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="lg:col-span-2 space-y-6">
          <div className="border-b-2 border-[#C8A850] pb-4">
            <p className="text-sm text-[#B45309] font-sans font-bold uppercase tracking-widest mb-1">{RD.artist}</p>
            <h1 className="text-4xl font-bold text-[#1A1008] leading-tight">{RD.title}</h1>
            <p className="text-[#7A5A28] font-sans mt-2">{RD.year} · {RD.genres.join(", ")}</p>
          </div>
          <div className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] overflow-hidden">
            <div className="px-4 py-3 bg-[#EDE0C0] border-b border-[#DDD0B0]">
              <h3 className="text-xs font-sans font-bold text-[#7A5A28] uppercase tracking-widest">Tracklist</h3>
            </div>
            {RD.tracks.map((t, i) => (
              <div key={t.pos} className={`flex items-center px-4 py-3 hover:bg-[#EDE0C0] cursor-pointer ${i>0 ? "border-t border-[#EDE0C0]" : ""}`}>
                <span className="text-xs text-[#C8A850] font-sans font-bold w-8">{t.pos}</span>
                <span className="flex-1 text-sm text-[#1A1008]">{t.title}</span>
                <span className="text-xs text-[#8A6A30] font-sans">{t.dur}</span>
              </div>
            ))}
          </div>
          <div className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-4">
            <h3 className="text-xs font-sans font-bold text-[#8A6A30] uppercase tracking-widest mb-2">Notes</h3>
            <p className="text-sm text-[#5A3A18] italic">{RD.purchase.notes}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function D3Add({ addTab, setAddTab }: { addTab: "manual"|"discogs"; setAddTab: (t:"manual"|"discogs")=>void }) {
  return (
    <div className="max-w-2xl space-y-6">
      <div className="border-b-2 border-[#C8A850] pb-4">
        <h1 className="text-2xl font-bold text-[#1A1008]">Add to Crates</h1>
        <p className="text-[#7A5A28] font-sans mt-1 text-sm">Add a new music release to your collection</p>
      </div>
      <div className="flex gap-3">
        {(["discogs","manual"] as const).map((t) => (
          <button key={t} onClick={() => setAddTab(t)}
            className={`px-5 py-2 rounded border-2 text-sm font-sans font-semibold transition-colors ${addTab===t ? "bg-[#B45309] border-[#B45309] text-white" : "border-[#DDD0B0] text-[#7A5A28] hover:border-[#B45309]"}`}>
            {t === "discogs" ? "🔎 Search Discogs" : "✏️ Manual Entry"}
          </button>
        ))}
      </div>
      {addTab === "discogs" ? (
        <div className="space-y-4">
          <div className="flex gap-3">
            <input readOnly type="text" placeholder="Artist, album, barcode..." className="flex-1 bg-[#FEFCF5] border-2 border-[#DDD0B0] rounded px-4 py-3 text-sm font-sans focus:outline-none focus:border-[#B45309]" />
            <button className="bg-[#B45309] text-white px-5 rounded text-sm font-sans font-semibold">Search</button>
          </div>
          <div className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-12 text-center">
            <p className="text-4xl mb-3">🔍</p>
            <p className="text-sm font-sans font-semibold text-[#7A5A28]">Search Discogs to import a release</p>
          </div>
        </div>
      ) : (
        <div className="bg-[#FEFCF5] rounded-lg border border-[#DDD0B0] p-6 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {RELEASE_FORM_FIELDS.map(([lbl, ph]) => (
              <div key={lbl}>
                <label className="block text-xs font-sans font-semibold text-[#7A5A28] mb-1">{lbl}</label>
                <input readOnly type="text" placeholder={ph} className="w-full bg-white border-2 border-[#DDD0B0] rounded px-3 py-2.5 text-sm font-sans focus:outline-none focus:border-[#B45309]" />
              </div>
            ))}
            <div>
              <label className="block text-xs font-sans font-semibold text-[#7A5A28] mb-1">Format</label>
              <select className="w-full bg-white border-2 border-[#DDD0B0] rounded px-3 py-2.5 text-sm font-sans"><option>12&quot; Vinyl LP</option><option>CD</option></select>
            </div>
            <div>
              <label className="block text-xs font-sans font-semibold text-[#7A5A28] mb-1">Genre</label>
              <select className="w-full bg-white border-2 border-[#DDD0B0] rounded px-3 py-2.5 text-sm font-sans"><option>Alternative Rock</option><option>Electronic</option></select>
            </div>
          </div>
          <div className="border-t border-[#DDD0B0] pt-4">
            <h3 className="text-xs font-sans font-bold text-[#8A6A30] uppercase tracking-widest mb-3">Purchase Information</h3>
            <div className="grid grid-cols-2 gap-4">
              {PURCHASE_FORM_FIELDS.map(([lbl, ph]) => (
                <div key={lbl}>
                  <label className="block text-xs font-sans font-semibold text-[#7A5A28] mb-1">{lbl}</label>
                  <input readOnly type="text" placeholder={ph} className="w-full bg-white border-2 border-[#DDD0B0] rounded px-3 py-2.5 text-sm font-sans focus:outline-none focus:border-[#B45309]" />
                </div>
              ))}
            </div>
          </div>
          <div className="flex gap-3">
            <button className="flex-1 bg-[#B45309] text-white py-3 rounded font-sans text-sm font-semibold">Add to Collection</button>
            <button className="px-6 py-3 border-2 border-[#DDD0B0] text-[#7A5A28] rounded text-sm font-sans">Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}

/* ══════════════════════════════════════════════
   DESIGN 4 — BOLD EDITORIAL (Magazine / High contrast)
   White bg, bold black elements, red accent
══════════════════════════════════════════════ */
function Design4({ activeTab, setActiveTab, addTab, setAddTab }: DesignProps) {
  return (
    <div className="min-h-screen bg-white text-black" style={{ fontFamily: "'Helvetica Neue', Helvetica, Arial, sans-serif" }}>
      <header className="border-b-4 border-black px-8 py-5 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <span className="text-2xl font-black tracking-tighter">KOLLECTOR SKÜM</span>
          <div className="hidden md:flex gap-6 border-l-2 border-black pl-6">
            {PAGE_TABS.map((t) => (
              <button key={t.key} onClick={() => setActiveTab(t.key)}
                className={`text-sm font-black tracking-tight uppercase transition-colors ${activeTab===t.key ? "text-[#DC2626]" : "text-black hover:text-[#DC2626]"}`}>
                {t.label}
              </button>
            ))}
          </div>
        </div>
        <div className="flex items-center gap-3">
          <input readOnly type="text" placeholder="SEARCH" className="border-b-2 border-black bg-transparent px-2 py-1 text-xs font-black tracking-widest placeholder-black/40 focus:outline-none w-32" />
          <div className="w-8 h-8 bg-black text-white flex items-center justify-center text-xs font-black">U</div>
        </div>
      </header>
      <main className="max-w-7xl mx-auto px-8 py-10">
        {activeTab === "dashboard" && <D4Dashboard />}
        {activeTab === "collection" && <D4Collection />}
        {activeTab === "release" && <D4Release />}
        {activeTab === "add" && <D4Add addTab={addTab} setAddTab={setAddTab} />}
      </main>
    </div>
  );
}

function D4Dashboard() {
  return (
    <div className="space-y-10">
      <div className="border-b-4 border-black pb-8 flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-black uppercase tracking-widest text-[#DC2626] mb-2">— Dashboard</p>
          <h1 className="text-6xl md:text-8xl font-black tracking-tighter leading-none">YOUR<br />COLLECTION</h1>
          <p className="text-gray-500 mt-4 font-medium">Organise and discover your music library</p>
        </div>
        <div className="text-right">
          <div className="flex items-center gap-2 justify-end">
            <span className="w-3 h-3 rounded-full bg-green-500" />
            <span className="text-sm font-black">ONLINE</span>
          </div>
          <p className="text-xs text-gray-400 mt-1">v2.1.0 · Synced just now</p>
        </div>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 border-2 border-black divide-x-2 divide-black">
        {[{label:"RELEASES",value:STATS.releases},{label:"ARTISTS",value:STATS.artists},{label:"GENRES",value:STATS.genres},{label:"LABELS",value:STATS.labels}].map((s) => (
          <div key={s.label} className="p-8">
            <div className="text-5xl font-black text-[#DC2626]">{s.value}</div>
            <div className="text-xs font-black tracking-widest mt-2 text-gray-500">{s.label}</div>
          </div>
        ))}
      </div>
      <div>
        <h2 className="text-xs font-black tracking-widest mb-6 flex items-center gap-3">QUICK ACTIONS <span className="flex-1 h-px bg-black" /></h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 border-t-2 border-l-2 border-black">
          {QA.map((a) => (
            <div key={a.title} className="p-5 border-b-2 border-r-2 border-black hover:bg-black hover:text-white cursor-pointer transition-colors group">
              <div className="text-2xl mb-2">{a.icon}</div>
              <div className="text-sm font-black">{a.title}</div>
              <div className="text-xs text-gray-500 group-hover:text-gray-300 mt-1">{a.desc}</div>
            </div>
          ))}
        </div>
      </div>
      <div>
        <h2 className="text-xs font-black tracking-widest mb-6 flex items-center gap-3">RECENTLY PLAYED <span className="flex-1 h-px bg-black" /></h2>
        <div className="border-2 border-black divide-y-2 divide-black">
          {RP.map((item, i) => (
            <div key={item.id} className="flex items-center gap-4 px-5 py-4 hover:bg-black hover:text-white cursor-pointer transition-colors group">
              <span className="text-2xl font-black text-[#DC2626] group-hover:text-white w-8">{String(i+1).padStart(2,"0")}</span>
              <div className="w-10 h-10 border-2 border-current flex items-center justify-center flex-shrink-0">💿</div>
              <div className="flex-1 min-w-0">
                <div className="font-black truncate">{item.title}</div>
                <div className="text-sm text-gray-500 group-hover:text-gray-300">{item.artist}</div>
              </div>
              <div className="text-xs font-bold text-gray-400 group-hover:text-gray-300 flex-shrink-0">{item.playedAt}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="border-2 border-black p-8 text-center">
        <div className="text-4xl mb-3">⏱️</div>
        <p className="font-black text-lg">ACTIVITY TRACKING COMING SOON</p>
        <p className="text-sm text-gray-500 mt-1">View your recent collection updates and changes here.</p>
      </div>
      <p className="text-center text-xs text-gray-400 font-bold">POWERED BY KOLLECTOR API v2.1.0 · LAST SYNC: JUST NOW</p>
    </div>
  );
}

function D4Collection() {
  return (
    <div className="space-y-6">
      <div className="flex gap-0 flex-wrap border-2 border-black">
        <input readOnly type="text" placeholder="SEARCH RELEASES..." className="flex-1 min-w-64 border-r-2 border-black px-4 py-3 text-sm font-black tracking-wider placeholder-black/40 focus:outline-none bg-transparent" />
        <select className="border-r-2 border-black px-4 py-3 text-sm font-black bg-transparent focus:outline-none">
          <option>DATE ADDED</option><option>TITLE</option><option>ARTIST</option><option>YEAR</option>
        </select>
        <button className="bg-[#DC2626] text-white px-6 py-3 text-xs font-black tracking-widest">FILTERS</button>
      </div>
      <div className="flex items-center gap-3 text-xs font-black">
        <span>247 RELEASES</span>
        <span className="bg-black text-white px-3 py-1 tracking-widest">GENRE: ALT ROCK ×</span>
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {RELEASES.map((r) => (
          <div key={r.id} className="group cursor-pointer">
            <div className="aspect-square bg-gray-100 border-2 border-black group-hover:bg-black group-hover:border-[#DC2626] transition-all mb-2 flex items-center justify-center text-4xl">💿</div>
            <div className="text-xs font-black truncate">{r.title}</div>
            <div className="text-xs text-gray-500 font-bold truncate">{r.artist}</div>
            <div className="text-xs text-gray-400 font-bold">{r.year} · {r.format}</div>
          </div>
        ))}
      </div>
      <div className="flex justify-center border-2 border-black divide-x-2 divide-black w-fit mx-auto">
        <button className="px-5 py-2 text-sm font-black hover:bg-black hover:text-white transition-colors">← PREV</button>
        {[1,2,3,4,5].map((n) => <button key={n} className={`w-10 h-10 text-sm font-black transition-colors ${n===1 ? "bg-[#DC2626] text-white" : "hover:bg-black hover:text-white"}`}>{n}</button>)}
        <button className="px-5 py-2 text-sm font-black hover:bg-black hover:text-white transition-colors">NEXT →</button>
      </div>
    </div>
  );
}

function D4Release() {
  return (
    <div className="space-y-6">
      <button className="text-xs font-black tracking-widest hover:text-[#DC2626] transition-colors">← BACK TO COLLECTION</button>
      <div className="grid lg:grid-cols-3 border-2 border-black">
        <div className="border-r-2 border-black p-6 space-y-4">
          <div className="aspect-square bg-gray-100 border-2 border-black flex items-center justify-center text-8xl">💿</div>
          <div className="flex border-2 border-black divide-x-2 divide-black">
            <button className="flex-1 bg-[#DC2626] text-white py-3 text-xs font-black tracking-widest">▶ PLAYED</button>
            <button className="w-12 flex items-center justify-center hover:bg-black hover:text-white transition-colors">✎</button>
            <button className="w-12 flex items-center justify-center text-red-500 hover:bg-red-600 hover:text-white transition-colors">🗑</button>
          </div>
          {[
            { title: "RELEASE", items: [["Format", RD.format], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "PURCHASE", items: [["Store", RD.purchase.store], ["Price", RD.purchase.price], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
            { title: "COLLECTION", items: [["Added", RD.dateAdded], ["Modified", RD.lastModified], ["Last Played", RD.lastPlayed]] },
          ].map(({ title, items }) => (
            <div key={title} className="border-2 border-black">
              <div className="bg-black text-white px-3 py-1.5 text-xs font-black tracking-widest">{title}</div>
              {items.map(([k, v]) => (
                <div key={k} className="flex justify-between px-3 py-2 border-t border-black/10 text-xs">
                  <span className="text-gray-500 font-bold">{k}</span>
                  <span className="font-black text-right max-w-xs truncate">{v}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="lg:col-span-2 p-8 space-y-6">
          <div className="border-b-4 border-black pb-6">
            <p className="text-xs font-black tracking-widest text-[#DC2626] mb-2">{RD.artist.toUpperCase()}</p>
            <h1 className="text-5xl font-black tracking-tighter leading-none">{RD.title.toUpperCase()}</h1>
            <p className="font-bold mt-3 text-gray-600">{RD.year} · {RD.genres.join(" / ")}</p>
          </div>
          <div>
            <h3 className="text-xs font-black tracking-widest mb-3 flex items-center gap-3">TRACKLIST <span className="flex-1 h-0.5 bg-black" /></h3>
            <div className="border-2 border-black divide-y divide-black/10">
              {RD.tracks.map((t, i) => (
                <div key={t.pos} className={`flex items-center px-4 py-3 hover:bg-black hover:text-white transition-colors cursor-pointer`}>
                  <span className="text-xs font-black text-[#DC2626] w-8">{t.pos}</span>
                  <span className="flex-1 text-sm font-bold">{t.title}</span>
                  <span className="text-xs font-bold text-gray-400">{t.dur}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="bg-gray-50 border-2 border-black p-4">
            <h3 className="text-xs font-black tracking-widest mb-2">NOTES</h3>
            <p className="text-sm text-gray-600">{RD.purchase.notes}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function D4Add({ addTab, setAddTab }: { addTab: "manual"|"discogs"; setAddTab: (t:"manual"|"discogs")=>void }) {
  return (
    <div className="max-w-2xl space-y-6">
      <div className="border-b-4 border-black pb-6">
        <p className="text-xs font-black tracking-widest text-[#DC2626] mb-2">— ADD RELEASE</p>
        <h1 className="text-4xl font-black tracking-tighter">ADD TO COLLECTION</h1>
      </div>
      <div className="flex border-2 border-black divide-x-2 divide-black w-fit">
        {(["discogs","manual"] as const).map((t) => (
          <button key={t} onClick={() => setAddTab(t)}
            className={`px-6 py-3 text-xs font-black tracking-widest transition-colors ${addTab===t ? "bg-black text-white" : "hover:bg-gray-100"}`}>
            {t === "discogs" ? "SEARCH DISCOGS" : "MANUAL ENTRY"}
          </button>
        ))}
      </div>
      {addTab === "discogs" ? (
        <div className="space-y-4">
          <div className="flex border-2 border-black">
            <input readOnly type="text" placeholder="ARTIST, ALBUM, BARCODE..." className="flex-1 border-r-2 border-black px-4 py-3 text-sm font-black placeholder-black/30 focus:outline-none tracking-wider" />
            <button className="bg-[#DC2626] text-white px-6 text-xs font-black tracking-widest">SEARCH</button>
          </div>
          <div className="border-2 border-black p-12 text-center">
            <p className="text-4xl mb-3">🔍</p>
            <p className="text-sm font-black tracking-wide">SEARCH DISCOGS TO IMPORT A RELEASE</p>
          </div>
        </div>
      ) : (
        <div className="border-2 border-black">
          <div className="grid grid-cols-2 border-b-2 border-black">
            {[["TITLE *","OK Computer"],["ARTIST *","Radiohead"],["RELEASE YEAR","1997"],["ORIGINAL YEAR","1997"],["LABEL","Parlophone"],["CATALOGUE #","NODATA 02"],["COUNTRY","United Kingdom"],["UPC","0724384568924"]].map(([lbl,ph],i) => (
              <div key={lbl} className={`p-4 border-b border-r border-black/20 ${i%2===1 ? "border-r-0" : ""}`}>
                <label className="block text-xs font-black tracking-widest text-gray-500 mb-1">{lbl}</label>
                <input readOnly type="text" placeholder={ph} className="w-full text-sm font-bold focus:outline-none bg-transparent placeholder-gray-300" />
              </div>
            ))}
          </div>
          <div className="grid grid-cols-2 border-b-2 border-black divide-x-2 divide-black">
            <div className="p-4"><label className="block text-xs font-black tracking-widest text-gray-500 mb-1">FORMAT</label><select className="w-full text-sm font-bold bg-transparent focus:outline-none"><option>12&quot; Vinyl LP</option><option>CD</option></select></div>
            <div className="p-4"><label className="block text-xs font-black tracking-widest text-gray-500 mb-1">GENRE</label><select className="w-full text-sm font-bold bg-transparent focus:outline-none"><option>Alternative Rock</option><option>Electronic</option></select></div>
          </div>
          <div className="p-4 border-b-2 border-black">
            <p className="text-xs font-black tracking-widest text-gray-500 mb-3">PURCHASE INFORMATION</p>
            <div className="grid grid-cols-2 gap-4">
              {[["STORE","Rough Trade"],["PRICE","22.99"],["DATE",""],["CONDITION","Near Mint"]].map(([lbl,ph]) => (
                <div key={lbl}><label className="block text-xs font-black tracking-widest text-gray-500 mb-1">{lbl}</label><input readOnly type="text" placeholder={ph} className="w-full text-sm font-bold border-b-2 border-black focus:outline-none bg-transparent pb-1 placeholder-gray-300" /></div>
              ))}
            </div>
          </div>
          <div className="flex p-4 gap-3">
            <button className="flex-1 bg-[#DC2626] text-white py-3 text-xs font-black tracking-widest">ADD TO COLLECTION</button>
            <button className="border-2 border-black px-6 text-xs font-black tracking-widest hover:bg-gray-100 transition-colors">CANCEL</button>
          </div>
        </div>
      )}
    </div>
  );
}

/* ══════════════════════════════════════════════
   DESIGN 5 — NEO GLOW (Futuristic / Glassmorphism)
   Very dark bg, cyan/purple neon, blur effects
══════════════════════════════════════════════ */

/** Base glassmorphism card style used throughout Design 5. */
const glassStyle: React.CSSProperties = {
  background: "rgba(255,255,255,0.03)",
  border: "1px solid rgba(255,255,255,0.07)",
  backdropFilter: "blur(10px)",
};

function Design5({ activeTab, setActiveTab, addTab, setAddTab }: DesignProps) {
  return (
    <div className="min-h-screen text-white relative overflow-hidden"
      style={{ background: "linear-gradient(135deg, #050510 0%, #0A0520 50%, #050510 100%)", fontFamily: "Inter, sans-serif" }}>
      <div className="fixed top-0 left-1/4 w-96 h-96 rounded-full pointer-events-none"
        style={{ background: "radial-gradient(circle, rgba(0,212,255,0.06) 0%, transparent 70%)", filter: "blur(40px)" }} />
      <div className="fixed bottom-0 right-1/4 w-96 h-96 rounded-full pointer-events-none"
        style={{ background: "radial-gradient(circle, rgba(123,47,255,0.05) 0%, transparent 70%)", filter: "blur(40px)" }} />
      <header className="relative px-8 py-4 flex items-center justify-between border-b sticky top-0 z-10"
        style={{ borderColor: "rgba(0,212,255,0.15)", background: "rgba(5,5,20,0.85)", backdropFilter: "blur(20px)" }}>
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg flex items-center justify-center" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>
            <span className="text-white text-xs font-black">K</span>
          </div>
          <span className="text-sm font-bold tracking-wide" style={{ background: "linear-gradient(90deg, #00D4FF, #7B2FFF)", WebkitBackgroundClip: "text", WebkitTextFillColor: "transparent" }}>
            KOLLECTOR SKÜM
          </span>
        </div>
        <nav className="hidden md:flex gap-1">
          {PAGE_TABS.map((t) => (
            <button key={t.key} onClick={() => setActiveTab(t.key)}
              className={`px-4 py-2 rounded-lg text-xs font-semibold transition-all ${activeTab===t.key ? "text-white" : "text-gray-500 hover:text-gray-300"}`}
              style={activeTab===t.key ? { background: "rgba(0,212,255,0.12)", border: "1px solid rgba(0,212,255,0.25)" } : {}}>
              {t.label}
            </button>
          ))}
        </nav>
        <div className="flex items-center gap-3">
          <input readOnly type="text" placeholder="Search..." className="text-sm bg-transparent rounded-lg px-3 py-2 text-gray-300 placeholder-gray-600 focus:outline-none w-36"
            style={{ border: "1px solid rgba(0,212,255,0.15)" }} />
          <div className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>U</div>
        </div>
      </header>
      <main className="max-w-7xl mx-auto px-6 py-8 relative z-10">
        {activeTab === "dashboard" && <D5Dashboard />}
        {activeTab === "collection" && <D5Collection />}
        {activeTab === "release" && <D5Release />}
        {activeTab === "add" && <D5Add addTab={addTab} setAddTab={setAddTab} />}
      </main>
    </div>
  );
}

function D5Dashboard() {
  return (
    <div className="space-y-8">
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <p className="text-xs font-bold tracking-widest mb-2" style={{ color: "#00D4FF" }}>DASHBOARD</p>
          <h1 className="text-4xl font-black text-white tracking-tight">Your Collection</h1>
          <p className="text-gray-500 mt-1 text-sm">Organise and discover your music library</p>
          <div className="flex items-center gap-2 mt-3">
            <span className="w-2 h-2 rounded-full bg-emerald-400" style={{ boxShadow: "0 0 6px #10B981" }} />
            <span className="text-xs text-emerald-400 font-semibold">System Online · v2.1.0</span>
          </div>
        </div>
        <div className="text-right text-xs text-gray-600">Powered by Kollector API · Last sync: just now</div>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: "Releases", value: STATS.releases, c: "#00D4FF" },
          { label: "Artists", value: STATS.artists, c: "#7B2FFF" },
          { label: "Genres", value: STATS.genres, c: "#00FFB2" },
          { label: "Labels", value: STATS.labels, c: "#FF6B00" },
        ].map((s) => (
          <div key={s.label} className="rounded-2xl p-5 relative overflow-hidden" style={glassStyle}>
            <div className="absolute -top-4 -right-4 w-20 h-20 rounded-full opacity-20" style={{ background: s.c, filter: "blur(15px)" }} />
            <div className="text-3xl font-black" style={{ color: s.c }}>{s.value.toLocaleString()}</div>
            <div className="text-xs text-gray-500 mt-1 font-medium uppercase tracking-widest">{s.label}</div>
          </div>
        ))}
      </div>
      <div>
        <h2 className="text-xs font-bold tracking-widest mb-4" style={{ color: "#00D4FF" }}>QUICK ACTIONS</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          {QA.map((a) => (
            <div key={a.title} className="rounded-xl p-4 cursor-pointer transition-all hover:scale-[1.02]" style={{ ...glassStyle, border: "1px solid rgba(0,212,255,0.15)" }}>
              <div className="text-2xl mb-2">{a.icon}</div>
              <div className="text-sm font-semibold text-white">{a.title}</div>
              <div className="text-xs text-gray-500 mt-1">{a.desc}</div>
            </div>
          ))}
        </div>
      </div>
      <div>
        <h2 className="text-xs font-bold tracking-widest mb-4" style={{ color: "#00D4FF" }}>RECENTLY PLAYED</h2>
        <div className="space-y-2">
          {RP.map((item, i) => (
            <div key={item.id} className="flex items-center gap-4 rounded-xl px-4 py-3 cursor-pointer" style={glassStyle}>
              <span className="text-xs font-bold w-5 text-center" style={{ color: "#00D4FF" }}>{i+1}</span>
              <div className="w-10 h-10 rounded-lg flex items-center justify-center text-lg flex-shrink-0" style={{ background: "rgba(0,212,255,0.08)" }}>💿</div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-semibold text-white truncate">{item.title}</div>
                <div className="text-xs text-gray-500">{item.artist}</div>
              </div>
              <div className="text-xs text-gray-600 flex-shrink-0">{item.playedAt}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="rounded-2xl p-6 text-center" style={glassStyle}>
        <div className="text-4xl mb-3">⏱️</div>
        <p className="font-semibold text-gray-400">Activity tracking coming soon</p>
        <p className="text-sm text-gray-600 mt-1">View your recent collection updates and changes here.</p>
      </div>
    </div>
  );
}

function D5Collection() {
  return (
    <div className="space-y-6">
      <div className="flex gap-3 flex-wrap">
        <input readOnly type="text" placeholder="Search releases, artists, albums..." className="flex-1 min-w-64 rounded-xl px-4 py-3 text-sm text-white placeholder-gray-600 focus:outline-none" style={glassStyle} />
        <select className="rounded-xl px-4 py-3 text-sm text-gray-300 focus:outline-none" style={glassStyle}>
          <option className="bg-gray-900">Date Added</option><option className="bg-gray-900">Title</option><option className="bg-gray-900">Artist</option>
        </select>
        <button className="px-5 py-3 rounded-xl text-sm font-semibold text-black" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>Filters</button>
      </div>
      <div className="flex items-center gap-3 text-xs text-gray-500">
        <span>247 releases</span>
        <span className="px-2 py-0.5 rounded-full text-xs font-medium" style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.25)" }}>Genre: Alternative Rock ×</span>
      </div>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {RELEASES.map((r) => (
          <div key={r.id} className="group cursor-pointer">
            <div className="aspect-square rounded-xl mb-2 flex items-center justify-center text-4xl transition-all group-hover:scale-105" style={glassStyle}>💿</div>
            <div className="text-xs font-semibold text-white truncate">{r.title}</div>
            <div className="text-xs text-gray-500 truncate">{r.artist}</div>
            <div className="text-xs text-gray-700">{r.year} · {r.format}</div>
          </div>
        ))}
      </div>
      <div className="flex justify-center items-center gap-2">
        <button className="px-4 py-2 rounded-lg text-sm text-gray-400" style={glassStyle}>← Prev</button>
        {[1,2,3,4,5].map((n) => (
          <button key={n} className="w-9 h-9 rounded-lg text-sm font-medium"
            style={n===1 ? { background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" } : glassStyle}>{n}</button>
        ))}
        <button className="px-4 py-2 rounded-lg text-sm text-gray-400" style={glassStyle}>Next →</button>
      </div>
    </div>
  );
}

function D5Release() {
  return (
    <div className="space-y-6">
      <button className="text-xs font-semibold tracking-widest transition-colors" style={{ color: "#00D4FF" }}>← COLLECTION</button>
      <div className="grid lg:grid-cols-3 gap-6">
        <div className="space-y-4">
          <div className="aspect-square rounded-2xl flex items-center justify-center text-8xl" style={glassStyle}>💿</div>
          <div className="flex gap-2">
            <button className="flex-1 py-3 rounded-xl text-sm font-semibold text-black" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>▶ Mark as Played</button>
            <button className="w-12 h-12 rounded-xl flex items-center justify-center text-gray-400 hover:text-white transition-colors" style={glassStyle}>✎</button>
            <button className="w-12 h-12 rounded-xl flex items-center justify-center text-red-400/60 hover:text-red-400 transition-colors" style={{ ...glassStyle, border: "1px solid rgba(239,68,68,0.2)" }}>🗑</button>
          </div>
          {[
            { title: "Release Info", items: [["Format", RD.format], ["Label", RD.label], ["Cat #", RD.labelNumber], ["Country", RD.country], ["UPC", RD.upc]] },
            { title: "Purchase Info", items: [["Store", RD.purchase.store], ["Price", RD.purchase.price], ["Date", RD.purchase.date], ["Condition", RD.purchase.condition]] },
            { title: "Collection", items: [["Added", RD.dateAdded], ["Modified", RD.lastModified], ["Last Played", RD.lastPlayed]] },
          ].map(({ title, items }) => (
            <div key={title} className="rounded-xl p-4" style={glassStyle}>
              <h3 className="text-xs font-bold tracking-widest mb-3" style={{ color: "#00D4FF" }}>{title.toUpperCase()}</h3>
              {items.map(([k, v]) => (
                <div key={k} className="flex justify-between text-sm py-1.5 border-b border-white/5 last:border-0">
                  <span className="text-gray-500">{k}</span>
                  <span className="text-white font-medium text-right max-w-xs truncate">{v}</span>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="lg:col-span-2 space-y-6">
          <div>
            <p className="text-xs font-bold tracking-widest mb-1" style={{ color: "#00D4FF" }}>{RD.artist.toUpperCase()}</p>
            <h1 className="text-4xl font-black text-white tracking-tight">{RD.title}</h1>
            <p className="text-gray-500 mt-1">{RD.year}</p>
            <div className="flex gap-2 mt-2">
              {RD.genres.map((g) => <span key={g} className="text-xs px-2 py-1 rounded-full" style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)" }}>{g}</span>)}
            </div>
          </div>
          <div className="rounded-2xl overflow-hidden" style={glassStyle}>
            <div className="px-5 py-3 border-b" style={{ borderColor: "rgba(255,255,255,0.05)" }}>
              <h3 className="text-xs font-bold tracking-widest" style={{ color: "#00D4FF" }}>TRACKLIST</h3>
            </div>
            {RD.tracks.map((t, i) => (
              <div key={t.pos} className="flex items-center px-5 py-3 cursor-pointer hover:bg-white/5 transition-colors"
                style={i>0 ? { borderTop: "1px solid rgba(255,255,255,0.04)" } : {}}>
                <span className="text-xs font-bold w-8" style={{ color: "#7B2FFF" }}>{t.pos}</span>
                <span className="flex-1 text-sm text-white">{t.title}</span>
                <span className="text-xs text-gray-600">{t.dur}</span>
              </div>
            ))}
          </div>
          <div className="rounded-xl p-4" style={glassStyle}>
            <h3 className="text-xs font-bold tracking-widest mb-2" style={{ color: "#00D4FF" }}>NOTES</h3>
            <p className="text-sm text-gray-400 italic">{RD.purchase.notes}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function D5Add({ addTab, setAddTab }: { addTab: "manual"|"discogs"; setAddTab: (t:"manual"|"discogs")=>void }) {
  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <p className="text-xs font-bold tracking-widest mb-1" style={{ color: "#00D4FF" }}>ADD RELEASE</p>
        <h1 className="text-2xl font-black text-white">Add to Collection</h1>
        <p className="text-gray-500 mt-1 text-sm">Add a new music release to your collection</p>
      </div>
      <div className="flex gap-1 p-1 rounded-xl w-fit" style={glassStyle}>
        {(["discogs","manual"] as const).map((t) => (
          <button key={t} onClick={() => setAddTab(t)}
            className="px-5 py-2 rounded-lg text-sm font-semibold transition-all"
            style={addTab===t ? { background: "linear-gradient(135deg, #00D4FF, #7B2FFF)", color: "#fff" } : { color: "#6B7280" }}>
            {t === "discogs" ? "🔎 Discogs" : "✏️ Manual"}
          </button>
        ))}
      </div>
      {addTab === "discogs" ? (
        <div className="space-y-4">
          <div className="flex gap-3">
            <input readOnly type="text" placeholder="Artist, album, barcode..." className="flex-1 rounded-xl px-4 py-3 text-sm text-white placeholder-gray-600 focus:outline-none" style={glassStyle} />
            <button className="px-5 rounded-xl text-sm font-semibold text-black" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>Search</button>
          </div>
          <div className="rounded-xl p-12 text-center" style={glassStyle}>
            <p className="text-4xl mb-3">🔍</p>
            <p className="text-sm text-gray-500">Search Discogs to import a release</p>
          </div>
        </div>
      ) : (
        <div className="rounded-2xl p-6 space-y-5" style={glassStyle}>
          <div className="grid grid-cols-2 gap-4">
            {RELEASE_FORM_FIELDS.map(([lbl, ph]) => (
              <div key={lbl}>
                <label className="block text-xs font-semibold text-gray-500 mb-1">{lbl}</label>
                <input readOnly type="text" placeholder={ph} className="w-full rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-700 focus:outline-none"
                  style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)" }} />
              </div>
            ))}
            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1">Format</label>
              <select className="w-full rounded-lg px-3 py-2.5 text-sm text-white focus:outline-none"
                style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)" }}>
                <option className="bg-gray-900">12&quot; Vinyl LP</option><option className="bg-gray-900">CD</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1">Genre</label>
              <select className="w-full rounded-lg px-3 py-2.5 text-sm text-white focus:outline-none"
                style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)" }}>
                <option className="bg-gray-900">Alternative Rock</option><option className="bg-gray-900">Electronic</option>
              </select>
            </div>
          </div>
          <div className="pt-3 border-t border-white/5">
            <h3 className="text-xs font-bold tracking-widest mb-3" style={{ color: "#00D4FF" }}>PURCHASE INFO</h3>
            <div className="grid grid-cols-2 gap-4">
              {PURCHASE_FORM_FIELDS.map(([lbl, ph]) => (
                <div key={lbl}>
                  <label className="block text-xs font-semibold text-gray-500 mb-1">{lbl}</label>
                  <input readOnly type="text" placeholder={ph} className="w-full rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-700 focus:outline-none"
                    style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)" }} />
                </div>
              ))}
            </div>
          </div>
          <div className="flex gap-3">
            <button className="flex-1 py-3 rounded-xl text-sm font-semibold text-black" style={{ background: "linear-gradient(135deg, #00D4FF, #7B2FFF)" }}>Add to Collection</button>
            <button className="px-6 py-3 rounded-xl text-sm text-gray-400 hover:text-white transition-colors" style={glassStyle}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}
