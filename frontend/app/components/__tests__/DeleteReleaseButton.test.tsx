import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { DeleteReleaseButton } from '../DeleteReleaseButton';
import * as api from '../../lib/api';

// Mock the API module
jest.mock('../../lib/api', () => ({
  deleteRelease: jest.fn(),
  ApiError: class ApiError extends Error {
    status?: number;
    details?: unknown;
    url?: string;
  },
}));

describe('DeleteReleaseButton', () => {
  const mockDeleteRelease = api.deleteRelease as jest.MockedFunction<typeof api.deleteRelease>;
  const mockOnDeleteSuccess = jest.fn();
  const mockOnDeleteError = jest.fn();

  const defaultProps = {
    releaseId: 1,
    releaseTitle: 'Test Album',
    onDeleteSuccess: mockOnDeleteSuccess,
    onDeleteError: mockOnDeleteError,
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders delete button with correct label', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const button = screen.getByRole('button', { name: /delete test album from collection/i });
      expect(button).toBeInTheDocument();
      // Button is an icon-only control with an accessible name — assert on accessible name instead of visible text
      expect(button).toHaveAccessibleName(/delete test album from collection/i);
    });

    it('renders trash icon', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const button = screen.getByRole('button', { name: /delete test album from collection/i });
      const svg = button.querySelector('svg');
      expect(svg).toBeInTheDocument();
    });

    it('applies custom className', () => {
      render(<DeleteReleaseButton {...defaultProps} className="custom-class" />);
      
      const button = screen.getByRole('button', { name: /delete test album from collection/i });
      expect(button).toHaveClass('custom-class');
    });
  });

  describe('Confirmation Dialog', () => {
    it('shows confirmation dialog when delete button is clicked', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      expect(screen.getByRole('heading', { name: /delete release/i })).toBeInTheDocument();
      expect(screen.getByText(/are you sure you want to delete "test album"/i)).toBeInTheDocument();
    });

    it('shows release title in confirmation message', () => {
      render(<DeleteReleaseButton {...defaultProps} releaseTitle="My Favorite Album" />);
      
      const deleteButton = screen.getByRole('button', { name: /delete my favorite album from collection/i });
      fireEvent.click(deleteButton);
      
      expect(screen.getByText(/my favorite album/i)).toBeInTheDocument();
    });

    it('closes dialog when cancel button is clicked', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      fireEvent.click(cancelButton);
      
      expect(screen.queryByRole('heading', { name: /delete release/i })).not.toBeInTheDocument();
    });

    it('closes dialog on escape key', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      expect(screen.getByRole('heading', { name: /delete release/i })).toBeInTheDocument();
      
      fireEvent.keyDown(document, { key: 'Escape' });
      
      expect(screen.queryByRole('heading', { name: /delete release/i })).not.toBeInTheDocument();
    });

    it('does not call API when cancel is clicked', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      fireEvent.click(cancelButton);
      
      expect(mockDeleteRelease).not.toHaveBeenCalled();
    });
  });

  describe('Delete Operation', () => {
    it('calls deleteRelease API when confirmed', async () => {
      mockDeleteRelease.mockResolvedValueOnce();
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(mockDeleteRelease).toHaveBeenCalledWith(1);
      });
    });

    it('calls onDeleteSuccess callback on successful deletion', async () => {
      mockDeleteRelease.mockResolvedValueOnce();
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(mockOnDeleteSuccess).toHaveBeenCalled();
      });
    });

    it('closes dialog after successful deletion', async () => {
      mockDeleteRelease.mockResolvedValueOnce();
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: /delete release/i })).not.toBeInTheDocument();
      });
    });

    it('shows loading state during deletion', async () => {
      let resolveDelete: () => void;
      mockDeleteRelease.mockImplementation(() => new Promise(resolve => {
        resolveDelete = resolve as () => void;
      }));
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      
      // Check loading state before resolving - the confirm button should show "Deleting..."
      fireEvent.click(confirmButton);
      
      // Immediately check for loading state - use getAllByRole to check both buttons
      await waitFor(() => {
        const buttons = screen.getAllByRole('button');
        const deletingButton = buttons.find(btn => btn.textContent?.includes('Deleting...'));
        expect(deletingButton).toBeDefined();
      });
      
      // Resolve the promise to clean up
      resolveDelete!();
      
      // Wait for completion
      await waitFor(() => {
        expect(mockDeleteRelease).toHaveBeenCalled();
      });
    });

    it('disables button during deletion', async () => {
      mockDeleteRelease.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)));
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(deleteButton).toBeDisabled();
      });
    });
  });

  describe('Error Handling', () => {
    it('displays error message when deletion fails', async () => {
      const error = new Error('Network error');
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      // Wait for dialog to close and error to appear
      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: /delete release/i })).not.toBeInTheDocument();
      });
      
      await waitFor(() => {
        expect(screen.getByText('Delete Failed')).toBeInTheDocument();
        expect(screen.getByText(/failed to delete release/i)).toBeInTheDocument();
      });
    });

    it('displays 404 error message when release not found', async () => {
      const error = { status: 404, message: 'Not found' };
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.getByText(/release not found/i)).toBeInTheDocument();
      });
    });

    it('displays 500 error message on server error', async () => {
      const error = { status: 500, message: 'Server error' };
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.getByText(/server error/i)).toBeInTheDocument();
      });
    });

    it('displays timeout error message', async () => {
      const error = { message: 'Request timeout after 8000ms' };
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.getByText(/request timed out/i)).toBeInTheDocument();
      });
    });

    it('calls onDeleteError callback on error', async () => {
      const error = { status: 500, message: 'Server error' };
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(mockOnDeleteError).toHaveBeenCalledWith(error);
      });
    });

    it('allows dismissing error message', async () => {
      const error = new Error('Network error');
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.getByText('Delete Failed')).toBeInTheDocument();
      });
      
      const dismissButton = screen.getByRole('button', { name: /dismiss error/i });
      fireEvent.click(dismissButton);
      
      expect(screen.queryByText('Delete Failed')).not.toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('has proper aria-label', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const button = screen.getByRole('button', { name: /delete test album from collection/i });
      expect(button).toHaveAttribute('aria-label', 'Delete Test Album from collection');
    });

    it('has proper title attribute', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const button = screen.getByRole('button', { name: /delete test album from collection/i });
      expect(button).toHaveAttribute('title', 'Delete release from collection');
    });

    it('confirmation dialog has proper ARIA attributes', () => {
      render(<DeleteReleaseButton {...defaultProps} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const dialog = screen.getByRole('dialog');
      expect(dialog).toHaveAttribute('aria-modal', 'true');
      expect(dialog).toHaveAttribute('aria-labelledby', 'dialog-title');
      expect(dialog).toHaveAttribute('aria-describedby', 'dialog-description');
    });
  });

  describe('Component without callbacks', () => {
    it('works without onDeleteSuccess callback', async () => {
      mockDeleteRelease.mockResolvedValueOnce();
      
      const propsWithoutCallback = {
        releaseId: 1,
        releaseTitle: 'Test Album',
      };
      
      render(<DeleteReleaseButton {...propsWithoutCallback} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(mockDeleteRelease).toHaveBeenCalled();
      });
    });

    it('works without onDeleteError callback', async () => {
      const error = new Error('Network error');
      mockDeleteRelease.mockRejectedValueOnce(error);
      
      const propsWithoutCallback = {
        releaseId: 1,
        releaseTitle: 'Test Album',
      };
      
      render(<DeleteReleaseButton {...propsWithoutCallback} />);
      
      const deleteButton = screen.getByRole('button', { name: /delete test album from collection/i });
      fireEvent.click(deleteButton);
      
      const confirmButton = screen.getByRole('button', { name: /delete release/i });
      fireEvent.click(confirmButton);
      
      await waitFor(() => {
        expect(screen.getByText('Delete Failed')).toBeInTheDocument();
      });
    });
  });
});
