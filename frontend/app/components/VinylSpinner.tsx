import React from 'react';

interface VinylSpinnerProps {
  size?: 'small' | 'medium' | 'large';
  message?: string;
}

/**
 * A cool animated vinyl record spinner for loading states
 * Features a spinning record with grooves, center label, and optional loading message
 */
export const VinylSpinner: React.FC<VinylSpinnerProps> = ({ 
  size = 'large',
  message = 'Loading your collection...'
}) => {
  const sizeClasses = {
    small: 'w-16 h-16',
    medium: 'w-32 h-32',
    large: 'w-48 h-48'
  };

  const containerSizeClasses = {
    small: 'w-20 h-20',
    medium: 'w-36 h-36',
    large: 'w-52 h-52'
  };

  return (
    <div className="flex flex-col items-center justify-center py-12 px-8 mt-8">
      {/* Vinyl Record Container with spinning animation */}
      <div className={`relative ${containerSizeClasses[size]} mb-6`}>
        {/* Outer glow effect */}
        <div className="absolute inset-0 bg-gradient-to-br from-red-500/20 via-orange-500/20 to-red-600/20 rounded-full blur-xl animate-pulse"></div>
        
        {/* Main vinyl record - spinning */}
        <div className={`relative ${sizeClasses[size]} mx-auto animate-spin-slow`}>
          <svg viewBox="0 0 200 200" className="w-full h-full drop-shadow-2xl">
            {/* Outer edge - black vinyl */}
            <circle
              cx="100"
              cy="100"
              r="98"
              fill="#1a1a1a"
              stroke="#000"
              strokeWidth="1"
            />
            
            {/* Vinyl grooves - multiple concentric circles */}
            {Array.from({ length: 30 }).map((_, i) => {
              const radius = 92 - (i * 2.8);
              return (
                <circle
                  key={i}
                  cx="100"
                  cy="100"
                  r={radius}
                  fill="none"
                  stroke="#2a2a2a"
                  strokeWidth="0.8"
                  opacity={0.6}
                />
              );
            })}
            
            {/* Inner shine rings */}
            {Array.from({ length: 3 }).map((_, i) => {
              const radius = 85 - (i * 8);
              return (
                <circle
                  key={`shine-${i}`}
                  cx="100"
                  cy="100"
                  r={radius}
                  fill="none"
                  stroke="#3a3a3a"
                  strokeWidth="1.5"
                  opacity={0.4}
                />
              );
            })}

            {/* Center label - red/orange gradient */}
            <defs>
              <radialGradient id="labelGradient" cx="50%" cy="50%">
                <stop offset="0%" stopColor="#D93611" />
                <stop offset="50%" stopColor="#F28A2E" />
                <stop offset="100%" stopColor="#D9601A" />
              </radialGradient>
              <radialGradient id="centerHole" cx="50%" cy="50%">
                <stop offset="0%" stopColor="#1a1a1a" />
                <stop offset="100%" stopColor="#000" />
              </radialGradient>
            </defs>
            
            {/* Center label circle */}
            <circle
              cx="100"
              cy="100"
              r="35"
              fill="url(#labelGradient)"
              stroke="#000"
              strokeWidth="1"
            />
            
            {/* Label text ring */}
            <circle
              cx="100"
              cy="100"
              r="30"
              fill="none"
              stroke="#fff"
              strokeWidth="0.5"
              opacity="0.3"
            />
            
            {/* Center spindle hole */}
            <circle
              cx="100"
              cy="100"
              r="8"
              fill="url(#centerHole)"
              stroke="#000"
              strokeWidth="1"
            />
            
            {/* Shine/highlight effect on label */}
            <ellipse
              cx="95"
              cy="90"
              rx="15"
              ry="10"
              fill="#fff"
              opacity="0.15"
              transform="rotate(-30 95 90)"
            />
          </svg>
        </div>

        {/* Tone arm decoration - positioned over the record like it's playing */}
        <div className="absolute top-1/4 right-0 w-24 h-1.5 bg-gradient-to-l from-gray-400 via-gray-600 to-gray-800 rounded-full transform origin-right shadow-lg" style={{ transform: 'rotate(-35deg) translateX(-20%)' }}>
          {/* Cartridge/needle at the end */}
          <div className="absolute left-0 w-4 h-4 bg-gradient-to-br from-gray-700 to-gray-900 rounded-sm shadow-lg" style={{ transform: 'translateX(-50%) translateY(-25%)' }}>
            <div className="absolute bottom-0 left-1/2 w-0.5 h-2 bg-gray-400 transform -translate-x-1/2"></div>
          </div>
          {/* Pivot point */}
          <div className="absolute right-0 w-3 h-3 bg-gray-700 rounded-full shadow-md" style={{ transform: 'translateY(-25%)' }}></div>
        </div>
      </div>

      {/* Loading message */}
      {message && (
        <div className="text-center">
          <p className="text-white text-lg font-medium mb-2 animate-pulse">
            {message}
          </p>
          <div className="flex items-center justify-center gap-1">
            <span className="w-2 h-2 bg-orange-500 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></span>
            <span className="w-2 h-2 bg-orange-500 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></span>
            <span className="w-2 h-2 bg-orange-500 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></span>
          </div>
        </div>
      )}
    </div>
  );
};

// Full-screen loading overlay component
export const VinylLoadingOverlay: React.FC<{ message?: string }> = ({ message }) => {
  return (
    <div className="fixed inset-0 bg-gradient-to-br from-red-900 via-red-950 to-black flex items-center justify-center z-50">
      <VinylSpinner size="large" message={message} />
    </div>
  );
};
