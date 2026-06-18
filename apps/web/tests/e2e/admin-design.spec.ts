import { test, expect } from '@playwright/test';

// T011 [US1] — smoke E2E do painel admin no novo design system (Feature 003).
// Cobre o caminho sempre testável (gating de sessão) e, opcionalmente, o render autenticado
// do dashboard/navegação quando é fornecido um storageState com sessão admin.
//
// Pré-requisitos: Next dev em :3000 (e API em :5191 para o render autenticado).
//   - Gating (sem sessão): corre sempre.
//   - Render autenticado: requer E2E_ADMIN_STORAGE=<caminho p/ storageState.json com sessão admin>;
//     sem ele, o teste é SALTADO (skip) em vez de falhar.

const ADMIN_STORAGE = process.env.E2E_ADMIN_STORAGE;

test.describe('US1 — gating de sessão do painel', () => {
  test('sem sessão, /admin redireciona para o login com returnUrl', async ({ page }) => {
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login\?returnUrl=%2Fadmin|\/login\?returnUrl=\/admin/);
  });
});

test.describe('US1 — render do painel autenticado', () => {
  test.skip(!ADMIN_STORAGE, 'requer E2E_ADMIN_STORAGE (storageState com sessão admin)');
  test.use({ storageState: ADMIN_STORAGE });

  test('/admin mostra o dashboard e a navegação no design system', async ({ page }) => {
    await page.goto('/admin');
    // Cabeçalho do dashboard.
    await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();
    // Navegação lateral (links principais).
    await expect(page.getByRole('link', { name: 'Iscas' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Auditoria' })).toBeVisible();
    // Item ativo destacado (aria-current na rota atual).
    await expect(page.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
  });
});
