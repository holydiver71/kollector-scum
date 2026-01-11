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
      {
        // Allow local backend image proxy on any port (dev/test). Using protocol 'http' here
        // to match local API URLs (e.g. http://localhost:8080/api/images/...). Omitting
        // `port` lets Next accept any localhost port.
        protocol: 'http',
        hostname: 'localhost',
        pathname: '/api/images/**',
      },
      {
        // Allow multi-tenant cover art storage (local filesystem)
        protocol: 'http',
        hostname: 'localhost',
        pathname: '/cover-art/**',
      },
    ],
  },
};

export default nextConfig;
