import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { SearchAndFilter } from '../SearchAndFilter';
import * as api from '../../lib/api';

// Mock the API module
jest.mock('../../lib/api');
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  useSearchParams: () => new URLSearchParams(),
}));

// Mock the lookup components
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
}));

describe('SearchAndFilter Component', () => {
  const mockOnFiltersChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (api.getSearchSuggestions as jest.Mock).mockResolvedValue([]);
  });

  it('renders search input field', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const searchInput = screen.getByPlaceholderText(/search by title, artist, or label/i);
    expect(searchInput).toBeInTheDocument();
  });

  it('calls onFiltersChange when search text changes', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const searchInput = screen.getByPlaceholderText(/search by title, artist, or label/i);
    fireEvent.change(searchInput, { target: { value: 'test search' } });

    expect(mockOnFiltersChange).toHaveBeenCalledWith({ search: 'test search' });
  });

  it('shows advanced filters when toggle button clicked', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const toggleButton = screen.getByText(/show advanced filters/i);
    fireEvent.click(toggleButton);

    expect(screen.getByText(/hide advanced filters/i)).toBeInTheDocument();
    expect(screen.getByTestId('artist-dropdown')).toBeInTheDocument();
    expect(screen.getByTestId('genre-dropdown')).toBeInTheDocument();
  });

  it('clears all filters when clear button clicked', () => {
    render(
      <SearchAndFilter 
        onFiltersChange={mockOnFiltersChange}
        initialFilters={{ search: 'test', artistId: 1 }}
      />
    );

    const clearButton = screen.getByText(/clear all filters/i);
    fireEvent.click(clearButton);

    expect(mockOnFiltersChange).toHaveBeenCalledWith({});
  });

  it('displays active filter chips', () => {
    render(
      <SearchAndFilter
        initialFilters={{ search: 'metal' }}
        onFiltersChange={mockOnFiltersChange}
      />
    );

    // Check that the search value is in the input
    const searchInput = screen.getByPlaceholderText(/search by title/i);
    expect(searchInput).toHaveValue('metal');
    
    // Check for Clear All Filters button which appears when filters are active
    expect(screen.getByText(/clear all filters/i)).toBeInTheDocument();
  });  it('fetches suggestions when search text is entered', async () => {
    const mockSuggestions = [
      { type: 'release', id: 1, name: 'Test Album', subtitle: '1990' },
      { type: 'artist', id: 2, name: 'Test Artist' },
    ];
    (api.getSearchSuggestions as jest.Mock).mockResolvedValue(mockSuggestions);

    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const searchInput = screen.getByPlaceholderText(/search by title, artist, or label/i);
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(api.getSearchSuggestions).toHaveBeenCalledWith('test');
    });
  });

  it('updates year range filters', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    // Show advanced filters
    const toggleButton = screen.getByText(/show advanced filters/i);
    fireEvent.click(toggleButton);

    const yearFromInput = screen.getByPlaceholderText(/e.g., 1970/i);
    fireEvent.change(yearFromInput, { target: { value: '1980' } });

    expect(mockOnFiltersChange).toHaveBeenCalledWith(expect.objectContaining({ yearFrom: 1980 }));
  });

  it('displays live/studio recording filter', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const toggleButton = screen.getByText(/show advanced filters/i);
    fireEvent.click(toggleButton);

    // Find the select by its options
    const recordingSelect = screen.getByText('All recordings').closest('select');
    expect(recordingSelect).toBeInTheDocument();
    
    if (recordingSelect) {
      fireEvent.change(recordingSelect, { target: { value: 'true' } });
      expect(mockOnFiltersChange).toHaveBeenCalledWith(expect.objectContaining({ live: true }));
    }
  });

  it('shows share button when URL sync is enabled and filters are active', () => {
    render(
      <SearchAndFilter 
        onFiltersChange={mockOnFiltersChange}
        initialFilters={{ search: 'test' }}
        enableUrlSync={true}
      />
    );

    const shareButton = screen.getByText(/share/i);
    expect(shareButton).toBeInTheDocument();
  });

  it('does not show suggestions for short search queries', async () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} />);
    
    const searchInput = screen.getByPlaceholderText(/search by title, artist, or label/i);
    fireEvent.change(searchInput, { target: { value: 'a' } });

    await waitFor(() => {
      expect(api.getSearchSuggestions).not.toHaveBeenCalled();
    });
  });
});
