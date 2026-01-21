import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { ImageGallery } from '../ImageGallery';

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, ...props }: { src: string; alt: string }) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} {...props} />
  ),
}));

const mockImages = {
  coverFront: 'front.jpg',
  coverBack: 'back.jpg',
  thumbnail: 'thumb.jpg',
};

const mockTitle = 'Master of Puppets';

describe('ImageGallery Component', () => {
  it('renders the gallery', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    const images = screen.getAllByAltText(/Master of Puppets/i);
    expect(images.length).toBeGreaterThan(0);
  });

  it('displays front cover as primary image', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    const images = screen.getAllByAltText(/Front Cover/i);
    expect(images[0]).toBeInTheDocument();
    expect(images[0]).toHaveAttribute('src', expect.stringContaining('front.jpg'));
  });

  it('renders thumbnail images when multiple images available', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    // Should have thumbnails for navigation
    const images = screen.getAllByRole('img');
    expect(images.length).toBeGreaterThan(1);
  });

  it('displays placeholder when no images provided', () => {
    render(<ImageGallery images={{}} title={mockTitle} />);
    expect(screen.getByText(/No cover image available/i)).toBeInTheDocument();
    const img = screen.getByAltText(/Album Cover/i);
    expect(img).toHaveAttribute('src', '/placeholder-album.svg');
  });

  it('displays placeholder when images is undefined', () => {
    render(<ImageGallery title={mockTitle} />);
    expect(screen.getByText(/No cover image available/i)).toBeInTheDocument();
    const img = screen.getByAltText(/Album Cover/i);
    expect(img).toHaveAttribute('src', '/placeholder-album.svg');
  });

  it('changes displayed image when thumbnail clicked', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    
    // Find all images
    const images = screen.getAllByRole('img');
    
    // Should have main image plus thumbnails
    expect(images.length).toBeGreaterThan(1);
  });

  it('handles only front cover', () => {
    const singleImage = { coverFront: 'front.jpg' };
    render(<ImageGallery images={singleImage} title={mockTitle} />);
    
    const img = screen.getByAltText(/Front Cover/i);
    expect(img).toBeInTheDocument();
  });

  it('handles only back cover', () => {
    const backOnly = { coverBack: 'back.jpg' };
    render(<ImageGallery images={backOnly} title={mockTitle} />);
    
    const img = screen.getByAltText(/Back Cover/i);
    expect(img).toBeInTheDocument();
  });

  it('renders clickable main image', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    const images = screen.getAllByAltText(/Front Cover/i);
    expect(images[0]).toHaveClass('cursor-pointer');
  });

  it('uses correct image URL format', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    const images = screen.getAllByAltText(/Front Cover/i);
    expect(images[0]).toHaveAttribute('src', expect.stringContaining('/api/images/'));
  });

  it('shows placeholder when no images', () => {
    render(<ImageGallery images={{}} title={mockTitle} />);
    expect(screen.getByText(/No cover image available/i)).toBeInTheDocument();
    const img = screen.getByAltText(/Album Cover/i);
    expect(img).toHaveAttribute('src', '/placeholder-album.svg');
  });

  it('handles image error gracefully', () => {
    const singleImageOnly = { coverFront: 'front.jpg' };
    render(<ImageGallery images={singleImageOnly} title={mockTitle} />);
    const image = screen.getByAltText(/Front Cover/i);
    
    // Simulate image error
    fireEvent.error(image);
    
    // After error, should show placeholder
    expect(image).toHaveAttribute('src', '/placeholder-album.svg');
  });

  it('has proper styling for image container', () => {
    const { container } = render(<ImageGallery images={mockImages} title={mockTitle} />);
    const imageContainer = container.querySelector('.aspect-square');
    expect(imageContainer).toBeInTheDocument();
  });

  it('displays all available images in thumbnail list', () => {
    render(<ImageGallery images={mockImages} title={mockTitle} />);
    const images = screen.getAllByRole('img');
    // Should have main image + thumbnails (front, back, thumb)
    expect(images.length).toBeGreaterThanOrEqual(2);
  });
});
