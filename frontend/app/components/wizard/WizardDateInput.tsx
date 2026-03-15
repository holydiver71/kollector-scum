"use client";

import { useRef, useState } from "react";

// ─── Calendar icon ────────────────────────────────────────────────────────────

function CalendarIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeWidth={1.75}
      aria-hidden="true"
    >
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  );
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Parse e.g. "15/03/1982" or "1982-03-15" or "1982" → "1982-03-15" (YYYY-MM-DD).
 *  Returns `null` when the string cannot be parsed to a valid date. */
function parseDisplayToIso(raw: string): string | null {
  const s = raw.trim();
  if (!s) return null;

  // DD/MM/YYYY
  const dmyMatch = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (dmyMatch) {
    const [, d, m, y] = dmyMatch;
    const dt = new Date(`${y}-${m.padStart(2, "0")}-${d.padStart(2, "0")}`);
    if (!isNaN(dt.getTime())) return `${y}-${m.padStart(2, "0")}-${d.padStart(2, "0")}`;
  }

  // YYYY-MM-DD (already ISO)
  const isoMatch = s.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (isoMatch) {
    const dt = new Date(s);
    if (!isNaN(dt.getTime())) return s;
  }

  // MM/DD/YYYY (American) — only as fallback when day > 12 disambiguates
  const mdyMatch = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (mdyMatch) {
    const [, m, d, y] = mdyMatch;
    const dt = new Date(`${y}-${m.padStart(2, "0")}-${d.padStart(2, "0")}`);
    if (!isNaN(dt.getTime())) return `${y}-${m.padStart(2, "0")}-${d.padStart(2, "0")}`;
  }

  return null;
}

/** Format an ISO `YYYY-MM-DD` string to `DD/MM/YYYY` for display. */
function isoToDisplay(iso: string): string {
  if (!iso) return "";
  const [y, m, d] = iso.split("-");
  if (!y || !m || !d) return iso;
  return `${d}/${m}/${y}`;
}

// ─── WizardDateInput ──────────────────────────────────────────────────────────

interface DateInputProps {
  /** Field id (for label association) */
  id: string;
  /** Current value in `YYYY-MM-DD` format (stored format). Empty string when unset. */
  value: string;
  /** Called with the new value in `YYYY-MM-DD` format, or `""` when cleared. */
  onChange: (iso: string) => void;
  /** Optional error message – renders in red when provided */
  error?: string;
  /** aria-label for the calendar button */
  label?: string;
  className?: string;
}

/**
 * A styled date input that lets the user:
 * 1. Type a date by hand in `DD/MM/YYYY` format (e.g. `14/03/1982`).
 *    `YYYY-MM-DD` is also accepted for pasted / programmatic values.
 * 2. Click the calendar icon to open the native browser date picker.
 *
 * The stored value (passed via `value` / `onChange`) is always `YYYY-MM-DD`.
 */
export function WizardDateInput({
  id,
  value,
  onChange,
  error,
  label = "Pick a date",
  className = "",
}: DateInputProps) {
  const hiddenRef = useRef<HTMLInputElement>(null);
  // The text the user sees – formatted as DD/MM/YYYY
  const [displayValue, setDisplayValue] = useState(() => isoToDisplay(value));
  const [touched, setTouched] = useState(false);

  const showError = touched && !!error;

  const handleTextChange = (raw: string) => {
    setDisplayValue(raw);
    const iso = parseDisplayToIso(raw);
    if (iso) {
      onChange(iso);
    } else if (!raw.trim()) {
      onChange("");
    }
    // While typing a partial value, don't propagate an invalid empty string
  };

  const handleBlur = () => {
    setTouched(true);
    // Re-format to canonical DD/MM/YYYY if we can parse what the user typed
    const iso = parseDisplayToIso(displayValue);
    if (iso) {
      setDisplayValue(isoToDisplay(iso));
    }
  };

  const handleNativePick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const iso = e.target.value; // YYYY-MM-DD from native picker
    if (iso) {
      setDisplayValue(isoToDisplay(iso));
      onChange(iso);
    }
  };

  const openPicker = () => {
    // Sync the hidden input value so the picker opens on the current date
    if (hiddenRef.current) {
      if (value) hiddenRef.current.value = value;
      hiddenRef.current.showPicker?.();
      hiddenRef.current.click();
    }
  };

  const borderClass = showError
    ? "border-red-500 focus:ring-red-500"
    : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]";

  return (
    <div className={`relative ${className}`}>
      <div className="relative flex items-center">
        <input
          id={id}
          type="text"
          value={displayValue}
          onChange={(e) => handleTextChange(e.target.value)}
          onBlur={handleBlur}
          placeholder="DD/MM/YYYY"
          autoComplete="off"
          className={`w-full bg-[#0F0F1A] border rounded-lg pl-4 pr-10 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${borderClass}`}
        />
        <button
          type="button"
          onClick={openPicker}
          aria-label={label}
          className="absolute right-3 text-gray-500 hover:text-[#A78BFA] transition-colors focus:outline-none"
          tabIndex={-1}
        >
          <CalendarIcon className="w-4 h-4" />
        </button>
      </div>

      {/* Hidden native date picker – invisible but functional */}
      <input
        ref={hiddenRef}
        type="date"
        defaultValue={value}
        onChange={handleNativePick}
        className="absolute inset-0 w-full h-full opacity-0 pointer-events-none"
        tabIndex={-1}
        aria-hidden="true"
      />

      {showError && (
        <p className="mt-1.5 text-sm text-red-400" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}


