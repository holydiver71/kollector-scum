import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import Header from '../Header';

jest.mock('../../lib/api', () => ({ getPagedCount: jest.fn().mockResolvedValue(42) }));

// Provide mutable mock state for next/navigation so tests can change path/search across cases
const mockPush = jest.fn();
const mockReplace = jest.fn();
let mockPathname = '/collection';

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, replace: mockReplace }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => mockPathname,
}));

describe('Header — responsive behavior & filters navigation', () => {
  afterEach(() => {
    jest.clearAllMocks();
    mockPathname = '/collection';
  });

  it('renders QuickSearch container and wrapper classes', async () => {
    const HeaderComp = require('../Header').default;

    const { container, asFragment } = render(<HeaderComp />);

    // The header should contain the search-bar and a flex wrapper that can wrap on small screens
    const searchBar = container.querySelector('.search-bar');
    expect(searchBar).toBeTruthy();
    // the search-bar should be responsive: allow shrinking on very small viewports, and restore min width on sm+
    expect(searchBar?.className).toContain('min-w-0');
    expect(searchBar?.className).toContain('sm:min-w-[300px]');
    const wrapper = container.querySelector('div.flex.flex-wrap');
    expect(wrapper).toBeTruthy();

    // wait for async stat fetch to settle (avoid act warnings)
    await screen.findByText(/42 releases in your collection/i);

    // logo should be present
    await screen.findByAltText(/Kollector Sküm logo/i);

    // snapshots for different viewport widths
    if (container.firstChild) (container.firstChild as HTMLElement).style.width = '375px';
    expect(asFragment()).toMatchSnapshot('header-mobile');

    if (container.firstChild) (container.firstChild as HTMLElement).style.width = '1024px';
    expect(asFragment()).toMatchSnapshot('header-tablet');
  });

  it('toggles the showAdvanced param via router.replace when on /collection', async () => {
    // ensure we are on the collection page
    mockPathname = '/collection';

    const HeaderComp = require('../Header').default;
    render(<HeaderComp />);

    // wait for initial async fetch to finish before interacting
    await screen.findByText(/releases in your collection/i);

    const filtersButton = screen.getByText(/Filters/);
    fireEvent.click(filtersButton);

    // since we're on /collection with no existing params, replace should be called to set showAdvanced=true
    expect(mockReplace).toHaveBeenCalledWith('/collection?showAdvanced=true', { scroll: false });
    expect(mockPush).not.toHaveBeenCalled();
  });
});
