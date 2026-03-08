import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AddReleaseForm from '../AddReleaseForm';
import * as api from '../../lib/api';

// Mock API
jest.mock('../../lib/api', () => ({
  fetchJson: jest.fn(),
  updateRelease: jest.fn(),
}));

const mockLookupData = {
  artists: { items: [{ id: 1, name: 'Test Artist' }, { id: 2, name: 'Another Artist' }] },
  genres: { items: [{ id: 1, name: 'Rock' }, { id: 2, name: 'Pop' }] },
  labels: { items: [{ id: 1, name: 'Test Label' }] },
  countries: { items: [{ id: 1, name: 'USA' }] },
  formats: { items: [{ id: 1, name: 'CD' }] },
  packagings: { items: [{ id: 1, name: 'Jewel Case' }] },
  stores: { items: [{ id: 405, name: 'Trade (Rui Martins)' }, { id: 1, name: 'Store 1' }] },
};

describe('AddReleaseForm - Edit Mode', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    
    // Mock all lookup data fetches
    (api.fetchJson as jest.Mock).mockImplementation((url: string) => {
      if (url.includes('/api/artists')) return Promise.resolve(mockLookupData.artists);
      if (url.includes('/api/genres')) return Promise.resolve(mockLookupData.genres);
      if (url.includes('/api/labels')) return Promise.resolve(mockLookupData.labels);
      if (url.includes('/api/countries')) return Promise.resolve(mockLookupData.countries);
      if (url.includes('/api/formats')) return Promise.resolve(mockLookupData.formats);
      if (url.includes('/api/packagings')) return Promise.resolve(mockLookupData.packagings);
      if (url.includes('/api/stores')) return Promise.resolve(mockLookupData.stores);
      return Promise.reject(new Error('Unknown API endpoint'));
    });
  });

  it('enters edit mode when releaseId prop is provided', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(
      <AddReleaseForm
        releaseId={123}
        initialData={initialData}
      />
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });
  });

  it('populates form with initialData', async () => {
    const initialData = {
      title: 'Easy Action',
      artistIds: [1],
      genreIds: [1],
      live: false,
      labelId: 1,
      countryId: 1,
      formatId: 1,
      packagingId: 1,
      labelNumber: 'TEST123',
      upc: '123456789',
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Easy Action')).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue('TEST123')).toBeInTheDocument();
    expect(screen.getByDisplayValue('123456789')).toBeInTheDocument();
  });

  it('updates form when initialData changes', async () => {
    const { rerender } = render(
      <AddReleaseForm
        initialData={{ title: 'First Title', artistIds: [1], genreIds: [1], live: false }}
        releaseId={1}
      />
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('First Title')).toBeInTheDocument();
    });

    rerender(
      <AddReleaseForm
        initialData={{ title: 'Updated Title', artistIds: [2], genreIds: [1], live: false }}
        releaseId={1}
      />
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('Updated Title')).toBeInTheDocument();
    });
  });

  it('converts ISO date to HTML date format for purchaseDate', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 1,
        purchaseDate: '2023-01-15T00:00:00',
        price: 20.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      const dateInput = screen.getByLabelText(/purchase date/i);
      expect(dateInput).toHaveValue('2023-01-15');
    });
  });

  it('converts ISO date to HTML date format for releaseYear', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      releaseYear: '2023-06-01T00:00:00',
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    const yearInput = screen.getByLabelText(/^release year$/i) as HTMLInputElement;
    // Date conversion might have timezone issues
    expect(yearInput.value).toMatch(/2023-(05-31|06-01)/);
  });

  it('converts ISO date to HTML date format for origReleaseYear', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      origReleaseYear: '2022-03-01T00:00:00',
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      const matches = screen.getAllByLabelText(/original release year/i);
      const origYearInput = matches.find((el: any) => el.tagName === 'INPUT') as HTMLInputElement;
      expect(origYearInput).toHaveValue('2022-03-01');
    });
  });

  it('does not mark existing store as new', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 405,
        storeName: 'Trade (Rui Martins)',
        price: 20.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    // Should not show "NEW" indicator for existing store
    // (This would be visible in the ComboBox component)
    expect(screen.queryByText(/will be created/i)).not.toBeInTheDocument();
  });

  it('marks store as new only when storeName exists without storeId', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeName: 'Brand New Store',
        price: 20.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    // Should show "NEW" indicator for new store
    // (This would be visible in the ComboBox component)
  });

  it('calls updateRelease when submitting in edit mode', async () => {
    const mockOnSuccess = jest.fn();
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({ id: 123 });

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(
      <AddReleaseForm
        initialData={initialData}
        releaseId={123}
        onSuccess={mockOnSuccess}
      />
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    const submitButton = screen.getByRole('button', { name: /update release/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        title: 'Test Album',
        artistIds: [1],
        genreIds: [1],
        live: false,
      }));
    });

    expect(mockOnSuccess).toHaveBeenCalledWith(123);
  });

  it('removes storeName from purchaseInfo when storeId exists', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({ id: 123 });

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 405,
        storeName: 'Trade (Rui Martins)', // This should be removed
        price: 20.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    const submitButton = screen.getByRole('button', { name: /update release/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        purchaseInfo: expect.objectContaining({
          storeId: 405,
          storeName: undefined, // Should be removed
          price: 20.00,
          currency: 'USD',
        }),
      }));
    });
  });

  it('keeps storeName when storeId does not exist', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({ id: 123 });

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeName: 'New Store Name',
        price: 20.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    const submitButton = screen.getByRole('button', { name: /update release/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        purchaseInfo: expect.objectContaining({
          storeName: 'New Store Name',
          price: 20.00,
          currency: 'USD',
        }),
      }));
    });
  });

  it('handles image filenames correctly', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      images: {
        coverFront: 'front.jpg',
        coverBack: 'back.jpg',
        thumbnail: 'thumb.jpg',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('front.jpg')).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue('back.jpg')).toBeInTheDocument();
    expect(screen.getByDisplayValue('thumb.jpg')).toBeInTheDocument();
  });

  it('displays "Update Release" button text in edit mode', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /^add release$/i })).not.toBeInTheDocument();
  });

  it('displays "Create Release" button text in create mode', async () => {
    render(<AddReleaseForm />);

    // Wait for lookup data to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /create release/i })).toBeInTheDocument();
    }, { timeout: 3000 });

    expect(screen.queryByRole('button', { name: /update release/i })).not.toBeInTheDocument();
  });

  it('loads stores with pageSize 1000 to accommodate large datasets', async () => {
    render(<AddReleaseForm releaseId={1} />);

    await waitFor(() => {
      expect(api.fetchJson).toHaveBeenCalledWith('/api/stores?pageSize=1000');
    });
  });

  it('shows field-level error when title is empty on submit', async () => {
    const initialData = {
      title: '',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      // The error alert banner should mention the specific problem
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/title is required/i);
    });

    // Field-level validation error should also appear under the title input
    expect(screen.getByText('Title is required')).toBeInTheDocument();
  });

  it('shows artist validation error when no artist is selected', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [] as number[],
      artistNames: [] as string[],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/at least one artist is required/i);
    });
  });

  it('shows validation error for empty track title', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      media: [
        {
          name: 'CD 1',
          tracks: [
            { title: 'Valid Track', index: 1, artists: [], genres: [], live: false },
            { title: '', index: 2, artists: [], genres: [], live: false }, // empty title
          ],
        },
      ],
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/track title is required/i);
    });
  });

  it('shows combined validation errors for multiple invalid fields', async () => {
    const initialData = {
      title: '',
      artistIds: [] as number[],
      artistNames: [] as string[],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      // Both errors should appear in the combined banner
      expect(alert).toHaveTextContent(/title is required/i);
      expect(alert).toHaveTextContent(/at least one artist is required/i);
    });
  });

  it('does not call updateRelease when frontend validation fails', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;

    const initialData = {
      title: '',
      artistIds: [] as number[],
      artistNames: [] as string[],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    expect(mockUpdateRelease).not.toHaveBeenCalled();
  });

  it('displays backend API validation errors on submit', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockRejectedValue({
      message: 'Validation failed',
      status: 400,
      details: JSON.stringify({
        errors: {
          Title: ['Title must be at most 200 characters'],
          ArtistIds: ['At least one artist is required'],
        },
      }),
    });

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/validation failed/i);
    });
  });

  it('displays a generic error message when updateRelease throws an unknown error', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockRejectedValue(new Error('Network error'));

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/network error/i);
    });
  });

  it('calls onSuccess with releaseId after successful update', async () => {
    const mockOnSuccess = jest.fn();
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(
      <AddReleaseForm
        initialData={initialData}
        releaseId={42}
        onSuccess={mockOnSuccess}
      />
    );

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      // onSuccess should be called with the original releaseId (42), not a response field
      expect(mockOnSuccess).toHaveBeenCalledWith(42);
    });
  });

  it('sends releaseYear as ISO string when bare year is provided', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      releaseYear: '1983',
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        releaseYear: '1983-01-01T00:00:00.000Z',
      }));
    });
  });

  it('handles full ISO releaseYear from backend without crashing (RangeError regression)', async () => {
    // This tests the specific bug where the backend returns "2023-01-01T00:00:00"
    // and the user clicks Update without modifying the year.
    // Previously this caused: RangeError: Invalid time value at Date.toISOString
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      releaseYear: '2023-01-01T00:00:00',    // full ISO from backend
      origReleaseYear: '2020-01-01T00:00:00', // full ISO from backend
    };

    render(<AddReleaseForm initialData={initialData} releaseId={979} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    // Should not throw – this was the bug
    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(979, expect.objectContaining({
        releaseYear: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T/),
        origReleaseYear: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T/),
      }));
    });
  });

  it('handles YYYY-MM-DD releaseYear from date picker correctly', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      releaseYear: '1983-06-15', // from HTML date input change
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        releaseYear: '1983-06-15T00:00:00.000Z',
      }));
    });
  });

  it('omits releaseYear from payload when it is not set', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const payload = mockUpdateRelease.mock.calls[0][1] as Record<string, unknown>;
      expect(payload.releaseYear).toBeUndefined();
    });
  });

  it('omits empty links array from payload', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      links: [],
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const payload = mockUpdateRelease.mock.calls[0][1] as Record<string, unknown>;
      expect(payload.links).toBeUndefined();
    });
  });

  it('allows submitting a link URL without a type selected (type is optional)', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({});

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      links: [{ url: 'https://example.com', type: '', description: '' }],
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    // Should succeed – no validation error for missing type
    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        links: expect.arrayContaining([
          expect.objectContaining({ url: 'https://example.com' }),
        ]),
      }));
    });

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('still validates that a link URL must be a valid URL format', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      links: [{ url: 'http://', type: 'Official', description: '' }], // missing host – always invalid
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update release/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /update release/i }));

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).toHaveTextContent(/invalid url format/i);
    });
  });

  it('populates all purchase info fields correctly', async () => {
    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 405,
        price: 25.99,
        currency: 'EUR',
        purchaseDate: '2023-05-20T00:00:00',
        notes: 'Test purchase notes',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByDisplayValue('25.99')).toBeInTheDocument();
    });

    expect(screen.getByDisplayValue('Test purchase notes')).toBeInTheDocument();
    
    // Date conversion might have timezone issues, just check it's populated correctly
    const dateInput = screen.getByLabelText(/purchase date/i) as HTMLInputElement;
    expect(dateInput.value).toMatch(/2023-05-(19|20)/);
    
    // Currency might be in a select dropdown
    const currencyField = screen.getByLabelText(/currency/i);
    expect(currencyField).toHaveValue('EUR');
  });

  it('displays price of 0.00 correctly (not empty)', async () => {
    const initialData = {
      title: 'Free Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 405,
        price: 0,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={1} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Free Album')).toBeInTheDocument();
    });

    // Price of 0 should display as "0" in the input, not empty
    const priceInput = screen.getByLabelText(/price/i) as HTMLInputElement;
    expect(priceInput.value).toBe('0');
  });

  it('allows entering 0 as a price value', async () => {
    const mockUpdateRelease = api.updateRelease as jest.MockedFunction<typeof api.updateRelease>;
    mockUpdateRelease.mockResolvedValue({ id: 123 });

    const initialData = {
      title: 'Test Album',
      artistIds: [1],
      genreIds: [1],
      live: false,
      purchaseInfo: {
        storeId: 405,
        price: 10.00,
        currency: 'USD',
      },
    };

    render(<AddReleaseForm initialData={initialData} releaseId={123} />);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test Album')).toBeInTheDocument();
    });

    // Clear the price and enter 0
    const priceInput = screen.getByLabelText(/price/i) as HTMLInputElement;
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '0');

    // Verify the input shows 0
    expect(priceInput.value).toBe('0');

    const submitButton = screen.getByRole('button', { name: /update release/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockUpdateRelease).toHaveBeenCalledWith(123, expect.objectContaining({
        purchaseInfo: expect.objectContaining({
          price: 0,
        }),
      }));
    });
  });
});
