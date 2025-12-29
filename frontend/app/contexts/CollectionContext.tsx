"use client";

import React, { createContext, useContext, useState, useEffect } from 'react';
import { isAuthenticated } from '../lib/auth';

interface CollectionContextType {
  hasCollection: boolean | null; // null = loading, true = has releases, false = empty
  setHasCollection: (value: boolean) => void;
  isReady: boolean; // true when we've checked
}

const CollectionContext = createContext<CollectionContextType>({
  hasCollection: null,
  setHasCollection: () => {},
  isReady: false,
});

export function CollectionProvider({ children }: { children: React.ReactNode }) {
  const [hasCollection, setHasCollection] = useState<boolean | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    // Reset when auth changes
    const handleAuthChange = () => {
      if (!isAuthenticated()) {
        setHasCollection(null);
        setIsReady(false);
      }
    };

    window.addEventListener('authChanged', handleAuthChange);
    
    return () => {
      window.removeEventListener('authChanged', handleAuthChange);
    };
  }, []);

  const contextValue: CollectionContextType = {
    hasCollection,
    setHasCollection: (value: boolean) => {
      setHasCollection(value);
      setIsReady(true);
    },
    isReady,
  };

  return (
    <CollectionContext.Provider value={contextValue}>
      {children}
    </CollectionContext.Provider>
  );
}

export function useCollection() {
  const context = useContext(CollectionContext);
  if (!context) {
    throw new Error('useCollection must be used within CollectionProvider');
  }
  return context;
}
