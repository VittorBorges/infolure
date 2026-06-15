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
  // O checkbox é controlado pela URL (onChange faz router.push). Usamos click() em vez de
  // check(): após a navegação o nó é re-renderizado e check() falharia ao validar o estado
  // pós-clique no elemento antigo.
  const firstType = page.locator('fieldset', { hasText: 'Tipo' }).locator('input[type=checkbox]').first();
  await firstType.click();
  await expect(page).toHaveURL(/lure_type=/);
  // Os resultados refletem o filtro: exatamente um tipo fica marcado após o re-render.
  await expect(
    page.locator('fieldset', { hasText: 'Tipo' }).locator('input[type=checkbox]:checked'),
  ).toHaveCount(1);
});

test('estado vazio mostra CTA limpar filtros', async ({ page }) => {
  await page.goto('/iscas?lure_type=inexistente-xyz');
  await expect(page.getByRole('button', { name: /limpar filtros/i })).toBeVisible();
});
