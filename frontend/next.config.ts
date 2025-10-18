import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5072',
        pathname: '/api/images/**',
      },
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5000',
        pathname: '/api/images/**',
      },
    ],
    // Enable image optimization
    formats: ['image/webp', 'image/avif'],
  },
  // Enable experimental optimizations
  experimental: {
    optimizePackageImports: ['recharts'],
  },
};

export default nextConfig;
