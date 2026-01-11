"use client";

import React from 'react';
import { GoogleOAuthProvider } from '@react-oauth/google';

interface Props {
  clientId: string;
  children: React.ReactNode;
}

export default function GoogleProviderWrapper({ clientId, children }: Props) {
  return (
    <GoogleOAuthProvider clientId={clientId}>
      {children}
    </GoogleOAuthProvider>
  );
}
