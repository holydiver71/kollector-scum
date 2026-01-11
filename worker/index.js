export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    // remove leading slash
    let key = url.pathname.replace(/^\/+/, '');
    if (!key) return new Response('Bad Request', { status: 400 });

    try {
      // Try direct key first. If not found, try stripping a leading path segment
      // (this handles URLs that include the bucket name as the first segment).
      let object = await env.COVERS.get(key, { type: 'stream' });
      if (!object && key.includes('/')) {
        const altKey = key.replace(/^[^\/]+\//, '');
        object = await env.COVERS.get(altKey, { type: 'stream' });
        if (object) key = altKey;
      }

      if (!object) return new Response('Not Found', { status: 404 });

      const headers = new Headers();
      const contentType = object.httpMetadata?.contentType || 'application/octet-stream';
      headers.set('Content-Type', contentType);
      headers.set('Cache-Control', 'public, max-age=31536000, immutable');
      // Optional CORS - only if your frontend needs cross-origin requests from browsers
      headers.set('Access-Control-Allow-Origin', '*');

      return new Response(object.body, { status: 200, headers });
    } catch (err) {
      return new Response('Internal Error', { status: 500 });
    }
  }
};
