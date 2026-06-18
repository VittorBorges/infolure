import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // O design system é um pacote do monorepo (workspace); o Next transpila-o e respeita
  // as fronteiras 'use client' dos componentes Radix (research §2).
  transpilePackages: ['@infolure/design-system'],
};

export default nextConfig;
