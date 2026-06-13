import { defineConfig } from '@playwright/test';

// Config E2E (US-01/US-03). Para executar localmente:
//   npm i -D @playwright/test && npx playwright install
//   docker compose up -d  (Postgres/Redis/Typesense)
//   (API em :5191 e `npm run dev` do web em :3000)
//   npx playwright test
export default defineConfig({
  testDir: './tests/e2e',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
  },
});
