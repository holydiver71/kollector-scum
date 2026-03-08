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

    // EP (Extended Play): green label, 4 grooves
    if (name === "ep" || name.includes("ep") || name.includes("extended play") || name.includes("mini-album")) {
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

    // Boxset / Box set: isometric 3D box icon
    if (name.includes("boxset") || name.includes("box set") || name.includes("box-set") || name.includes("box")) {
      return (
        <div className={wrapperStyle} title={formatName}>
          <svg viewBox="0 0 100 100" className="w-full h-full">
            {/* Drop shadow */}
            <ellipse cx="50" cy="78" rx="26" ry="5" fill="#000" opacity="0.35"/>
            {/* Right face (darkest) */}
            <polygon points="50,44 82,30 82,56 50,70" fill="#3730a3"/>
            {/* Left face (mid) */}
            <polygon points="18,30 50,44 50,70 18,56" fill="#4f46e5"/>
            {/* Top face (lightest) */}
            <polygon points="50,16 82,30 50,44 18,30" fill="#818cf8"/>
            {/* Lid crease line */}
            <polygon points="50,16 82,30 50,44 18,30" fill="none" stroke="#a5b4fc" strokeWidth="1"/>
            {/* Label band on left face */}
            <polygon points="18,49 50,63 50,68 18,54" fill="#fbbf24" opacity="0.9"/>
            {/* Highlight on top-left edge */}
            <line x1="18" y1="30" x2="50" y2="16" stroke="#c7d2fe" strokeWidth="1.5"/>
          </svg>
        </div>
      );
    }

  // LP / standard Vinyl: purple label, iridescent shimmer on disc, 3 grooves
  if (name.includes("lp") || name.includes("vinyl") || name.includes("album")) {
    return (
      <div className={wrapperStyle} title={formatName}>
        <svg viewBox="0 0 100 100" className="w-full h-full">
          <defs>
            <radialGradient id="lp-disc-shine" cx="35%" cy="35%" r="65%">
              <stop offset="0%" stopColor="#1e1a2e" />
              <stop offset="50%" stopColor="#111118" />
              <stop offset="100%" stopColor="#0d0d14" />
            </radialGradient>
            <radialGradient id="lp-label" cx="40%" cy="35%" r="70%">
              <stop offset="0%" stopColor="#c084fc" />
              <stop offset="100%" stopColor="#7e22ce" />
            </radialGradient>
          </defs>
          <circle cx="50" cy="50" r="48" fill="url(#lp-disc-shine)" stroke="#9333ea" strokeWidth="1"/>
          <circle cx="50" cy="50" r="42" fill="transparent" stroke="#9333ea" strokeWidth="0.5" strokeOpacity="0.25"/>
          <circle cx="50" cy="50" r="34" fill="transparent" stroke="#9333ea" strokeWidth="0.5" strokeOpacity="0.2"/>
          <circle cx="50" cy="50" r="26" fill="transparent" stroke="#9333ea" strokeWidth="0.5" strokeOpacity="0.15"/>
          <circle cx="50" cy="50" r="18" fill="url(#lp-label)" />
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
