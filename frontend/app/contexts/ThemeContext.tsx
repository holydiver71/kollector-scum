"use client";

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

/** All supported theme identifiers. */
export type ThemeName = "metal-default" | "clean-light";

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
    name: "metal-default",
    label: "Metal Default",
    description: "The classic dark metal look with deep reds and dark greys.",
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
  theme: "metal-default",
  setTheme: () => {},
});

const THEME_STORAGE_KEY = "selectedTheme";

/**
 * Provider that manages the active UI theme.
 * It applies a `data-theme` attribute to `<html>` and persists the selection
 * in `localStorage` so the preference survives page reloads.
 */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setThemeState] = useState<ThemeName>("metal-default");

  // Initialise from localStorage on mount (client-only)
  useEffect(() => {
    if (typeof window === "undefined") return;
    const stored = localStorage.getItem(THEME_STORAGE_KEY) as ThemeName | null;
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
