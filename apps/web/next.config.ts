import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // O design system é um pacote do monorepo (workspace); o Next transpila-o e respeita
  // as fronteiras 'use client' dos componentes Radix (research §2).
  transpilePackages: ['@infolure/design-system'],
  // Feature 006 (US5/FR-010): o limite por omissão dos Server Actions é 1 MB e fazia falhar
  // o upload de fotos > 1 MB. Sobe-se para 5 MB (alinha com o limite do BlobUploadService).
  experimental: {
    serverActions: { bodySizeLimit: '5mb' },
  },
};

export default nextConfig;
