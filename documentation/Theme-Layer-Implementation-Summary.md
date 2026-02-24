# Theme Layer Implementation Summary

## Overview

This document summarises the changes made to implement a theme layer that allows users to switch between pre-defined UI styles from their profile settings page.

The feature introduces a `metal-default` theme (the current styling) as the default, and a `clean-light` theme as a second option. New themes can be added by extending a small set of touch points.

---

## Problem Statement

Users needed the ability to select a pre-defined UI style (theme) through their profile settings page. The selected theme should hot-swap immediately and be persisted across sessions.

---

## Architecture

### Theme Identification

Each theme is identified by a short string name (e.g. `"metal-default"`, `"clean-light"`). The name is stored in the database (`UserProfiles.SelectedTheme`) and in `localStorage` on the client.

### CSS Variable Strategy

Themes are defined as CSS custom property (`--theme-*`) blocks scoped to `[data-theme="<name>"]` selectors on the `<html>` element. The `body` and other global rules reference these variables. This means swapping a theme requires only changing the `data-theme` attribute on `<html>` – no class changes or re-renders.

---

## Backend Changes

| File | Change |
|---|---|
| `Models/UserProfile.cs` | Added `SelectedTheme` property (`string`, max 100 chars, default `"metal-default"`) |
| `DTOs/ProfileDtos.cs` | Added `SelectedTheme` to `UserProfileDto` and `UpdateProfileRequest` |
| `Controllers/ProfileController.cs` | Reads and writes `SelectedTheme`; validates against allow-list (`metal-default`, `clean-light`) |
| `Migrations/20260224231744_AddThemeToUserProfile.cs` | Adds `SelectedTheme` column to `UserProfiles` table with default `"metal-default"` |

### Allow-List Validation

The `UpdateProfile` endpoint rejects unknown theme names with a `400 Bad Request` response, preventing arbitrary strings from being stored.

---

## Frontend Changes

| File | Change |
|---|---|
| `app/contexts/ThemeContext.tsx` | New – `ThemeProvider`, `useTheme` hook, `AVAILABLE_THEMES` constant, `ThemeName` type |
| `app/globals.css` | Added `--theme-*` CSS variable blocks for each theme; `body` and `.header-with-bg` updated to use variables |
| `app/components/ThemeSwitcher.tsx` | New – picker UI with immediate preview and "Save Theme" button |
| `app/lib/auth.ts` | Added `selectedTheme` to `UserProfile` interface; updated `updateUserProfile` signature |
| `app/layout.tsx` | Wrapped app with `<ThemeProvider>` |
| `app/profile/page.tsx` | Added **Appearance** section containing `<ThemeSwitcher>` |

### ThemeContext

- Reads the saved theme from `localStorage` on mount.
- Writes the chosen theme to `localStorage` and sets `data-theme` on `<html>` whenever the theme changes.
- Provides `theme` (current value) and `setTheme` (updater) via React context.

### ThemeSwitcher Component

- Renders a card grid of available theme options.
- Applies the selected theme as a preview immediately (before saving).
- On "Save Theme", calls the backend `PUT /api/profile` with the chosen theme name.
- Reverts to the previously saved theme on API failure.

---

## Available Themes

| Name | Label | Description |
|---|---|---|
| `metal-default` | Metal Default | Classic dark metal look with deep reds and dark greys (original app style). |
| `clean-light` | Clean Light | Minimal light theme with neutral tones. |

### Adding a New Theme

1. Add CSS variable block `[data-theme="<name>"] { ... }` in `globals.css`.
2. Add the theme to `AVAILABLE_THEMES` in `ThemeContext.tsx`.
3. Add the name to the allow-list `HashSet` in `ProfileController.cs`.

---

## Tests

| Test File | Coverage |
|---|---|
| `app/contexts/__tests__/ThemeContext.test.tsx` | Default theme, `data-theme` attribute, theme switching, localStorage persistence, localStorage restore, invalid localStorage value |
| `app/components/__tests__/ThemeSwitcher.test.tsx` | Renders options, Save button, active state, click selection, success callback, error callback, `data-testid` |

All 413 frontend tests pass. All 732 backend tests pass.

---

## Security Considerations

- The `SelectedTheme` value is validated server-side against an explicit allow-list before being stored, preventing stored XSS or injection via theme names.
- Theme names are purely presentational strings applied as a `data-*` HTML attribute; they cannot execute code.
