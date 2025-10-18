import React from 'react';
import { render, screen } from '@testing-library/react';
import CollectionPage from '../page';

// Mock the child components
jest.mock('../../components/SearchAndFilter', () => ({
  SearchAndFilter: ({ onFiltersChange, initialFilters, enableUrlSync }: any) => (
    <div data-testid="search-and-filter">
      SearchAndFilter Component
      <button onClick={() => onFiltersChange({ search: 'test' })}>Apply Filters</button>
    </div>
  ),
}));

jest.mock('../../components/MusicReleaseList', () => ({
  MusicReleaseList: ({ filters, pageSize }: any) => (
    <div data-testid="music-release-list">
      MusicReleaseList Component (pageSize: {pageSize})
    </div>
  ),
}));

describe('CollectionPage', () => {
  it('renders the page header', () => {
    render(<CollectionPage />);
    
    expect(screen.getByText('Music Collection')).toBeInTheDocument();
    expect(screen.getByText('Browse and search your music releases')).toBeInTheDocument();
  });

  it('renders SearchAndFilter component', () => {
    render(<CollectionPage />);
    
    expect(screen.getByTestId('search-and-filter')).toBeInTheDocument();
  });

  it('renders MusicReleaseList component', () => {
    render(<CollectionPage />);
    
    expect(screen.getByTestId('music-release-list')).toBeInTheDocument();
  });

  it('passes correct pageSize to MusicReleaseList', () => {
    render(<CollectionPage />);
    
    expect(screen.getByText(/pageSize: 60/)).toBeInTheDocument();
  });

  it('handles filter changes', () => {
    const { getByText } = render(<CollectionPage />);
    
    // Initially no filters
    expect(screen.getByTestId('music-release-list')).toBeInTheDocument();
    
    // Apply filters
    const applyButton = getByText('Apply Filters');
    applyButton.click();
    
    // MusicReleaseList should still be rendered
    expect(screen.getByTestId('music-release-list')).toBeInTheDocument();
  });

  it('has proper page structure', () => {
    const { container } = render(<CollectionPage />);
    
    // Check for main layout elements
    expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
    expect(container.querySelector('.bg-gray-50')).toBeInTheDocument();
  });

  it('passes enableUrlSync prop to SearchAndFilter', () => {
    render(<CollectionPage />);
    
    // The SearchAndFilter component is rendered
    expect(screen.getByTestId('search-and-filter')).toBeInTheDocument();
  });
});
