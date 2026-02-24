import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AddReleaseForm from '../AddReleaseForm';
import { fetchJson } from '../../lib/api';

// Mock the api module
jest.mock('../../lib/api', () => ({
  fetchJson: jest.fn(),
}));

// Mock ComboBox to simplify testing
jest.mock('../ComboBox', () => {
  return function MockComboBox({ 
    label, 
    items = [],
    value, 
    newValues = [],
    onChange, 
    multiple, 
    required,
    error, 
    placeholder,
  }: { 
    label: string;
    items?: Array<{ id: number; name: string }>;
    value?: number | number[] | null;
    newValues?: string[];
    onChange: (ids: number[], newValues: string[]) => void;
    multiple?: boolean;
    required?: boolean;
    error?: string;
    placeholder?: string;
  }) {
    const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
      if (multiple) {
        const selected = Array.from(e.target.selectedOptions).map((opt) => parseInt(opt.value));
        onChange(selected, []);
      } else {
        const val = e.target.value ? parseInt(e.target.value) : null;
        onChange(val ? [val] : [], []);
      }
    };

    const handleNewValue = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newVal = e.target.value;
      if (newVal) {
        onChange([], [newVal]);
      }
    };

    return (
      <div data-testid={`combobox-${label.toLowerCase()}`}>
        <label>{label} {required && <span>*</span>}</label>
        <select
          data-testid={`select-${label.toLowerCase()}`}
          multiple={multiple}
          value={multiple ? (Array.isArray(value) ? value.map(String) : []) : (value ? String(value) : '')}
          onChange={handleChange}
        >
          <option value="">Select...</option>
          {items.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
        <input
          data-testid={`input-new-${label.toLowerCase()}`}
          type="text"
          placeholder={placeholder}
          onChange={handleNewValue}
        />
        {error && <span data-testid={`error-${label.toLowerCase()}`}>{error}</span>}
        {newValues.length > 0 && (
          <div data-testid={`new-values-${label.toLowerCase()}`}>
            {newValues.join(', ')}
          </div>
        )}
      </div>
    );
  };
});

const mockFetchJson = fetchJson as jest.MockedFunction<typeof fetchJson>;

