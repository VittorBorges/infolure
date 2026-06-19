import { test, expect } from '@playwright/test';

// Feature 006 (US1) — E2E do interruptor global de indexação SEO.
// Gating de sessão corre sempre; o toggle autenticado exige E2E_ADMIN_STORAGE (senão SKIP).

const ADMIN_STORAGE = process.env.E2E_ADMIN_STORAGE;

test.describe('US1 — gating das definições', () => {
  test('sem sessão, /admin/settings redireciona para o login', async ({ page }) => {
    await page.goto('/admin/settings');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
  });
});

test.describe('US1 — indexação global (autenticado)', () => {
  test.skip(!ADMIN_STORAGE, 'requer E2E_ADMIN_STORAGE (storageState com sessão admin)');
  test.use({ storageState: ADMIN_STORAGE });

  test('alterna o interruptor global de indexação', async ({ page }) => {
    await page.goto('/admin/settings');
    await expect(page.getByRole('heading', { name: 'Indexação SEO (global)' })).toBeVisible();
    await page.getByRole('button', { name: 'Desligar tudo' }).click();
    await expect(page.getByText(/desligada para todo o catálogo/)).toBeVisible();
    await page.getByRole('button', { name: 'Ligar tudo' }).click();
    await expect(page.getByText(/ligada para todo o catálogo/)).toBeVisible();
  });
});
