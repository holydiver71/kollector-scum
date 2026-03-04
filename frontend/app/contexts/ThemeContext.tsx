"use client";

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

/** All supported theme identifiers. */
export type ThemeName = "midnight" | "metal-default" | "metal-1" | "clean-light" | "dark";

/** Metadata about a selectable theme. */
export interface ThemeOption {
  /** The unique theme identifier stored in the user profile. */
  name: ThemeName;
  /** Human-readable label displayed in the UI. */
  label: string;
  /** Short description shown next to the picker option. */
  description: string;
}

/** Themes available to users. Add new entries here to expose additional themes. */
export const AVAILABLE_THEMES: ThemeOption[] = [
  {
    name: "midnight",
    label: "Midnight",
    description: "Dark streaming-platform aesthetic with purple accents.",
  },
  {
    name: "metal-default",
    label: "Metal Default",
    description: "The classic dark metal look with deep reds and dark greys.",
  },
  {
    name: "metal-1",
    label: "Metal Theme 1",
    description: "Dark teal & cream palette inspired by the heavy metal stage.",
  },
  {
    name: "dark",
    label: "Dark",
    description: "A deep, high-contrast dark theme for low-light environments.",
  },
  {
    name: "clean-light",
    label: "Clean Light",
    description: "A clean, minimal light theme with neutral tones.",
  },
];

interface ThemeContextType {
  /** The currently active theme name. */
  theme: ThemeName;
  /** Update the active theme, persisting it to localStorage. */
  setTheme: (theme: ThemeName) => void;
}

const ThemeContext = createContext<ThemeContextType>({
  theme: "midnight",
  setTheme: () => {},
});

const THEME_STORAGE_KEY = "selectedTheme";
const THEME_MIGRATION_V1_KEY = "selectedTheme_migrated_v1";

/**
 * Provider that manages the active UI theme.
 * It applies a `data-theme` attribute to `<html>` and persists the selection
 * in `localStorage` so the preference survives page reloads.
 */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setThemeState] = useState<ThemeName>("midnight");

  // Initialise from localStorage on mount (client-only)
  useEffect(() => {
    if (typeof window === "undefined") return;
    const stored = localStorage.getItem(THEME_STORAGE_KEY) as ThemeName | null;
    const hasMigrated = localStorage.getItem(THEME_MIGRATION_V1_KEY) === "1";

    if (!hasMigrated && stored === "metal-default") {
      localStorage.setItem(THEME_STORAGE_KEY, "midnight");
      localStorage.setItem(THEME_MIGRATION_V1_KEY, "1");
      setThemeState("midnight");
      return;
    }

    if (!hasMigrated) {
      localStorage.setItem(THEME_MIGRATION_V1_KEY, "1");
    }

    if (stored && AVAILABLE_THEMES.some((t) => t.name === stored)) {
      setThemeState(stored);
    }
  }, []);

  // Apply theme to <html> element whenever it changes
  useEffect(() => {
    if (typeof document === "undefined") return;
    document.documentElement.setAttribute("data-theme", theme);
  }, [theme]);

  const setTheme = useCallback((newTheme: ThemeName) => {
    setThemeState(newTheme);
    if (typeof window !== "undefined") {
      localStorage.setItem(THEME_STORAGE_KEY, newTheme);
    }
  }, []);

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

/**
 * Hook to access the current theme and a setter from any client component.
 * Must be used inside a `ThemeProvider`.
 */
export function useTheme(): ThemeContextType {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
}
