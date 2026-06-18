import { test, expect } from '@playwright/test';

// Feature 005 — E2E do formulário de registo/edição de iscas (US1/US2/US3).
// Segue o padrão de admin-design.spec.ts: o gating de sessão corre sempre; os fluxos autenticados
// exigem E2E_ADMIN_STORAGE (storageState com sessão admin) e são SALTADOS sem ele.
// Pré-requisitos do fluxo autenticado: Next dev em :3000 e API em :5191.

const ADMIN_STORAGE = process.env.E2E_ADMIN_STORAGE;

test.describe('US1 — gating de sessão do formulário', () => {
  test('sem sessão, /admin/lures/new redireciona para o login', async ({ page }) => {
    await page.goto('/admin/lures/new');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
  });
});

test.describe('Formulário de iscas (autenticado)', () => {
  test.skip(!ADMIN_STORAGE, 'requer E2E_ADMIN_STORAGE (storageState com sessão admin)');
  test.use({ storageState: ADMIN_STORAGE });

  // US1 — registar isca com tamanho e cor.
  test('regista uma nova isca com tamanho e cor', async ({ page }) => {
    const slug = `e2e-isca-${Date.now()}`;
    await page.goto('/admin/lures/new');

    await page.getByLabel('Slug *').fill(slug);
    await page.getByLabel('Nome *').fill('Minnow E2E');
    await page.getByLabel('Tipo *').fill('jerkbait');

    // Primeiro tamanho (já presente).
    await page.getByLabel('Rótulo 1').fill('90SP');
    await page.getByLabel('Peso 1').fill('9.5');

    await page.getByRole('button', { name: '+ Adicionar cor' }).click();
    await page.getByLabel('Nome da cor 1').fill('Tiger');
    await page.getByRole('button', { name: '+ Adicionar cor/hex' }).click();
    await page.getByLabel('Código hex 1').fill('#00ff00');

    await page.getByRole('button', { name: 'Criar isca' }).click();

    // Redireciona para a edição da isca criada.
    await expect(page).toHaveURL(/\/admin\/lures\/[0-9a-f-]{36}$/);
  });

  // US3 — hex inválido bloqueia com mensagem.
  test('hex inválido mostra erro e não grava', async ({ page }) => {
    await page.goto('/admin/lures/new');
    await page.getByLabel('Slug *').fill(`e2e-badhex-${Date.now()}`);
    await page.getByLabel('Nome *').fill('Bad Hex');
    await page.getByLabel('Tipo *').fill('jig');
    await page.getByLabel('Rótulo 1').fill('STD');
    await page.getByLabel('Peso 1').fill('10');

    await page.getByRole('button', { name: '+ Adicionar cor' }).click();
    await page.getByLabel('Nome da cor 1').fill('X');
    await page.getByRole('button', { name: '+ Adicionar cor/hex' }).click();
    await page.getByLabel('Código hex 1').fill('#12xz');

    await expect(page.getByText('Hex inválido (use #RGB ou #RRGGBB).')).toBeVisible();

    await page.getByRole('button', { name: 'Criar isca' }).click();
    // Não redireciona (permanece em /new) e mostra erro de validação.
    await expect(page).toHaveURL(/\/admin\/lures\/new$/);
  });
});
