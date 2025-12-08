import React from 'react';
import { render, screen } from '@testing-library/react';
import { SearchAndFilter } from '../SearchAndFilter';

jest.mock('../../lib/api');
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
}));

// Mock lookup components the same way other tests do
jest.mock('../LookupComponents', () => ({
  ArtistDropdown: ({ onSelect, value }: any) => (
    <select data-testid="artist-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Artist</option>
      <option value="1">Test Artist</option>
    </select>
  ),
  GenreDropdown: ({ onSelect, value }: any) => (
    <select data-testid="genre-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Genre</option>
      <option value="1">Metal</option>
    </select>
  ),
  LabelDropdown: ({ onSelect, value }: any) => (
    <select data-testid="label-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Label</option>
      <option value="1">Test Label</option>
    </select>
  ),
  CountryDropdown: ({ onSelect, value }: any) => (
    <select data-testid="country-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Country</option>
      <option value="1">UK</option>
    </select>
  ),
  FormatDropdown: ({ onSelect, value }: any) => (
    <select data-testid="format-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Format</option>
      <option value="1">Vinyl</option>
    </select>
  ),
  LookupDropdown: ({ items, value, placeholder, onSelect }: any) => (
    <div>
      <button type="button">{items?.find((i: any) => i.id === value)?.name || placeholder}</button>
    </div>
  ),
}));

describe('SearchAndFilter — responsive checks', () => {
  const mockChange = jest.fn();

  beforeEach(() => jest.clearAllMocks());

  it('exposes the responsive grid classes for advanced filters', () => {
    const { container } = render(<SearchAndFilter onFiltersChange={mockChange} openAdvanced={true} />);

    // Find the advanced filters grid container and assert it includes the responsive classes
    const grid = container.querySelector('div.grid');
    expect(grid).toBeTruthy();
    expect(grid?.className).toContain('grid-cols-1');
    expect(grid?.className).toContain('md:grid-cols-2');
    expect(grid?.className).toContain('lg:grid-cols-3');
  });

  it('does not render the search input when showSearchInput is false (search moved to header)', () => {
    render(<SearchAndFilter onFiltersChange={mockChange} showSearchInput={false} />);

    expect(screen.queryByPlaceholderText(/search by title, artist, or label/i)).not.toBeInTheDocument();
  });

  it('renders a compact snapshot (mobile, tablet, desktop) for visual regression', () => {
    const { asFragment, container } = render(<SearchAndFilter onFiltersChange={mockChange} openAdvanced={true} />);

    // mobile width
    if (container.firstChild) {
      (container.firstChild as HTMLElement).style.width = '375px';
    }
    expect(asFragment()).toMatchSnapshot('searchandfilter-mobile');

    // tablet width
    if (container.firstChild) {
      (container.firstChild as HTMLElement).style.width = '768px';
    }
    expect(asFragment()).toMatchSnapshot('searchandfilter-tablet');

    // desktop width
    if (container.firstChild) {
      (container.firstChild as HTMLElement).style.width = '1280px';
    }
    expect(asFragment()).toMatchSnapshot('searchandfilter-desktop');
  });
});
