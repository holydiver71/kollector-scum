# Midnight Theme Implementation Summary

## Overview

Applied a consistent "midnight" dark theme across all major routes of the Kollector Sküm application, replacing the previous light/white card aesthetic with a deep dark style inspired by modern streaming platforms.

## Colour Palette

| Token | Hex | Usage |
|-------|-----|-------|
| Body background | `#0A0A10` | Page background |
| Card background | `#13131F` | All card and panel surfaces |
| Card border | `#1C1C28` | Card borders, dividers |
| Deep border | `#2E2E3E` | Input borders |
| Primary accent | `#8B5CF6` | Buttons, active states, badges |
| Accent hover | `#7C3AED` | Hover state for primary accent |
| Accent light | `#A78BFA` | Links, chip text, secondary accents |

## Files Changed

### CSS & Theming
- **`frontend/app/globals.css`** – Added `midnight` theme as the new `:root` default. Retained `metal-default` and `clean-light` under their own `[data-theme]` selectors. Added `--theme-accent`, `--theme-accent-hover`, `--theme-accent-light` variables to all themes.
- **`frontend/app/contexts/ThemeContext.tsx`** – Added `midnight` to `ThemeName` union and `AVAILABLE_THEMES` array. Changed default from `metal-default` to `midnight`.

### Backend
- **`backend/KollectorScum.Api/Controllers/ProfileController.cs`** – Added `midnight` to the server-side allowed themes validation set.

### Pages
- **`frontend/app/page.tsx`** (Dashboard) – Midnight stat cards with coloured accents, quick-action tiles with border hover, status indicator, system info footer.
- **`frontend/app/collection/page.tsx`** – Active filter chips updated from orange (`#D9601A`) to purple midnight style (`bg-[#8B5CF6]/15 border-[#8B5CF6]/20 text-[#A78BFA]`).
- **`frontend/app/releases/[id]/page.tsx`** – Redesigned as a 3-column layout: left sidebar with cover art, action buttons, and metadata cards; right area with title, tracklist and links. All white cards replaced with midnight dark panels.
- **`frontend/app/add/page.tsx`** – Pill-style tab switcher replacing the underline nav. Success/error states updated to midnight colours.

### Components
- **`components/RecentlyPlayed.tsx`** – Midnight bordered thumbnail grid with purple date headings and hover overlay.
- **`components/MusicReleaseList.tsx`** (MusicReleaseCard) – Dark cards with purple format badge and accent hover. Play/list action buttons updated. Error/empty states and pagination updated.
- **`components/SearchAndFilter.tsx`** – Plain dark panel replacing the background-image overlay. Purple accents on suggestion highlights and active filter state.
- **`components/Sidebar.tsx`** – Active nav item background changed from `#D93611` (red) to `#8B5CF6` (purple). Hover states darkened to `#1C1C28`.

### Tests Updated
- `app/add/__tests__/page.test.tsx` – Updated class selectors (`max-w-3xl`, `space-y-6`) and tab detection to match new markup.
- `app/components/__tests__/ThemeSwitcher.test.tsx` – Default active theme updated to `midnight`.
- `app/contexts/__tests__/ThemeContext.test.tsx` – Default theme assertions updated to `midnight`. Added `midnight` to AVAILABLE_THEMES tests.
- `app/components/__tests__/__snapshots__/SearchAndFilter.responsive.test.tsx.snap` – Snapshots updated to reflect new dark panel markup.

## Test Results

All **414 tests** pass after the changes.

## Notes

- The existing `metal-default` and `clean-light` themes remain fully functional and selectable via the theme switcher.
- Users who previously saved `metal-default` in localStorage will continue to see that theme (localStorage preference persists).
- New users and those without a saved theme preference will default to `midnight`.
