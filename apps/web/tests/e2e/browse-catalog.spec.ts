import { test, expect } from '@playwright/test';

// T022 — E2E US-01: navegação e filtro do catálogo.
// Pré-requisitos: API em :5191 (com seed/índice) e Next dev em :3000.
// Setup local: `npm i -D @playwright/test && npx playwright install` (não executado nesta sessão).

test('lista o catálogo e mostra cards de iscas', async ({ page }) => {
  await page.goto('/iscas');
  await expect(page.getByRole('heading', { level: 3 }).first()).toBeVisible();
  await expect(page.getByText(/iscas$/)).toBeVisible(); // contador "N iscas"
});

test('filtrar por tipo atualiza a URL e os resultados', async ({ page }) => {
  await page.goto('/iscas');
  // Marca o primeiro filtro de tipo disponível
  const firstType = page.locator('fieldset', { hasText: 'Tipo' }).locator('input[type=checkbox]').first();
  await firstType.check();
  await expect(page).toHaveURL(/lure_type=/);
});

test('estado vazio mostra CTA limpar filtros', async ({ page }) => {
  await page.goto('/iscas?lure_type=inexistente-xyz');
  await expect(page.getByRole('button', { name: /limpar filtros/i })).toBeVisible();
});
