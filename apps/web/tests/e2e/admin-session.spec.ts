import { test, expect } from '@playwright/test';

// Feature 007 — identidade da sessão no painel admin (US1) + terminar sessão (US2).
// O gating de sessão corre sempre; os cenários autenticados exigem E2E_ADMIN_STORAGE (senão SKIP).

const ADMIN_STORAGE = process.env.E2E_ADMIN_STORAGE;

test.describe('US1/US2 — gating', () => {
  test('sem sessão, /admin redireciona para o login', async ({ page }) => {
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
  });
});

test.describe('US1 — identidade visível (autenticado)', () => {
  test.skip(!ADMIN_STORAGE, 'requer E2E_ADMIN_STORAGE (storageState com sessão admin)');
  test.use({ storageState: ADMIN_STORAGE });

  test('mostra a identidade do utilizador no painel', async ({ page }) => {
    await page.goto('/admin');
    const session = page.getByLabel('Sessão do utilizador');
    await expect(session).toBeVisible();
    // Função visível como badge (admin) e botão de terminar sessão presente.
    await expect(session.getByText('admin')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Terminar sessão' })).toBeVisible();
  });

  test('identidade mantém-se ao navegar entre secções', async ({ page }) => {
    await page.goto('/admin/lures');
    await expect(page.getByLabel('Sessão do utilizador')).toBeVisible();
    await page.goto('/admin/species');
    await expect(page.getByLabel('Sessão do utilizador')).toBeVisible();
  });
});

test.describe('US2 — terminar sessão (autenticado)', () => {
  test.skip(!ADMIN_STORAGE, 'requer E2E_ADMIN_STORAGE (storageState com sessão admin)');
  test.use({ storageState: ADMIN_STORAGE });

  test('logout redireciona para o login e bloqueia o acesso', async ({ page }) => {
    await page.goto('/admin');
    await page.getByRole('button', { name: 'Terminar sessão' }).click();
    await expect(page).toHaveURL(/\/login/);

    // Após o logout, aceder a /admin exige nova autenticação (SC-004).
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
  });
});
