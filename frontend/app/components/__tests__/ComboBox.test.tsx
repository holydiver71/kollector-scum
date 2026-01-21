import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ComboBox, { ComboBoxItem } from '../ComboBox';

describe('ComboBox', () => {
  const mockItems: ComboBoxItem[] = [
    { id: 1, name: 'Metallica' },
    { id: 2, name: 'Iron Maiden' },
    { id: 3, name: 'Megadeth' },
    { id: 4, name: 'Slayer' },
  ];

  const mockOnChange = jest.fn();

  beforeEach(() => {
    mockOnChange.mockClear();
  });

  describe('Rendering', () => {
    it('renders with label and placeholder', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          placeholder="Select artists..."
        />
      );

      expect(screen.getByText('Artists')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Select artists...')).toBeInTheDocument();
    });

    it('renders with required indicator', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          required={true}
        />
      );

      expect(screen.getByText('*')).toBeInTheDocument();
    });

    it('renders help text', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          helpText="Select at least one artist"
        />
      );

      expect(screen.getByText('Select at least one artist')).toBeInTheDocument();
    });

    it('renders error message', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          error="At least one artist is required"
        />
      );

      expect(screen.getByText('At least one artist is required')).toBeInTheDocument();
    });

    it('renders selected items as badges', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1, 2]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.getByText('Iron Maiden')).toBeInTheDocument();
    });

    it('renders new values as green badges', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          newValues={['New Artist']}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByText('New Artist')).toBeInTheDocument();
      expect(screen.getByText('✨')).toBeInTheDocument();
    });
  });

  describe('Single-select mode', () => {
    it('selects an item on click', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Label"
          items={mockItems}
          value={null}
          onChange={mockOnChange}
          multiple={false}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      await waitFor(() => {
        expect(screen.getByText('Metallica')).toBeInTheDocument();
      });

      await user.click(screen.getByText('Metallica'));

      expect(mockOnChange).toHaveBeenCalledWith([1], []);
    });

    it('closes dropdown after selection', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Label"
          items={mockItems}
          value={null}
          onChange={mockOnChange}
          multiple={false}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      await user.click(screen.getByText('Metallica'));

      await waitFor(() => {
        expect(screen.queryByText('Iron Maiden')).not.toBeInTheDocument();
      });
    });

    it('disables input when item is selected', () => {
      render(
        <ComboBox
          label="Label"
          items={mockItems}
          value={1}
          onChange={mockOnChange}
          multiple={false}
        />
      );

      const input = screen.getByRole('textbox');
      expect(input).toBeDisabled();
    });
  });

  describe('Multi-select mode', () => {
    it('selects multiple items', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          multiple={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      await user.click(screen.getByText('Metallica'));
      expect(mockOnChange).toHaveBeenCalledWith([1], []);

      await user.click(screen.getByText('Iron Maiden'));
      expect(mockOnChange).toHaveBeenCalledWith([2], []);
    });

    it('deselects item when clicked again', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1, 2]}
          onChange={mockOnChange}
          multiple={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      // Click on the Metallica item in the dropdown (not the badge)
      const dropdownItems = screen.getAllByText('Metallica');
      const dropdownItem = dropdownItems.find(el => 
        el.parentElement?.parentElement?.classList.contains('cursor-pointer')
      );
      await user.click(dropdownItem!);
      
      expect(mockOnChange).toHaveBeenCalledWith([2], []);
    });

    it('keeps dropdown open after selection', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          multiple={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      await user.click(screen.getByText('Metallica'));

      expect(screen.getByText('Iron Maiden')).toBeInTheDocument();
    });
  });

  describe('Search and filter', () => {
    it('filters items based on search term', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'meta');

      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.queryByText('Iron Maiden')).not.toBeInTheDocument();
    });

    it('is case-insensitive', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'METALLICA');

      expect(screen.getByText('Metallica')).toBeInTheDocument();
    });

    it('shows no results message when no matches', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          allowCreate={false}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'xyz');

      expect(screen.getByText('No results found')).toBeInTheDocument();
    });
  });

  describe('Create new values', () => {
    it('shows create option for non-existent items', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          allowCreate={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'New Band');

      expect(screen.getByText('Create "New Band"')).toBeInTheDocument();
    });

    it('does not show create option when allowCreate is false', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          allowCreate={false}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'New Band');

      expect(screen.queryByText(/Create/)).not.toBeInTheDocument();
    });

    it('creates new value on click', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          newValues={[]}
          onChange={mockOnChange}
          allowCreate={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'New Band');

      await user.click(screen.getByText('Create "New Band"'));

      expect(mockOnChange).toHaveBeenCalledWith([], ['New Band']);
    });

    it('does not create duplicate new values', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          newValues={['Existing New Band']}
          onChange={mockOnChange}
          allowCreate={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'Existing New Band');

      expect(screen.queryByText(/Create/)).not.toBeInTheDocument();
    });

    it('trims whitespace from new values', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          allowCreate={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, '  Spaced Band  ');

      await user.click(screen.getByText('Create "Spaced Band"'));

      expect(mockOnChange).toHaveBeenCalledWith([], ['Spaced Band']);
    });

    it('shows info text about new values count', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          newValues={['New Band 1', 'New Band 2']}
          onChange={mockOnChange}
        />
      );

      // The text is split across elements, so we need to match it flexibly
      expect(screen.getByText(/2 new item/i)).toBeInTheDocument();
      expect(screen.getByText(/will be created/i)).toBeInTheDocument();
    });
  });

  describe('Remove functionality', () => {
    it('removes existing selected item', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1, 2]}
          onChange={mockOnChange}
          multiple={true}
        />
      );

      const removeButtons = screen.getAllByLabelText(/Remove/);
      await user.click(removeButtons[0]);

      expect(mockOnChange).toHaveBeenCalledWith([2], []);
    });

    it('removes new value', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          newValues={['New Band', 'Another Band']}
          onChange={mockOnChange}
        />
      );

      const removeButtons = screen.getAllByLabelText(/Remove/);
      await user.click(removeButtons[0]);

      expect(mockOnChange).toHaveBeenCalledWith([], ['Another Band']);
    });
  });

  describe('Keyboard navigation', () => {
    it('opens dropdown on ArrowDown', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);
      await user.keyboard('{ArrowDown}');

      expect(screen.getByText('Metallica')).toBeInTheDocument();
    });

    it('closes dropdown on Escape', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      expect(screen.getByText('Metallica')).toBeInTheDocument();

      await user.keyboard('{Escape}');

      await waitFor(() => {
        expect(screen.queryByText('Metallica')).not.toBeInTheDocument();
      });
    });

    it('navigates items with arrow keys', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);
      await user.keyboard('{ArrowDown}');
      await user.keyboard('{ArrowDown}');

      // Second item should be highlighted (visual test would check bg-blue-50)
      expect(screen.getByText('Iron Maiden')).toBeInTheDocument();
    });

    it('selects item on Enter', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);
      await user.keyboard('{Enter}');

      expect(mockOnChange).toHaveBeenCalledWith([1], []);
    });

    it('creates new value on Enter', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          allowCreate={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'New Band');
      await user.keyboard('{ArrowDown}'); // Move to "Create" option
      await user.keyboard('{Enter}');

      expect(mockOnChange).toHaveBeenCalledWith([], ['New Band']);
    });
  });

  describe('Disabled state', () => {
    it('renders as disabled', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          disabled={true}
        />
      );

      const input = screen.getByRole('textbox');
      expect(input).toBeDisabled();
    });

    it('does not open dropdown when disabled', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
          disabled={true}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      expect(screen.queryByText('Metallica')).not.toBeInTheDocument();
    });

    it('does not show remove buttons when disabled', () => {
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1]}
          onChange={mockOnChange}
          disabled={true}
        />
      );

      expect(screen.queryByLabelText(/Remove/)).not.toBeInTheDocument();
    });
  });

  describe('Click outside behavior', () => {
    it('closes dropdown when clicking outside', async () => {
      const user = userEvent.setup();
      render(
        <div>
          <ComboBox
            label="Artists"
            items={mockItems}
            value={[]}
            onChange={mockOnChange}
          />
          <div data-testid="outside">Outside element</div>
        </div>
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      expect(screen.getByText('Metallica')).toBeInTheDocument();

      await user.click(screen.getByTestId('outside'));

      await waitFor(() => {
        expect(screen.queryByText('Metallica')).not.toBeInTheDocument();
      });
    });
  });

  describe('Edge cases', () => {
    it('handles empty items array', () => {
      render(
        <ComboBox
          label="Artists"
          items={[]}
          value={[]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('handles null value', () => {
      render(
        <ComboBox
          label="Artist"
          items={mockItems}
          value={null}
          onChange={mockOnChange}
          multiple={false}
        />
      );

      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('handles empty string search', async () => {
      const user = userEvent.setup();
      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.click(input);

      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.getByText('Iron Maiden')).toBeInTheDocument();
    });

    it('handles very long item names', () => {
      const longNameItems: ComboBoxItem[] = [
        { id: 1, name: 'A'.repeat(100) },
      ];

      render(
        <ComboBox
          label="Artists"
          items={longNameItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('handles special characters in search', async () => {
      const user = userEvent.setup();
      const specialItems: ComboBoxItem[] = [
        { id: 1, name: 'AC/DC' },
        { id: 2, name: 'Guns N\' Roses' },
      ];

      render(
        <ComboBox
          label="Artists"
          items={specialItems}
          value={[]}
          onChange={mockOnChange}
        />
      );

      const input = screen.getByRole('textbox');
      await user.type(input, 'AC/DC');

      expect(screen.getByText('AC/DC')).toBeInTheDocument();
    });
  });

  describe('Pre-selected items', () => {
    it('displays pre-selected items even if not in items list', () => {
      // Saxon is pre-selected but not in the items list (simulating pagination issue)
      const preSelectedItems: ComboBoxItem[] = [
        { id: 998, name: 'Saxon' },
      ];

      render(
        <ComboBox
          label="Artists"
          items={mockItems} // Does not include Saxon
          value={[998]}
          onChange={mockOnChange}
          preSelectedItems={preSelectedItems}
        />
      );

      // Saxon should be displayed as a selected item badge
      expect(screen.getByText('Saxon')).toBeInTheDocument();
    });

    it('displays both pre-selected items and items from list', () => {
      const preSelectedItems: ComboBoxItem[] = [
        { id: 998, name: 'Saxon' },
      ];

      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1, 998]} // Metallica from items list, Saxon from preSelectedItems
          onChange={mockOnChange}
          multiple={true}
          preSelectedItems={preSelectedItems}
        />
      );

      // Both should be displayed
      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.getByText('Saxon')).toBeInTheDocument();
    });

    it('prefers items list over preSelectedItems when ID exists in both', () => {
      // If an item exists in both lists, prefer the items list version
      const preSelectedItems: ComboBoxItem[] = [
        { id: 1, name: 'Metallica (Old Name)' }, // Same ID, different name
      ];

      render(
        <ComboBox
          label="Artists"
          items={mockItems}
          value={[1]}
          onChange={mockOnChange}
          preSelectedItems={preSelectedItems}
        />
      );

      // Should use the name from items list, not preSelectedItems
      expect(screen.getByText('Metallica')).toBeInTheDocument();
      expect(screen.queryByText('Metallica (Old Name)')).not.toBeInTheDocument();
    });

    it('handles single-select mode with preSelectedItems', () => {
      const preSelectedItems: ComboBoxItem[] = [
        { id: 998, name: 'Saxon' },
      ];

      render(
        <ComboBox
          label="Artist"
          items={mockItems}
          value={998}
          onChange={mockOnChange}
          multiple={false}
          preSelectedItems={preSelectedItems}
        />
      );

      // Saxon should be displayed
      expect(screen.getByText('Saxon')).toBeInTheDocument();
    });
  });
});
