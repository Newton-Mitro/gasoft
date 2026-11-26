import { gzipSync } from 'node:zlib';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const outputDirectory = fileURLToPath(new URL('../dist/gasoft-frontend/browser/', import.meta.url));
const index = readFileSync(resolve(outputDirectory, 'index.html'), 'utf8');
const assets = new Set(
  [...index.matchAll(/(?:src|href)="([^"]+\.(?:js|css))"/g)]
    .map(([, asset]) => asset)
    .filter(asset => !asset.startsWith('http') && !asset.startsWith('//')),
);

if (assets.size === 0) {
  throw new Error('Could not find initial JavaScript and CSS assets in the production index.');
}

const gzipBytes = [...assets].reduce((total, asset) => {
  const contents = readFileSync(resolve(outputDirectory, asset));
  return total + gzipSync(contents, { level: 6 }).byteLength;
}, 0);
const budgetBytes = 180 * 1024;

console.info(`Initial assets gzip size: ${(gzipBytes / 1024).toFixed(1)} KiB / 180 KiB`);
if (gzipBytes > budgetBytes) {
  throw new Error(`Initial compressed assets exceed the 180 KiB gzip budget by ${gzipBytes - budgetBytes} bytes.`);
}
