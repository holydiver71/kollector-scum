# Phase 3: Frontend Manual Data Entry - Implementation Plan

## Overview
Enhance the existing AddReleaseForm with advanced features including:
- Combo-box inputs for all lookup fields (select existing OR type new)
- Track list editor with add/remove/reorder functionality
- Enhanced validation and error handling
- Support for auto-creation indicators
- Better UX with loading states and helpful messages

**Start Date**: October 20, 2025  
**Status**: 🚧 In Progress

---

## Current State Analysis

### Existing Implementation (`AddReleaseForm.tsx`)
✅ **Already Implemented**:
- Basic form layout with sections (Basic Info, Classification, etc.)
- Lookup data loading (artists, genres, labels, countries, formats, packagings, stores)
- Multi-select for artists and genres
- Single-select dropdowns for label, format, country, packaging
- Form validation (title, artists required)
- Error handling and loading states
- Submit functionality

❌ **Missing / Needs Enhancement**:
- Combo-box functionality (can't type new values)
- Track list editor
- Purchase info section
- Image upload/URL inputs
- External links section
- Visual indicators for "will create new entity"
- Better multi-select UX (current is native HTML)
- Auto-save/draft functionality
- Unsaved changes warning

---

## Implementation Strategy

### Approach: Incremental Enhancement
Rather than rewriting the entire form, we'll enhance it section by section:

1. **Phase 3.1**: Create reusable Combo-box component
2. **Phase 3.2**: Replace existing dropdowns with combo-boxes
3. **Phase 3.3**: Create Track List Editor component
4. **Phase 3.4**: Add Purchase Info section
5. **Phase 3.5**: Add Images and Links sections
6. **Phase 3.6**: Enhanced validation and UX polish

---

## Phase 3.1: Combo-box Component

### Component: `ComboBox.tsx`

**Features**:
- Search/filter existing items
- Select existing item from dropdown
- OR type new value (not in list)
- Visual indicator for "new" vs "existing"
- Support for single-select and multi-select modes
- Keyboard navigation (arrows, enter, escape)
- Accessible (ARIA labels)

**Props**:
```typescript
interface ComboBoxProps {
  label: string;
  required?: boolean;
  items: LookupItem[];
  value: number[] | number | string[] | string; // Supports IDs or names
  onChange: (value: number[] | number | string[] | string) => void;
  multiple?: boolean;
  placeholder?: string;
  helpText?: string;
  error?: string;
  disabled?: boolean;
  onCreateNew?: (name: string) => void; // Called when new value added
}
```

**UI States**:
- Dropdown closed: Shows selected items or placeholder
- Dropdown open: Shows filtered list with search
- New item: Badge showing "New: Artist Name"
- Selected existing: Standard item display

### Implementation Steps
1. Create `/frontend/app/components/ComboBox.tsx`
2. Add search/filter logic
3. Add keyboard navigation
4. Add "new item" detection and styling
5. Write unit tests
6. Create Storybook stories (optional)

---

## Phase 3.2: Replace Dropdowns with Combo-boxes

### Fields to Update in `AddReleaseForm.tsx`:

1. **Artists** (multi-select combo-box)
   - Allow typing new artist names
   - Show "New: Artist Name" badges
   - Store artistIds for existing, artistNames for new

2. **Genres** (multi-select combo-box)
   - Allow typing new genre names
   - Store genreIds for existing, genreNames for new

3. **Label** (single-select combo-box)
   - Allow typing new label name
   - Store labelId for existing, labelName for new

4. **Country** (single-select combo-box)
   - Allow typing new country name
   - Store countryId for existing, countryName for new

5. **Format** (single-select combo-box)
   - Allow typing new format name
   - Store formatId for existing, formatName for new

6. **Packaging** (single-select combo-box)
   - Allow typing new packaging name
   - Store packagingId for existing, packagingName for new

### Updated DTO Structure
```typescript
interface CreateMusicReleaseDto {
  title: string;
  
  // Artists: support both IDs and names
  artistIds?: number[];
  artistNames?: string[]; // NEW
  
  // Genres: support both IDs and names
  genreIds?: number[];
  genreNames?: string[]; // NEW
  
  // Label: support both ID and name
  labelId?: number;
  labelName?: string; // NEW
  
  // Country: support both ID and name
  countryId?: number;
  countryName?: string; // NEW
  
  // Format: support both ID and name
  formatId?: number;
  formatName?: string; // NEW
  
  // Packaging: support both ID and name
  packagingId?: number;
  packagingName?: string; // NEW
  
  // ... rest of fields
}
```

---

## Phase 3.3: Track List Editor Component

### Component: `TrackListEditor.tsx`

**Features**:
- Add new track
- Remove track
- Reorder tracks (drag & drop with react-beautiful-dnd or manual up/down buttons)
- Track fields:
  - Position/index (auto-assigned)
  - Title (required)
  - Duration (MM:SS or seconds)
  - Artists (optional, defaults to album artists)
  - Live checkbox
- Validation per track
- Disc/media organization (multiple discs support)

**Props**:
```typescript
interface Track {
  index: number;
  title: string;
  lengthSecs?: number;
  artists?: string[];
  genres?: string[];
  live?: boolean;
}

interface Media {
  name?: string; // e.g., "CD 1", "LP Side A"
  tracks: Track[];
}

interface TrackListEditorProps {
  value: Media[];
  onChange: (media: Media[]) => void;
  albumArtists: string[]; // For default track artists
  error?: string;
}
```

**UI Layout**:
```
[+ Add Disc/Media]

Disc 1 [Rename] [Remove]
├─ Track 1: Title [Edit] [Remove] [↑] [↓]
├─ Track 2: Title [Edit] [Remove] [↑] [↓]
└─ [+ Add Track]
```

### Implementation Steps
1. Create `/frontend/app/components/TrackListEditor.tsx`
2. Create `/frontend/app/components/TrackRow.tsx`
3. Add track validation
4. Add duration formatter (convert MM:SS ↔ seconds)
5. Add drag & drop (optional, phase 2)
6. Write unit tests

---

## Phase 3.4: Purchase Info Section

### UI Addition to `AddReleaseForm.tsx`

**Section**: "Purchase Information (Optional)"

**Fields**:
- Store (combo-box, supports new store creation)
- Price (number input)
- Currency (dropdown: USD, EUR, GBP, CAD, etc.)
- Purchase Date (date picker)
- Notes (textarea)

**Note**: Per the two-step workflow, purchase info is **optional** during initial release creation. It can be added later via a modal after release creation.

### Decision: Include in Form or Separate Modal?
**Recommended**: Add to form as optional collapsible section
- **Pros**: Single-step workflow option for users who have all info
- **Cons**: Makes form longer
- **Solution**: Make it collapsible/expandable, default collapsed

---

## Phase 3.5: Images and Links Sections

### Images Section
**Fields**:
- Cover Front (URL input or file upload)
- Cover Back (URL input)
- Thumbnail (auto-generated or URL)

**UX**: Show image preview when URL entered

### Links Section
**Dynamic list** of external links:
- URL (input)
- Type (dropdown: Discogs, Spotify, MusicBrainz, Bandcamp, etc.)
- Description (optional)
- [+ Add Link] button
- [Remove] button per link

---

## Phase 3.6: Validation & UX Enhancements

### Enhanced Validation

**Field-level validation**:
- Title: Required, max 300 chars
- Artists: At least 1 required
- Release Year: Optional, YYYY or YYYY-MM-DD format
- Catalog Number: Max 100 chars
- UPC/Barcode: Numeric, 12-13 digits
- Track Title: Required per track
- URLs: Valid HTTP/HTTPS format

**Form-level validation**:
- Check for duplicate catalog number (API call)
- Warn if title + artist combination exists
- Validate at least 1 track if media array provided

### UX Enhancements

1. **Loading States**
   - Skeleton loaders for lookup data
   - Disable form while submitting
   - Progress indicator for file uploads

2. **Error Messages**
   - Inline field errors (red border + message)
   - Summary error banner at top
   - API error messages (duplicate detection, etc.)

3. **Success Feedback**
   - Success message with created entity summary
   - "Created 2 new artists, 1 new label"
   - Redirect to release detail page

4. **Unsaved Changes Warning**
   - Detect form dirty state
   - Confirm before navigation
   - Optional: Auto-save to localStorage

5. **Help Text**
   - Tooltips for complex fields
   - Example formats (e.g., "YYYY-MM-DD")
   - Character counters

6. **Responsive Design**
   - Mobile-friendly layout
   - Stack fields on small screens
   - Touch-friendly buttons/inputs

---

## Technical Considerations

### State Management
**Current**: Local component state with `useState`  
**Recommendation**: Continue with local state, add form library if complexity grows

**Options**:
- Continue with manual state (works for current form)
- Add `react-hook-form` for better validation
- Add `formik` for complex forms

### Accessibility
- All inputs have labels
- ARIA attributes for custom components
- Keyboard navigation support
- Focus management
- Error announcement for screen readers

### Performance
- Debounce search/filter in combo-boxes
- Virtualize long lists (if >1000 items)
- Lazy load lookup data (fetch on demand vs all upfront)
- Memoize expensive computations

---

## Testing Strategy

### Unit Tests (Jest + React Testing Library)
- ComboBox component behavior
- Track list add/remove/reorder
- Form validation logic
- Error handling

### Integration Tests
- Form submission flow
- API integration (mocked)
- Combo-box with lookup data
- Track editor with form

### E2E Tests (Playwright)
- Complete form fill and submit
- Create release with new artists
- Add tracks to release
- Validation error handling
- Success flow end-to-end

---

## Timeline Estimate

| Phase | Task | Estimated Time |
|-------|------|----------------|
| 3.1 | ComboBox component | 4-6 hours |
| 3.2 | Replace dropdowns | 2-3 hours |
| 3.3 | Track List Editor | 4-5 hours |
| 3.4 | Purchase Info section | 2 hours |
| 3.5 | Images & Links sections | 2-3 hours |
| 3.6 | Validation & UX polish | 3-4 hours |
| Testing | Unit + Integration tests | 4-5 hours |
| **Total** | | **21-30 hours** (3-4 days) |

---

## Dependencies

### Existing
- React 18+
- Next.js 14+
- Tailwind CSS
- TypeScript

### Potential Additions
- `react-beautiful-dnd` - Drag & drop for tracks (optional)
- `react-hook-form` - Form validation (optional)
- `date-fns` - Date formatting
- `react-select` - Enhanced select component (alternative to custom combo-box)

---

## Next Steps

1. ✅ Create this plan document
2. Create ComboBox component
3. Update AddReleaseForm with combo-boxes
4. Create TrackListEditor component
5. Add remaining form sections
6. Write tests
7. Update documentation

---

**Last Updated**: October 20, 2025  
**Status**: 📋 Planning Complete, Ready for Implementation
