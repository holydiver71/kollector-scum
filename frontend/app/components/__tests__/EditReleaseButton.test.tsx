import { render, screen } from '@testing-library/react';
import { EditReleaseButton } from '../EditReleaseButton';
import { useRouter } from 'next/navigation';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}));

const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;

describe('EditReleaseButton', () => {
  const mockPush = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    } as any);
  });

  it('renders the edit button with correct label', () => {
    render(
      <EditReleaseButton
        releaseId={123}
        releaseTitle="Test Album"
      />
    );

    const button = screen.getByRole('button', { name: /edit test album/i });
    expect(button).toBeInTheDocument();
  });

  it('displays edit icon', () => {
    render(
      <EditReleaseButton
        releaseId={123}
        releaseTitle="Test Album"
      />
    );

    const button = screen.getByRole('button');
    const svg = button.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('navigates to edit page when clicked', () => {
    render(
      <EditReleaseButton
        releaseId={456}
        releaseTitle="Another Album"
      />
    );

    const button = screen.getByRole('button');
    fireEvent.click(button);

    expect(mockPush).toHaveBeenCalledWith('/releases/456/edit');
  });

  it('has correct accessibility attributes', () => {
    render(
      <EditReleaseButton
        releaseId={789}
        releaseTitle="My Album"
      />
    );

    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('aria-label', 'Edit My Album');
    expect(button).toHaveAttribute('title', 'Edit release');
  });

  it('has correct data-testid', () => {
    render(
      <EditReleaseButton
        releaseId={123}
        releaseTitle="Test"
      />
    );

    expect(screen.getByTestId('edit-release-button')).toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(
      <EditReleaseButton
        releaseId={123}
        releaseTitle="Test"
        className="custom-class"
      />
    );

    const button = screen.getByRole('button');
    expect(button).toHaveClass('custom-class');
  });

  it('has blue styling', () => {
    render(
      <EditReleaseButton
        releaseId={123}
        releaseTitle="Test"
      />
    );

    const button = screen.getByRole('button');
    expect(button).toHaveClass('bg-blue-600');
    expect(button).toHaveClass('hover:bg-blue-700');
  });
});
