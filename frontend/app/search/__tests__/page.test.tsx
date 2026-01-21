import React from 'react';
import { render, screen } from '@testing-library/react';
import SearchPage from '../page';

// Mock the child components
jest.mock('../../components/SearchAndFilter', () => ({
  SearchAndFilter: ({ onFiltersChange }: { onFiltersChange: (filters: { search: string }) => void }) => (
    <div data-testid="search-and-filter">
      SearchAndFilter Component
      <button onClick={() => onFiltersChange({ search: 'test' })}>Apply Filters</button>
    </div>
  ),
  QuickSearch: ({ onSearch }: { onSearch?: (value: string) => void }) => (
    <div data-testid="quick-search">
      QuickSearch Component
      <input
        type="text"
        placeholder="Search..."
        onChange={(e) => onSearch && onSearch(e.target.value)}
      />
    </div>
  ),
}));

jest.mock('../../components/MusicReleaseList', () => ({
  MusicReleaseList: ({ filters }: { filters: { search?: string } }) => (
    <div data-testid="music-release-list">
      MusicReleaseList Component
      {filters.search && <span>Search: {filters.search}</span>}
    </div>
  ),
}));

describe('SearchPage', () => {
  it('renders the page header', () => {
    render(<SearchPage />);
    
    expect(screen.getByText('Search Music')).toBeInTheDocument();
    expect(screen.getByText('Find specific releases in your collection')).toBeInTheDocument();
  });

  it('initially shows search landing page', () => {
    render(<SearchPage />);
    
    expect(screen.getByText('Search Your Collection')).toBeInTheDocument();
    expect(screen.getByText(/Use the search below to quickly find releases/)).toBeInTheDocument();
  });

  it('renders QuickSearch component initially', () => {
    render(<SearchPage />);
    
    expect(screen.getByTestId('quick-search')).toBeInTheDocument();
  });

  it('does not show results initially', () => {
    render(<SearchPage />);
    
    expect(screen.queryByTestId('music-release-list')).not.toBeInTheDocument();
  });

  it('shows results after applying filters', () => {
    render(<SearchPage />);
    
    // Apply filters
    const applyButton = screen.getByText('Apply Filters');
    fireEvent.click(applyButton);
    
    // Should show results now
    expect(screen.getByTestId('music-release-list')).toBeInTheDocument();
  });

  it('hides search landing when results are shown', () => {
    render(<SearchPage />);
    
    // Initially shows landing
    expect(screen.getByText('Search Your Collection')).toBeInTheDocument();
    
    // Apply filters
    const applyButton = screen.getByText('Apply Filters');
    fireEvent.click(applyButton);
    
    // Landing should be hidden
    expect(screen.queryByText('Search Your Collection')).not.toBeInTheDocument();
  });

  it('shows SearchAndFilter after applying filters', () => {
    render(<SearchPage />);
    
    // Apply filters
    const applyButton = screen.getByText('Apply Filters');
    fireEvent.click(applyButton);
    
    // SearchAndFilter should be visible in results view
    expect(screen.getByTestId('search-and-filter')).toBeInTheDocument();
  });

  it('has proper page structure', () => {
    const { container } = render(<SearchPage />);
    
    // Check for main layout elements
    expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
    expect(container.querySelector('.bg-gray-50')).toBeInTheDocument();
  });

  it('shows search icon in landing page', () => {
    render(<SearchPage />);
    
    expect(screen.getByText('🔍')).toBeInTheDocument();
  });

  it('passes filters to MusicReleaseList', () => {
    render(<SearchPage />);
    
    // Apply filters
    const applyButton = screen.getByText('Apply Filters');
    fireEvent.click(applyButton);
    
    // Check that filters are passed
    expect(screen.getByText('Search: test')).toBeInTheDocument();
  });
});
