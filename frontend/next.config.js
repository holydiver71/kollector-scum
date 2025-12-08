const isDev = process.env.NODE_ENV !== 'production';

module.exports = {
  async rewrites() {
    // Enable a dev-only proxy so frontend requests to /api/* are forwarded
    // to the local backend at 127.0.0.1:5072. This keeps same-origin API
    // paths during development without requiring per-developer env setup.
    if (!isDev) return [];
    return [
      {
        source: '/api/:path*',
        // Use 127.0.0.1 (IPv4) to avoid IPv6 binding mismatches on some hosts
        destination: 'http://127.0.0.1:5072/api/:path*',
      },
    ];
  },
};
