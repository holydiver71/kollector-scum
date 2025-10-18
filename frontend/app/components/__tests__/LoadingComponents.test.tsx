import React from 'react';
import { render, screen } from '@testing-library/react';
import {
  LoadingSpinner,
  LoadingState,
  Skeleton,
} from '../LoadingComponents';

describe('LoadingSpinner Component', () => {
  it('renders with default size', () => {
    const { container } = render(<LoadingSpinner />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
    expect(spinner).toHaveClass('w-6', 'h-6');
  });

  it('renders with small size', () => {
    const { container } = render(<LoadingSpinner size="small" />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toHaveClass('w-4', 'h-4');
  });

  it('renders with large size', () => {
    const { container } = render(<LoadingSpinner size="large" />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toHaveClass('w-8', 'h-8');
  });

  it('renders with blue color by default', () => {
    const { container } = render(<LoadingSpinner />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toHaveClass('text-blue-600');
  });

  it('renders with custom color', () => {
    const { container } = render(<LoadingSpinner color="gray" />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toHaveClass('text-gray-600');
  });
});

describe('LoadingState Component', () => {
  it('renders with loading message', () => {
    render(<LoadingState message="Loading data..." />);
    expect(screen.getByText('Loading data...')).toBeInTheDocument();
  });

  it('renders with default message', () => {
    render(<LoadingState />);
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('includes a spinner', () => {
    const { container } = render(<LoadingState />);
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('renders non-fullscreen by default', () => {
    const { container } = render(<LoadingState />);
    const loadingContainer = container.firstChild;
    expect(loadingContainer).toHaveClass('flex', 'items-center', 'justify-center', 'p-8');
    expect(loadingContainer).not.toHaveClass('fixed');
  });

  it('renders fullscreen when specified', () => {
    const { container } = render(<LoadingState fullScreen />);
    const loadingContainer = container.firstChild;
    expect(loadingContainer).toHaveClass('fixed', 'inset-0');
  });
});

describe('Skeleton Component', () => {
  it('renders with default single line', () => {
    const { container } = render(<Skeleton />);
    const skeletonLines = container.querySelectorAll('.bg-gray-200');
    expect(skeletonLines).toHaveLength(1);
  });

  it('renders multiple lines when specified', () => {
    const { container } = render(<Skeleton lines={3} />);
    const skeletonLines = container.querySelectorAll('.bg-gray-200');
    expect(skeletonLines).toHaveLength(3);
  });

  it('has animate-pulse class for animation', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.firstChild;
    expect(skeleton).toHaveClass('animate-pulse');
  });

  it('applies custom className', () => {
    const { container } = render(<Skeleton className="custom-skeleton" />);
    const skeleton = container.firstChild;
    expect(skeleton).toHaveClass('custom-skeleton');
  });

  it('renders with rounded styling', () => {
    const { container } = render(<Skeleton />);
    const skeletonLine = container.querySelector('.bg-gray-200');
    expect(skeletonLine).toHaveClass('rounded');
  });

  it('makes last line narrower when multiple lines', () => {
    const { container } = render(<Skeleton lines={3} />);
    const skeletonLines = container.querySelectorAll('.bg-gray-200');
    const lastLine = skeletonLines[skeletonLines.length - 1];
    expect(lastLine).toHaveClass('w-3/4');
  });
});
