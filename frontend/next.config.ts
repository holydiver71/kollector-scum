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
    // Next.js image optimisation requires a Node.js server; Cloudflare Pages runs on
    // the edge runtime so we disable server-side optimisation here. Images are
    // already served optimally from Cloudflare R2 via the Worker.
    unoptimized: true,
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
      {
        // Allow staging API host for direct image links served by the API
        protocol: 'https',
        hostname: 'kollector-scum-staging-api.onrender.com',
        pathname: '/**',
      },
      {
        // Allow Cloudflare Worker domains used as image proxy/public gateway
        protocol: 'https',
        hostname: '*.workers.dev',
        pathname: '/**',
      },
      {
        // Allow Cloudflare R2 storage domains (cover art storage)
        protocol: 'https',
        hostname: '*.r2.cloudflarestorage.com',
        pathname: '/**',
      },
    ],
  },
};

export default nextConfig;
