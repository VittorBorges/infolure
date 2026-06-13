import { test, expect } from '@playwright/test';

// T038 — E2E US-03: página de detalhe (SSR/SEO).
// Pré-requisitos: API em :5191 (com seed) e Next dev em :3000.

test('abre o detalhe a partir de um card e mostra a ficha', async ({ page }) => {
  await page.goto('/iscas');
  await page.getByRole('heading', { level: 3 }).first().click();
  await expect(page).toHaveURL(/\/iscas\/.+/);
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
});

test('detalhe é renderizado no servidor com structured data', async ({ page }) => {
  const res = await page.goto('/iscas/isca-001');
  expect(res?.status()).toBe(200);
  // SSR: o HTML inicial deve conter o JSON-LD de Product
  const html = await res!.text();
  expect(html).toContain('application/ld+json');
  expect(html).toContain('"@type":"Product"');
});

test('slug inexistente devolve 404', async ({ page }) => {
  const res = await page.goto('/iscas/nao-existe-xyz');
  expect(res?.status()).toBe(404);
});
