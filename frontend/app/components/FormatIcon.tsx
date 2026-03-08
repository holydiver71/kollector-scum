export function FormatIcon({ formatName, className }: { formatName?: string; className?: string }) {
  if (!formatName) return null;

  const name = formatName.toLowerCase();

  // Common styles
  const wrapperStyle = className || "absolute top-2 right-2 w-8 h-8 rounded-full shadow-lg drop-shadow-md z-10 bg-[#13131F]/50 flex items-center justify-center p-0.5";

  // CD Single: CD style with a prominent 'S'
  if (name.includes("cd single") || name.includes("maxi-cd") || name.includes("mini-cd") || (name.includes("cd") && name.includes("single"))) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
          <circle cx="50" cy="50" r="48" fill="url(#cd-single-gradient)" stroke="#9ca3af" strokeWidth="1"/>
          <text x="80" y="58" fontSize="32" textAnchor="middle" fill="#374151" fontWeight="bold" stroke="#374151" strokeWidth="1" fontFamily="sans-serif">S</text>
          <circle cx="50" cy="50" r="15" fill="#13131F" />
          <circle cx="50" cy="50" r="7" fill="transparent" stroke="#d1d5db" strokeWidth="2" />
          <defs>
            <linearGradient id="cd-single-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#e5e7eb" />
              <stop offset="33%" stopColor="#fca5a5" />
              <stop offset="66%" stopColor="#87ceeb" />
              <stop offset="100%" stopColor="#e5e7eb" />
            </linearGradient>
          </defs>
        </svg>
      </div>
    );
  }

  // CD-R / CD-ROM: Silver disc with a prominent 'R'
  if (name.includes("cd-r") || name.match(/\bcdr\b/) || name.includes("cdrom") || name.includes("cd-rom") || name.includes("cd rom")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
          <circle cx="50" cy="50" r="48" fill="url(#cdr-gradient)" stroke="#9ca3af" strokeWidth="1"/>
          <text x="80" y="58" fontSize="32" textAnchor="middle" fill="#374151" fontWeight="bold" stroke="#374151" strokeWidth="1" fontFamily="sans-serif">R</text>
          <circle cx="50" cy="50" r="15" fill="#13131F" />
          <circle cx="50" cy="50" r="7" fill="transparent" stroke="#d1d5db" strokeWidth="2" />
          <defs>
            <linearGradient id="cdr-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#e5e7eb" />
              <stop offset="33%" stopColor="#7dd3fc" />
              <stop offset="66%" stopColor="#93c5fd" />
              <stop offset="100%" stopColor="#e5e7eb" />
            </linearGradient>
          </defs>
        </svg>
      </div>
    );
  }

  // Standard CD: Silver disc with a small mirror hole
  if (name.includes("cd") || name.includes("compact disc")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
          <circle cx="50" cy="50" r="48" fill="url(#cd-gradient)" stroke="#9ca3af" strokeWidth="1"/>
          <circle cx="50" cy="50" r="15" fill="#13131F" />
          <circle cx="50" cy="50" r="7" fill="transparent" stroke="#d1d5db" strokeWidth="2" />
          <defs>
            <linearGradient id="cd-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#e5e7eb" />
              <stop offset="33%" stopColor="#d8b4e2" />
              <stop offset="66%" stopColor="#87ceeb" />
              <stop offset="100%" stopColor="#e5e7eb" />
            </linearGradient>
          </defs>
        </svg>
      </div>
    );
  }

  // Cassette: Rectangular body, two reels
  if (name.includes("cassette") || name.includes("tape")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 60" className="w-[110%] h-[110%] text-white">
          <rect x="5" y="5" width="90" height="50" rx="5" fill="#374151" stroke="#9ca3af" strokeWidth="1" />
          <rect x="20" y="20" width="60" height="20" rx="3" fill="#1f2937" />
          <circle cx="35" cy="30" r="6" fill="#13131F" stroke="#d1d5db" strokeWidth="1" />
          <circle cx="65" cy="30" r="6" fill="#13131F" stroke="#d1d5db" strokeWidth="1" />
          <path d="M 25 45 L 75 45 L 80 55 L 20 55 Z" fill="#4b5563" />
        </svg>
      </div>
    );
  }

  // 7" Single: Vinyl with large center hole
  if (name.includes("7\"") || name.includes("7 inch") || name.includes("7-inch") || name === "single") {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full">
          <circle cx="50" cy="50" r="48" fill="#111" stroke="#333" strokeWidth="1"/>
          {/* Grooves */}
          <circle cx="50" cy="50" r="40" fill="transparent" stroke="#222" strokeWidth="1"/>
          <circle cx="50" cy="50" r="32" fill="transparent" stroke="#222" strokeWidth="1"/>
          {/* Label */}
          <circle cx="50" cy="50" r="22" fill="#ef4444" />
          {/* Large hole typical for 7" */}
          <circle cx="50" cy="50" r="12" fill="#13131F" />
        </svg>
      </div>
    );
  }

  // 10" Vinyl: Between 7" and 12", medium hole, distinct label
  if (name.includes("10\"") || name.includes("10 inch") || name.includes("10-inch")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-[85%] h-[85%] m-auto text-transparent">
          <circle cx="50" cy="50" r="48" fill="#111" stroke="#333" strokeWidth="1"/>
          <circle cx="50" cy="50" r="40" fill="transparent" stroke="#222" strokeWidth="1"/>
          <circle cx="50" cy="50" r="30" fill="transparent" stroke="#222" strokeWidth="1"/>
          <circle cx="50" cy="50" r="18" fill="#3b82f6" />
          <circle cx="50" cy="50" r="3" fill="#13131F" />
        </svg>
      </div>
    );
  }

  // 12" Single: Large vinyl, maybe distinct from LP label
  if (name.includes("12\"") || name.includes("12 inch") || name.includes("12-inch") || name.includes("maxi")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
          <circle cx="50" cy="50" r="48" fill="#111" stroke="#333" strokeWidth="1"/>
          <circle cx="50" cy="50" r="35" fill="transparent" stroke="#222" strokeWidth="3"/>
          <circle cx="50" cy="50" r="16" fill="#eab308" />
          <circle cx="50" cy="50" r="3" fill="#13131F" />
        </svg>
      </div>
    );
  }

  // LP / standard Vinyl
  if (name.includes("lp") || name.includes("vinyl") || name.includes("album")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
          <circle cx="50" cy="50" r="48" fill="#111" stroke="#333" strokeWidth="1"/>
          <circle cx="50" cy="50" r="42" fill="transparent" stroke="#222" strokeWidth="0.5"/>
          <circle cx="50" cy="50" r="36" fill="transparent" stroke="#222" strokeWidth="0.5"/>
          <circle cx="50" cy="50" r="30" fill="transparent" stroke="#222" strokeWidth="0.5"/>
          <circle cx="50" cy="50" r="24" fill="transparent" stroke="#222" strokeWidth="0.5"/>
          <circle cx="50" cy="50" r="16" fill="#10b981" />
          <circle cx="50" cy="50" r="3" fill="#13131F" />
        </svg>
      </div>
    );
  }

  // Fallback icon for other formats
  return (
    <div className={wrapperStyle} title={formatName}>
      <svg viewBox="0 0 100 100" className="w-full h-full text-transparent">
        <circle cx="50" cy="50" r="48" fill="#374151" stroke="#9ca3af" strokeWidth="1"/>
        <text x="50" y="55" fontSize="24" textAnchor="middle" fill="#fff" fontWeight="bold">?</text>
      </svg>
    </div>
  );
}
