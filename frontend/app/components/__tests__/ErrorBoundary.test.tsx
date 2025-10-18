import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ErrorBoundary } from '../ErrorBoundary';

// Component that throws an error
const ThrowError = ({ shouldThrow = false }: { shouldThrow?: boolean }) => {
  if (shouldThrow) {
    throw new Error('Test error');
  }
  return <div>No error</div>;
};

// Suppress console.error for these tests since we're intentionally throwing errors
const originalError = console.error;
beforeAll(() => {
  console.error = jest.fn();
});

afterAll(() => {
  console.error = originalError;
});

describe('ErrorBoundary Component', () => {
  it('renders children when there is no error', () => {
    render(
      <ErrorBoundary>
        <div>Test content</div>
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Test content')).toBeInTheDocument();
  });

  it('renders error UI when an error is thrown', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('displays error message in details', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    expect(screen.getByText(/We apologize for the inconvenience/i)).toBeInTheDocument();
  });

  it('shows error details section', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    const details = screen.getByText('Error Details');
    expect(details).toBeInTheDocument();
  });

  it('displays the actual error message in details', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Test error')).toBeInTheDocument();
  });

  it('renders custom fallback when provided', () => {
    const customFallback = <div>Custom error message</div>;
    
    render(
      <ErrorBoundary fallback={customFallback}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Custom error message')).toBeInTheDocument();
    expect(screen.queryByText('Something went wrong')).not.toBeInTheDocument();
  });

  it('has refresh button in error UI', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    const refreshButton = screen.getByRole('button', { name: /refresh page/i });
    expect(refreshButton).toBeInTheDocument();
  });

  it('displays error icon', () => {
    const { container } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('handles multiple children without error', () => {
    render(
      <ErrorBoundary>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Child 1')).toBeInTheDocument();
    expect(screen.getByText('Child 2')).toBeInTheDocument();
    expect(screen.getByText('Child 3')).toBeInTheDocument();
  });

  it('catches errors from nested components', () => {
    const NestedComponent = () => {
      return (
        <div>
          <ThrowError shouldThrow={true} />
        </div>
      );
    };
    
    render(
      <ErrorBoundary>
        <NestedComponent />
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('error boundary has proper styling classes', () => {
    const { container } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
  });

  it('details section can be toggled', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );
    
    const details = screen.getByText('Error Details').closest('details');
    expect(details).toBeInTheDocument();
  });

  it('renders without custom fallback', () => {
    render(
      <ErrorBoundary>
        <div>Content</div>
      </ErrorBoundary>
    );
    
    expect(screen.getByText('Content')).toBeInTheDocument();
  });
});
