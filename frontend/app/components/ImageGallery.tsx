"use client";
import { useState } from "react";

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

  const getImageUrl = (imagePath?: string) => {
    if (!imagePath) return null;
    
    // If it's already a full URL, return it as-is
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    
    // Otherwise, treat it as a relative path and construct the full URL
    const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5072';
    return `${apiBaseUrl}/api/images/${imagePath}`;
  };

  const handleImageError = (imagePath: string) => {
    setImageError(prev => new Set([...prev, imagePath]));
  };

  const availableImages = [
    { key: 'coverFront', path: images?.coverFront, label: 'Front Cover' },
    { key: 'coverBack', path: images?.coverBack, label: 'Back Cover' }
  ].filter(img => img.path && !imageError.has(img.path));

  const primaryImage = availableImages[0];

  if (!primaryImage) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="aspect-square bg-gray-100 rounded-lg flex items-center justify-center">
          <div className="text-center">
            <span className="text-gray-400 text-6xl block mb-2">🎵</span>
            <p className="text-gray-500 text-sm">No images available</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        {/* Main Image */}
        <div className="aspect-square mb-4">
          <img
            src={getImageUrl(selectedImage || primaryImage.path) || ''}
            alt={`${title} - ${selectedImage ? availableImages.find(img => img.path === selectedImage)?.label : primaryImage.label}`}
            className="w-full h-full object-contain rounded-lg border border-gray-200 bg-white cursor-pointer hover:shadow-lg transition-shadow"
            onError={() => handleImageError(selectedImage || primaryImage.path!)}
            onClick={() => {
              // Open full-size image in a modal or new tab
              const imageUrl = getImageUrl(selectedImage || primaryImage.path);
              if (imageUrl) {
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
                <img
                  src={getImageUrl(image.path) || ''}
                  alt={`${title} - ${image.label}`}
                  className="w-full h-full object-contain bg-white"
                  onError={() => handleImageError(image.path!)}
                />
              </button>
            ))}
          </div>
        )}

        {/* Image Labels */}
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            {selectedImage 
              ? availableImages.find(img => img.path === selectedImage)?.label 
              : primaryImage.label
            }
          </p>
          {availableImages.length > 1 && (
            <p className="text-xs text-gray-500 mt-1">
              Click thumbnails to switch images
            </p>
          )}
        </div>
      </div>

      {/* Full-screen Modal (optional - for future enhancement) */}
      {/* You could add a full-screen image modal here if desired */}
    </>
  );
}
