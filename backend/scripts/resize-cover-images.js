#!/usr/bin/env node
"use strict";

// Resize and recompress images in the covers folder, overwriting originals.
// Usage:
//   node resize-cover-images.js --path /home/andy/music-images/covers --max 1600 --dry
//   node resize-cover-images.js --path /home/andy/music-images/covers --max 1600 --simulate
// `--dry` : quick no-op (lists files, reports current sizes)
// `--simulate` : re-encodes each image to a temp file to measure new sizes without replacing originals
// Install dependency: npm install sharp minimist

const fs = require('fs');
const path = require('path');
const os = require('os');

const argv = require('minimist')(process.argv.slice(2));
const sharp = require('sharp');

const DEFAULT_IMAGES_PATH = '/home/andy/music-images/covers';
const imagesPath = argv.path || DEFAULT_IMAGES_PATH;
const maxDim = parseInt(argv.max || '1600', 10);
const dryRun = !!argv.dry || !!argv['dry-run'];
const simulate = !!argv.simulate || !!argv.s;

function human(bytes) {
  if (bytes === 0) return '0 B';
  const units = ['B','KB','MB','GB','TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + units[i];
}

async function processFile(filePath) {
  const ext = path.extname(filePath).toLowerCase();
  const supported = ['.jpg', '.jpeg', '.png', '.webp'];
  if (!supported.includes(ext)) {
    return { skipped: true };
  }

  const stat = await fs.promises.stat(filePath);
  const originalSize = stat.size;

  // Dry run: don't touch files, just report current sizes
  if (dryRun && !simulate) {
    return { skipped: false, originalSize, newSize: originalSize, processed: false };
  }

  const tempPath = filePath + '.tmp-' + Date.now();

  try {
    const image = sharp(filePath, { failOnError: false }).withMetadata();
    const meta = await image.metadata();

    // Only resize if larger than maxDim, but always re-encode to apply compression
    const needResize = (meta.width && meta.width > maxDim) || (meta.height && meta.height > maxDim);

    let pipeline = image;
    if (needResize) {
      pipeline = pipeline.resize({ width: maxDim, height: maxDim, fit: 'inside', withoutEnlargement: true });
    }

    // Re-encode according to original format to keep filenames unchanged
    if (ext === '.jpg' || ext === '.jpeg') {
      pipeline = pipeline.jpeg({ quality: 80, mozjpeg: true });
    } else if (ext === '.png') {
      pipeline = pipeline.png({ compressionLevel: 9, adaptiveFiltering: true });
    } else if (ext === '.webp') {
      pipeline = pipeline.webp({ quality: 80 });
    }

    await pipeline.toFile(tempPath);

    const newStat = await fs.promises.stat(tempPath);
    const newSize = newStat.size;

    if (simulate) {
      // In simulate mode, remove temp and don't replace original
      try { await fs.promises.unlink(tempPath); } catch (e) {}
      return { skipped: false, originalSize, newSize, processed: true };
    }

    // Replace original atomically
    await fs.promises.rename(tempPath, filePath);

    return { skipped: false, originalSize, newSize, processed: true };
  } catch (err) {
    try { if (await fs.promises.stat(tempPath)) await fs.promises.unlink(tempPath); } catch (e) {}
    return { skipped: false, error: String(err) };
  }
}

async function walkDir(dir) {
  const results = [];
  const entries = await fs.promises.readdir(dir, { withFileTypes: true });
  for (const ent of entries) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      results.push(...await walkDir(full));
    } else if (ent.isFile()) {
      results.push(full);
    }
  }
  return results;
}

(async function main(){
  try {
    console.log(`Images folder: ${imagesPath}`);
    console.log(`Max dimension: ${maxDim}px` + (dryRun ? ' (dry run)' : ''));

    const exists = fs.existsSync(imagesPath);
    if (!exists) {
      console.error('Images path does not exist:', imagesPath);
      process.exit(2);
    }

    const files = await walkDir(imagesPath);
    let totalOriginal = 0;
    let totalNew = 0;
    let processedCount = 0;
    let skippedCount = 0;
    let errorCount = 0;

    for (const f of files) {
      const res = await processFile(f);
      if (!res) continue;
      if (res.skipped) { skippedCount++; continue; }
      if (res.error) { errorCount++; console.error('Error processing', f, res.error); continue; }
      totalOriginal += res.originalSize || 0;
      totalNew += res.newSize || res.originalSize || 0;
      if (res.processed) processedCount++;
    }

    console.log('--- Summary ---');
    console.log('Files scanned:', files.length);
    console.log('Files processed:', processedCount);
    console.log('Files skipped (unsupported):', skippedCount);
    console.log('Errors:', errorCount);
    console.log('Original total size:', human(totalOriginal));
    console.log('New total size     :', human(totalNew));
    const saved = totalOriginal - totalNew;
    const pct = totalOriginal > 0 ? (saved / totalOriginal * 100).toFixed(1) : '0.0';
    console.log('Saved              :', human(saved), `(${pct}%)`);

    if (dryRun) console.log('Dry run completed. No files were modified.');
  } catch (err) {
    console.error('Fatal error:', err);
    process.exit(1);
  }
})();
