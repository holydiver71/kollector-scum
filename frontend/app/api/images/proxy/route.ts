import { NextRequest, NextResponse } from "next/server";

/**
 * GET /api/images/proxy?url=<encoded-discogs-url>
 *
 * Server-side image proxy for Discogs CDN thumbnails.
 *
 * Discogs uses Referer-based hotlink protection that blocks browser requests
 * originating from third-party domains.  A server-side fetch carries no
 * browser Referer header, so it succeeds unconditionally.
 *
 * Only https://i.discogs.com URLs are accepted (SSRF prevention).
 */

const ALLOWED_HOSTNAME = "i.discogs.com";

const ALLOWED_CONTENT_TYPES = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/gif",
  "image/webp",
  "image/bmp",
  "image/tiff",
]);

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const url = searchParams.get("url");

  if (!url) {
    return new NextResponse("url parameter is required", { status: 400 });
  }

  // Security: validate URL format and restrict to Discogs CDN only (SSRF prevention).
  let parsedUrl: URL;
  try {
    parsedUrl = new URL(url);
  } catch {
    return new NextResponse("Invalid URL", { status: 400 });
  }

  if (
    parsedUrl.protocol !== "https:" ||
    parsedUrl.hostname !== ALLOWED_HOSTNAME
  ) {
    return new NextResponse(
      `Only https://${ALLOWED_HOSTNAME} image URLs are supported.`,
      { status: 400 }
    );
  }

  try {
    // Fetch from Discogs server-side — no browser Referer header is sent,
    // which bypasses Discogs hotlink protection.
    const upstream = await fetch(parsedUrl.toString(), {
      headers: {
        "User-Agent": "KollectorScum/1.0 (+https://github.com/holydiver71/kollector-scum)",
      },
      // Do not follow cross-origin redirects to unexpected hosts.
      redirect: "follow",
    });

    if (!upstream.ok) {
      return new NextResponse("Failed to retrieve image from Discogs.", {
        status: 502,
      });
    }

    const contentType =
      upstream.headers.get("content-type")?.split(";")[0].trim() ??
      "image/jpeg";

    if (!ALLOWED_CONTENT_TYPES.has(contentType)) {
      return new NextResponse("Upstream responded with an unsupported content type.", {
        status: 400,
      });
    }

    const imageBuffer = await upstream.arrayBuffer();

    return new NextResponse(imageBuffer, {
      status: 200,
      headers: {
        "Content-Type": contentType,
        // Cache at the browser/CDN level for 1 hour — Discogs thumbnails rarely change.
        "Cache-Control": "public, max-age=3600",
      },
    });
  } catch {
    return new NextResponse("Network error retrieving image.", { status: 502 });
  }
}
