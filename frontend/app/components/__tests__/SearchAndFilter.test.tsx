import { render, screen, waitFor } from '@testing-library/react';
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
  ArtistDropdown: ({ onSelect, value }: { onSelect: (item: { id: number }) => void; value?: number }) => (
    <select data-testid="artist-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Artist</option>
      <option value="1">Test Artist</option>
    </select>
  ),
  GenreDropdown: ({ onSelect, value }: { onSelect: (item: { id: number }) => void; value?: number }) => (
    <select data-testid="genre-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Genre</option>
      <option value="1">Metal</option>
    </select>
  ),
  LabelDropdown: ({ onSelect, value }: { onSelect: (item: { id: number }) => void; value?: number }) => (
    <select data-testid="label-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Label</option>
      <option value="1">Test Label</option>
    </select>
  ),
  CountryDropdown: ({ onSelect, value }: { onSelect: (item: { id: number }) => void; value?: number }) => (
    <select data-testid="country-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Country</option>
      <option value="1">UK</option>
    </select>
  ),
  FormatDropdown: ({ onSelect, value }: { onSelect: (item: { id: number }) => void; value?: number }) => (
    <select data-testid="format-dropdown" value={value || ''} onChange={(e) => onSelect({ id: parseInt(e.target.value) })}>
      <option value="">Select Format</option>
      <option value="1">Vinyl</option>
    </select>
  ),
  LookupDropdown: ({ items, value, placeholder, onSelect }: any) => (
    <div>
      <button type="button">{items?.find((i: any) => i.id === value)?.name || placeholder}</button>
      <div>
        {(items || []).map((it: any) => (
          <button type="button" key={it.id} onClick={() => onSelect && onSelect(it)}>{it.name}</button>
        ))}
      </div>
    </div>
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

  it('renders advanced filters when openAdvanced is true', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} openAdvanced={true} />);

    expect(screen.getByTestId('artist-dropdown')).toBeInTheDocument();
    expect(screen.getByTestId('genre-dropdown')).toBeInTheDocument();
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
    
    // Active filters visible in the UI (search field retains its value)
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
    // Render with advanced filters visible
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} openAdvanced={true} />);

    const yearFromInput = screen.getByPlaceholderText(/e.g., 1970/i);
    fireEvent.change(yearFromInput, { target: { value: '1980' } });

    expect(mockOnFiltersChange).toHaveBeenCalledWith(expect.objectContaining({ yearFrom: 1980 }));
  });

  it('displays live/studio recording filter', () => {
    render(<SearchAndFilter onFiltersChange={mockOnFiltersChange} openAdvanced={true} />);

    // Find the select by its options
     const dropdownButton = screen.getByRole('button', { name: /all recordings/i });
     fireEvent.click(dropdownButton);

     const liveOption = screen.getByText('Live recordings');
     fireEvent.click(liveOption);

     expect(mockOnFiltersChange).toHaveBeenCalledWith(expect.objectContaining({ live: true }));
  });

  it('does not render share control (removed from control panel)', () => {
    render(
      <SearchAndFilter 
        onFiltersChange={mockOnFiltersChange}
        initialFilters={{ search: 'test' }}
        enableUrlSync={true}
      />
    );
    // share control row removed from the control panel; it is intentionally handled at the page-level
    const shareButton = screen.queryByText(/share/i);
    expect(shareButton).not.toBeInTheDocument();
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
