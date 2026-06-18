import { defineConfig } from 'tsup';

// Build do pacote de design system: ESM + tipos.
// A diretiva 'use client' é garantida por um passo pós-build (scripts/postbuild.mjs),
// porque o esbuild remove diretivas de módulo ao bundlar. Ver research §2.
export default defineConfig({
  entry: ['src/index.ts'],
  format: ['esm'],
  dts: true,
  sourcemap: true,
  clean: true,
  external: ['react', 'react-dom'],
});
