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
};

export default nextConfig;
