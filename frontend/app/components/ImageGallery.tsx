"use client";
import { useState } from "react";
import Image from "next/image";

interface ReleaseImages {
  coverFront?: string;
  coverBack?: string;
  thumbnail?: string;
}

interface ImageGalleryProps {
  images?: ReleaseImages;
  title: string;
}

export function ImageGallery({ images, title }: ImageGalleryProps) {
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [imageError, setImageError] = useState<Set<string>>(new Set());
  const placeholderImage = '/placeholder-album.svg';

  const getImageUrl = (imagePath?: string) => {
    if (!imagePath) return placeholderImage;
    
    // If it's already a full URL, return it as-is
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    
    // If it starts with /cover-art/, it's already a full path (multi-tenant storage)
    if (imagePath.startsWith('/cover-art/')) {
      const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
      return `${apiBaseUrl}${imagePath}`;
    }
    
    // Otherwise, treat it as a relative path and construct the full URL with /api/images/
    const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
    return `${apiBaseUrl}/api/images/${imagePath}`;
  };

  const handleImageError = (imagePath: string, event: React.SyntheticEvent<HTMLImageElement>) => {
    setImageError(prev => new Set([...prev, imagePath]));
    // Set the image src to the placeholder
    event.currentTarget.src = placeholderImage;
  };

  const availableImages = [
    { key: 'coverFront', path: images?.coverFront, label: 'Front Cover' },
    { key: 'coverBack', path: images?.coverBack, label: 'Back Cover' }
  ].filter(img => img.path && !imageError.has(img.path));

  const primaryImage = availableImages[0] || { key: 'placeholder', path: undefined, label: 'Album Cover' };

  // Always show the image gallery, even if no images available
  if (!primaryImage.path) {
    return (
      <div className="bg-white rounded-lg">
        <div className="aspect-square">
          <Image
            src={placeholderImage}
            alt={`${title} - Album Cover`}
            width={500}
            height={500}
            sizes="(max-width: 768px) 100vw, 50vw"
            className="w-full h-full object-contain rounded-lg bg-gray-50"
          />
        </div>
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-500">No cover image available</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="bg-white rounded-lg">
        {/* Main Image */}
        <div className="aspect-square mb-4">
          <Image
            src={getImageUrl(selectedImage || primaryImage.path) || placeholderImage}
            alt={`${title} - ${selectedImage ? availableImages.find(img => img.path === selectedImage)?.label : primaryImage.label}`}
            width={500}
            height={500}
            sizes="(max-width: 768px) 100vw, 50vw"
            className="w-full h-full object-contain rounded-lg bg-white cursor-pointer hover:shadow-lg transition-shadow"
            onError={(e) => handleImageError(selectedImage || primaryImage.path!, e)}
            onClick={() => {
              // Open full-size image in a modal or new tab
              const imageUrl = getImageUrl(selectedImage || primaryImage.path);
              if (imageUrl && imageUrl !== placeholderImage) {
                window.open(imageUrl, '_blank');
              }
            }}
          />
        </div>

        {/* Image Thumbnails */}
        {availableImages.length > 1 && (
          <div className="grid grid-cols-3 gap-2">
            {availableImages.map((image) => (
              <button
                key={image.key}
                onClick={() => setSelectedImage(image.path!)}
                className={`aspect-square rounded border-2 overflow-hidden transition-all ${
                  (selectedImage || primaryImage.path) === image.path
                    ? 'border-blue-500 ring-2 ring-blue-200'
                    : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <Image
                  src={getImageUrl(image.path) || placeholderImage}
                  alt={`${title} - ${image.label}`}
                  width={150}
                  height={150}
                  sizes="33vw"
                  className="w-full h-full object-contain bg-white"
                  onError={(e) => handleImageError(image.path!, e)}
                />
              </button>
            ))}
          </div>
        )}

        {/* Image Labels */}
        {availableImages.length > 1 && (
          <div className="mt-4 text-center">
            <p className="text-xs text-gray-500 mt-1">
              Click thumbnails to switch images
            </p>
          </div>
        )}
      </div>

      {/* Full-screen Modal (optional - for future enhancement) */}
      {/* You could add a full-screen image modal here if desired */}
    </>
  );
}