describe('AddReleaseForm', () => {
  const mockLookupData = {
    artists: { items: [{ id: 1, name: 'Metallica' }, { id: 2, name: 'Iron Maiden' }] },
    genres: { items: [{ id: 1, name: 'Metal' }, { id: 2, name: 'Rock' }] },
    labels: { items: [{ id: 1, name: 'Warner' }] },
    countries: { items: [{ id: 1, name: 'USA' }] },
    formats: { items: [{ id: 1, name: 'CD' }] },
    packagings: { items: [{ id: 1, name: 'Jewel Case' }] },
    stores: { items: [{ id: 1, name: 'Best Buy' }] },
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Mock all lookup data requests
    mockFetchJson.mockImplementation((url: string) => {
      if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
      if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
      if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
      if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
      if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
      if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
      if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
      return Promise.resolve({ items: [] });
    });
  });

  describe('Rendering', () => {
    it('shows loading state while fetching lookup data', () => {
      // Make lookup requests never resolve to keep the component in the loading state
      mockFetchJson.mockImplementation(() => new Promise(() => {}));

      render(<AddReleaseForm />);
      expect(screen.getByText('Loading form...')).toBeInTheDocument();
    });

    it('renders form after loading lookup data', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument();
      });

      expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      expect(screen.getByTestId('combobox-artists')).toBeInTheDocument();
      expect(screen.getByTestId('combobox-genres')).toBeInTheDocument();
      expect(screen.getByTestId('combobox-label')).toBeInTheDocument();
    });

    it('renders all form sections', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByText('Basic Information')).toBeInTheDocument();
      });

      expect(screen.getByText('Classification')).toBeInTheDocument();
      expect(screen.getByText('Label Information')).toBeInTheDocument();
    });

    it('shows error message when lookup data fails to load', async () => {
      mockFetchJson.mockRejectedValue(new Error('Network error'));

      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByText(/Failed to load form data/)).toBeInTheDocument();
      });
    });
  });

  describe('ComboBox Integration', () => {
    it('renders Artists ComboBox as required multi-select', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByTestId('combobox-artists')).toBeInTheDocument();
      });

      const combobox = screen.getByTestId('combobox-artists');
      expect(combobox).toHaveTextContent('Artists');
      expect(combobox).toHaveTextContent('*'); // Required indicator

      const select = screen.getByTestId('select-artists');
      expect(select).toHaveAttribute('multiple');
    });

    it('renders Genres ComboBox as multi-select', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByTestId('combobox-genres')).toBeInTheDocument();
      });

      const select = screen.getByTestId('select-genres');
      expect(select).toHaveAttribute('multiple');
    });

    it('renders Label ComboBox as single-select', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByTestId('combobox-label')).toBeInTheDocument();
      });

      const select = screen.getByTestId('select-label');
      expect(select).not.toHaveAttribute('multiple');
    });

    it('renders Format, Packaging, Country as single-select', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByTestId('combobox-format')).toBeInTheDocument();
      });

      expect(screen.getByTestId('select-format')).not.toHaveAttribute('multiple');
      expect(screen.getByTestId('select-packaging')).not.toHaveAttribute('multiple');
      expect(screen.getByTestId('select-country')).not.toHaveAttribute('multiple');
    });
  });

  describe('Form Validation', () => {
    it('shows validation error when title is empty', async () => {
      const user = userEvent.setup();
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      expect(screen.getByText('Title is required')).toBeInTheDocument();
    });

    it('shows validation error when no artists selected', async () => {
      const user = userEvent.setup();
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      expect(screen.getByTestId('error-artists')).toHaveTextContent('At least one artist is required');
    });

    it('clears validation error when field is corrected', async () => {
      const user = userEvent.setup();
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Trigger validation error
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      expect(screen.getByText('Title is required')).toBeInTheDocument();

      // Fix the error
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      await waitFor(() => {
        expect(screen.queryByText('Title is required')).not.toBeInTheDocument();
      });
    });
  });

  describe('Form Submission', () => {
    it('submits form with basic data', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Fill in required fields
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Master of Puppets');

      // Select an artist
      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      // Submit form
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });

    it('submits form with new artist names', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.artistNames).toEqual(['New Band']);
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Fill in title
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Debut Album');

      // Add new artist
      const artistInput = screen.getByTestId('input-new-artists');
      await user.type(artistInput, 'New Band');

      // Submit form
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });

    it('shows error message when submission fails', async () => {
      const user = userEvent.setup();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          return Promise.reject(new Error('Server error'));
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Fill in required fields
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      // Submit form
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/Server error|Failed to create release/)).toBeInTheDocument();
      }, { timeout: 3000 });
    });

    it('disables submit button while submitting', async () => {
      const user = userEvent.setup();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          return new Promise((resolve) => setTimeout(() => resolve({ id: 123 }), 100));
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Fill in required fields
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      // Submit form
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      // Check that button is disabled and shows loading text
      expect(submitButton).toBeDisabled();
      expect(submitButton).toHaveTextContent('Creating...');
    });
  });

  describe('Optional Fields', () => {
    it('allows submitting without optional fields', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.genreIds).toEqual([]);
          expect(body.labelId).toBeUndefined();
          expect(body.countryId).toBeUndefined();
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Fill in only required fields
      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Minimal Album');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      // Submit form
      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });

    it('includes release year when provided', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.releaseYear).toBe('2024-06-15T00:00:00.000Z');
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Album 2024');

      const releaseYearInput = screen.getByLabelText('Release Year');
      // Use a full date string so the date input accepts it in jsdom
      await user.type(releaseYearInput, '2024-06-15');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });

    it('includes live recording flag when checked', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.live).toBe(true);
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Live Album');

      const liveCheckbox = screen.getByLabelText(/Live Recording/);
      await user.click(liveCheckbox);

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });
  });

  describe('Cancel Button', () => {
    it('renders cancel button when onCancel provided', async () => {
      const mockOnCancel = jest.fn();
      render(<AddReleaseForm onCancel={mockOnCancel} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const cancelButton = screen.getByRole('button', { name: /Cancel/ });
      expect(cancelButton).toBeInTheDocument();
    });

    it('does not render cancel button when onCancel not provided', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      expect(screen.queryByRole('button', { name: /Cancel/ })).not.toBeInTheDocument();
    });

    it('calls onCancel when cancel button clicked', async () => {
      const user = userEvent.setup();
      const mockOnCancel = jest.fn();
      render(<AddReleaseForm onCancel={mockOnCancel} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const cancelButton = screen.getByRole('button', { name: /Cancel/ });
      await user.click(cancelButton);

      expect(mockOnCancel).toHaveBeenCalled();
    });
  });

  describe('Copy Release Year Button', () => {
    it('renders copy release year button', async () => {
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const copyButton = screen.getByRole('button', { name: /Copy Release Year to Original Release Year/ });
      expect(copyButton).toBeInTheDocument();
    });

    it('copies release year to original release year when button is clicked', async () => {
      const user = userEvent.setup();
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Set release year
      const releaseYearInput = screen.getByLabelText('Release Year') as HTMLInputElement;
      await user.clear(releaseYearInput);
      await user.type(releaseYearInput, '2024-06-15');

      // Click the copy button
      const copyButton = screen.getByRole('button', { name: /Copy Release Year to Original Release Year/ });
      await user.click(copyButton);

      // Verify original release year was updated
      const origReleaseYearInput = screen.getByLabelText('Original Release Year') as HTMLInputElement;
      expect(origReleaseYearInput.value).toBe('2024-06-15');
    });

    it('does not change original release year when copy button is clicked with empty release year', async () => {
      const user = userEvent.setup();
      render(<AddReleaseForm />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      // Click the copy button without setting release year
      const copyButton = screen.getByRole('button', { name: /Copy Release Year to Original Release Year/ });
      await user.click(copyButton);

      // Verify original release year remains empty
      const origReleaseYearInput = screen.getByLabelText('Original Release Year') as HTMLInputElement;
      expect(origReleaseYearInput.value).toBe('');
    });
  });

  describe('New Value Auto-Creation', () => {
    it('submits with new label name', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.labelName).toBe('New Label');
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      const labelInput = screen.getByTestId('input-new-label');
      await user.type(labelInput, 'New Label');

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });

    it('submits with new genre names', async () => {
      const user = userEvent.setup();
      const mockOnSuccess = jest.fn();

      mockFetchJson.mockImplementation((url: string, options?: any) => {
        if (options?.method === 'POST') {
          const body = JSON.parse(options.body);
          expect(body.genreNames).toEqual(['New Genre']);
          return Promise.resolve({ id: 123 });
        }
        if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
        if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
        if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
        if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
        if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
        if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
        if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
        return Promise.resolve({ items: [] });
      });

      render(<AddReleaseForm onSuccess={mockOnSuccess} />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
      });

      const titleInput = screen.getByLabelText(/Title/);
      await user.type(titleInput, 'Test Album');

      const artistSelect = screen.getByTestId('select-artists');
      await user.selectOptions(artistSelect, '1');

      const genreInput = screen.getByTestId('input-new-genres');
      await user.type(genreInput, 'New Genre');

      const submitButton = screen.getByRole('button', { name: /Create Release/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalledWith(123);
      });
    });
  });
});
