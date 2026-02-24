"use client";

import React from "react";
import { useTheme, AVAILABLE_THEMES, ThemeName } from "../contexts/ThemeContext";
import { updateUserProfile } from "../lib/auth";

interface ThemeSwitcherProps {
  /** Called after the theme has been saved successfully. */
  onSaveSuccess?: (theme: ThemeName) => void;
  /** Called when saving fails. */
  onSaveError?: (error: string) => void;
}

/**
 * ThemeSwitcher lets the user pick from the available pre-defined UI themes.
 * The selection is applied immediately via CSS variables and persisted to the
 * backend profile when the user clicks "Save Theme".
 */
export default function ThemeSwitcher({ onSaveSuccess, onSaveError }: ThemeSwitcherProps) {
  const { theme, setTheme } = useTheme();
  const [saving, setSaving] = React.useState(false);
  const [pendingTheme, setPendingTheme] = React.useState<ThemeName>(theme);
  // Track the last successfully saved theme so we can revert on API error
  const savedThemeRef = React.useRef<ThemeName>(theme);

  // Keep pendingTheme in sync if theme changes externally
  React.useEffect(() => {
    setPendingTheme(theme);
    savedThemeRef.current = theme;
  }, [theme]);

  const handleSelect = (name: ThemeName) => {
    setPendingTheme(name);
    // Apply preview immediately so the user can see the effect before saving
    setTheme(name);
  };

  const handleSave = async () => {
    setSaving(true);
    const previousTheme = savedThemeRef.current;
    try {
      await updateUserProfile(null, pendingTheme);
      savedThemeRef.current = pendingTheme;
      onSaveSuccess?.(pendingTheme);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to save theme";
      onSaveError?.(message);
      // Revert to the last successfully saved theme on error
      setTheme(previousTheme);
      setPendingTheme(previousTheme);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-4" data-testid="theme-switcher">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {AVAILABLE_THEMES.map((option) => (
          <button
            key={option.name}
            type="button"
            onClick={() => handleSelect(option.name)}
            aria-pressed={pendingTheme === option.name}
            className={`text-left p-4 rounded-lg border-2 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-blue-500 ${
              pendingTheme === option.name
                ? "border-blue-600 bg-blue-50"
                : "border-gray-200 bg-white hover:border-gray-400"
            }`}
          >
            <span className="block font-semibold text-gray-900 mb-1">
              {option.label}
              {pendingTheme === option.name && (
                <span className="ml-2 text-xs font-medium text-blue-600 uppercase tracking-wide">
                  Active
                </span>
              )}
            </span>
            <span className="text-sm text-gray-600">{option.description}</span>
          </button>
        ))}
      </div>

      <button
        type="button"
        onClick={handleSave}
        disabled={saving}
        className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-blue-500"
      >
        {saving ? (
          <>
            <svg
              className="animate-spin h-4 w-4"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8v8H4z"
              />
            </svg>
            Saving…
          </>
        ) : (
          "Save Theme"
        )}
      </button>
    </div>
  );
}
