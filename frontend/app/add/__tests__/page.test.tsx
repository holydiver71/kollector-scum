import React from 'react';
import { render, screen } from '@testing-library/react';
import AddReleasePage from '../page';

describe('AddReleasePage', () => {
  it('renders the page header', () => {
    render(<AddReleasePage />);
    
    expect(screen.getByRole('heading', { name: 'Add Release', level: 1 })).toBeInTheDocument();
    expect(screen.getByText('Add a new music release to your collection')).toBeInTheDocument();
  });

  it('renders the main heading', () => {
    render(<AddReleasePage />);
    
    expect(screen.getByRole('heading', { name: 'Add New Release', level: 2 })).toBeInTheDocument();
  });

  it('displays the description text', () => {
    render(<AddReleasePage />);
    
    expect(screen.getByText(/Form to add new music releases with all metadata/)).toBeInTheDocument();
  });

  it('shows future enhancement note', () => {
    render(<AddReleasePage />);
    
    expect(screen.getByText(/Future Enhancement:/)).toBeInTheDocument();
    expect(screen.getByText(/CRUD operations for music releases/)).toBeInTheDocument();
  });

  it('renders the plus icon SVG', () => {
    const { container } = render(<AddReleasePage />);
    
    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
    expect(svg).toHaveClass('mx-auto');
  });

  it('has proper page structure', () => {
    const { container } = render(<AddReleasePage />);
    
    expect(container.querySelector('.max-w-7xl')).toBeInTheDocument();
    expect(container.querySelector('.bg-white')).toBeInTheDocument();
    expect(container.querySelector('.rounded-lg')).toBeInTheDocument();
  });
});
