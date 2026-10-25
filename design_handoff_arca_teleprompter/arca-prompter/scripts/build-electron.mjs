// Compila electron/*.ts (processo principal e preload) para dist-electron/ com esbuild.
import { build } from 'esbuild';

const common = { bundle: true, platform: 'node', target: 'node20', format: 'cjs', external: ['electron', 'bufferutil', 'utf-8-validate'], sourcemap: false, logLevel: 'info' };
await build({ ...common, entryPoints: ['electron/main.ts'], outfile: 'dist-electron/main.cjs', minify: true });
await build({ ...common, entryPoints: ['electron/preload.ts'], outfile: 'dist-electron/preload.cjs' });
