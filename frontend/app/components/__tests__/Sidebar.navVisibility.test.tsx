/**
 * Tests for conditional sidebar navigation visibility based on collection state.
 *
 * When a user's collection is empty (hasCollection === false | null) only the
 * "always visible" items (Home, Add Music, Settings, Profile) should appear.
 * When the collection has at least one release (hasCollection === true) all
 * navigation items including collection-dependent ones and Random Album become
 * visible.
 */

import React from 'react';
import { render } from '@testing-library/react';
import Sidebar from '../Sidebar';

// next/navigation is mocked globally in jest.setup.ts; re-declare here to keep
// tests self-documenting and to ensure the correct return values are set.
jest.mock('next/navigation', () => ({
  usePathname: () => '/',
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
}));

// Mock the api utilities so the Sidebar's useEffect check doesn't make real
// HTTP calls during these tests.
jest.mock('../../lib/api', () => ({
  fetchJson: jest.fn().mockResolvedValue({ totalCount: 0 }),
  getRandomReleaseId: jest.fn().mockResolvedValue(1),
}));

// ----------------------------------------------------------------------
// Helper – lets each test control the collection state independently.
// ----------------------------------------------------------------------

let mockHasCollection: boolean | null = null;
let mockIsReady = false;

jest.mock('../../contexts/CollectionContext', () => ({
  useCollection: () => ({
    hasCollection: mockHasCollection,
    setHasCollection: jest.fn(),
    isReady: mockIsReady,
  }),
}));

// Always-visible navigation items (href → label)
const ALWAYS_VISIBLE: Array<{ label: string; href?: string }> = [
  { label: 'Home', href: '/' },
  { label: 'Add Music', href: '/add' },
  { label: 'Settings', href: '/settings' },
  { label: 'Profile', href: '/profile' },
];

// Navigation items that require a non-empty collection
const COLLECTION_ONLY: Array<{ label: string; href?: string }> = [
  { label: 'Collection', href: '/collection' },
  { label: 'Kollections', href: '/kollections' },
  { label: 'Lists', href: '/lists' },
  { label: 'Artists', href: '/artists' },
  { label: 'Genres', href: '/genres' },
  { label: 'Statistics', href: '/statistics' },
  { label: 'Random Album' }, // button, no href
];

/** Returns the first nav link or button with this label (ignores tooltips). */
function queryNavItem(label: string, href?: string) {
  if (href) {
    return document.querySelector(`a[href="${href}"]`);
  }
  // For buttons, find a button whose text content includes the label
  const buttons = document.querySelectorAll('button');
  for (const btn of Array.from(buttons)) {
    if (btn.textContent?.includes(label)) return btn;
  }
  return null;
}

// ======================================================================
describe('Sidebar navigation visibility', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  // ------------------------------------------------------------------
  describe('when collection is empty (hasCollection === false)', () => {
    beforeEach(() => {
      mockHasCollection = false;
      mockIsReady = true;
    });

    it('shows always-visible items: Home, Add Music, Settings, Profile', () => {
      render(<Sidebar />);
      for (const item of ALWAYS_VISIBLE) {
        expect(queryNavItem(item.label, item.href)).toBeInTheDocument();
      }
    });

    it('hides collection-dependent navigation items', () => {
      render(<Sidebar />);
      for (const item of COLLECTION_ONLY) {
        expect(queryNavItem(item.label, item.href)).not.toBeInTheDocument();
      }
    });
  });

  // ------------------------------------------------------------------
  describe('when collection status is loading (hasCollection === null)', () => {
    beforeEach(() => {
      mockHasCollection = null;
      mockIsReady = false;
    });

    it('shows always-visible items while loading', () => {
      render(<Sidebar />);
      for (const item of ALWAYS_VISIBLE) {
        expect(queryNavItem(item.label, item.href)).toBeInTheDocument();
      }
    });

    it('hides collection-dependent items until status is confirmed', () => {
      render(<Sidebar />);
      for (const item of COLLECTION_ONLY) {
        expect(queryNavItem(item.label, item.href)).not.toBeInTheDocument();
      }
    });
  });

  // ------------------------------------------------------------------
  describe('when collection has at least one release (hasCollection === true)', () => {
    beforeEach(() => {
      mockHasCollection = true;
      mockIsReady = true;
    });

    it('shows always-visible items', () => {
      render(<Sidebar />);
      for (const item of ALWAYS_VISIBLE) {
        expect(queryNavItem(item.label, item.href)).toBeInTheDocument();
      }
    });

    it('shows all collection-dependent navigation items', () => {
      render(<Sidebar />);
      for (const item of COLLECTION_ONLY) {
        expect(queryNavItem(item.label, item.href)).toBeInTheDocument();
      }
    });

    it('shows the Random Album button', () => {
      render(<Sidebar />);
      expect(queryNavItem('Random Album')).toBeInTheDocument();
    });
  });
});
