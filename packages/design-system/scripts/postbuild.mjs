// Pós-build: garante a diretiva 'use client' no topo do bundle.
// O esbuild (via tsup) remove diretivas de módulo ao bundlar; este passo repõe-na de forma
// determinística, marcando o pacote como Client Component para o Next/RSC.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const dir = dirname(fileURLToPath(import.meta.url));
const target = join(dir, '..', 'dist', 'index.js');

const src = readFileSync(target, 'utf8');
const directive = '"use client";';

if (!src.startsWith(directive) && !src.startsWith("'use client'")) {
  writeFileSync(target, `${directive}\n${src}`);
  console.log('[postbuild] "use client" adicionada a dist/index.js');
} else {
  console.log('[postbuild] "use client" já presente');
}
