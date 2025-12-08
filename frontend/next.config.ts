import type { NextConfig } from "next";
import path from 'path';

const nextConfig: NextConfig = {
  // Explicit Turbopack root keeps the build deterministic when multiple lockfiles
  // exist in parent folders. Without this, Next may pick the wrong workspace root.
  turbopack: {
    // Turbopack expects an absolute path for the project root.
    root: path.resolve(__dirname),
  },
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'i.discogs.com',
        pathname: '/**',
      },
    ],
  },
  async rewrites() {
    // Keep rewrites dev-only — forward /api/* to the backend running locally.
    if (process.env.NODE_ENV === 'production') return [];
    return [
      {
        source: '/api/:path*',
        destination: 'http://127.0.0.1:5072/api/:path*',
      },
    ];
  },
};

export default nextConfig;
