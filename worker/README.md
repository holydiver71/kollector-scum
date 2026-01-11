Cloudflare Worker for serving `cover-art` R2 objects

This worker reads objects from the bound R2 bucket and returns them with appropriate headers.

Quick deploy

1. Install Wrangler:

```bash
npm install -g wrangler
```

2. Login:

```bash
wrangler login
```

3. Edit `wrangler.toml` and set your `account_id`.

4. Publish (workers_dev):

```bash
cd worker
wrangler publish
```

Wrangler will print the worker URL, e.g. `https://kollector-images.YOUR_ACCOUNT.workers.dev`.

Set `R2__PublicBaseUrl` in your API environment to the worker origin (or custom domain).

Example `R2__PublicBaseUrl` to set:

```
R2__PublicBaseUrl=https://kollector-images.YOUR_ACCOUNT.workers.dev
```

Usage

The app expects object URLs like:

`<R2__PublicBaseUrl>/cover-art-staging/{userId}/{objectName}`

Testing

```bash
curl -I "https://kollector-images.YOUR_ACCOUNT.workers.dev/cover-art-staging/6419cd5f-.../5faa63a2-....jpg"
```

Security notes

- `workers_dev` publishes to a Cloudflare-managed subdomain for quick testing. For production, add a custom domain and point it at the worker.
- If you prefer not to make images globally public, implement signed/presigned URLs in the backend instead.
