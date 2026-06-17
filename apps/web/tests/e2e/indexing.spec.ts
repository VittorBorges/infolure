import { test, expect, type APIRequestContext } from '@playwright/test';

// T038 [US-03] — E2E do controlo de indexação (FR-014 / FR-018 / SC-005).
// Cenários cobertos:
//   1) Interruptor global OFF → robots.txt proíbe tudo, sitemap.xml vazio e o detalhe sai noindex;
//      ON → robots permite + lista o sitemap, e o detalhe de uma isca elegível indexa.
//   2) Perfis /u/:username são sempre noindex, independentemente do interruptor (FR-018).
//
// Pré-requisitos: API em :5191 (com seed/índice) e Next dev em :3000 a correr.
//   npm i -D @playwright/test && npx playwright install
//
// O toggle global exige um JWT admin REAL do Supabase (claim role=admin) — não há como mexer no
// interruptor sem auth. Fornecer via variáveis de ambiente:
//   E2E_ADMIN_TOKEN=<jwt admin>            (obrigatória p/ os cenários de toggle)
//   E2E_API_BASE_URL=http://localhost:5191 (opcional; default abaixo)
//   E2E_PROFILE_USERNAME=<username público> (obrigatória p/ a invariante de perfil)
// Sem as credenciais, os respetivos testes são SALTADOS (skip) em vez de falharem.

const API_BASE =
  process.env.E2E_API_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';
const ADMIN_TOKEN = process.env.E2E_ADMIN_TOKEN;
const PROFILE_USERNAME = process.env.E2E_PROFILE_USERNAME;

// SC-005: o toggle reflete-se em < 60s. A cache de /v1/seo é invalidada na escrita, pelo que na
// prática é imediato; damos folga para evitar flakiness sem mascarar regressões reais.
const PROPAGATION = 60_000;

async function setIndexing(request: APIRequestContext, enabled: boolean): Promise<void> {
  const res = await request.put(`${API_BASE}/v1/admin/settings/indexing`, {
    headers: { Authorization: `Bearer ${ADMIN_TOKEN}` },
    data: { enabled },
  });
  expect(res.status(), 'PUT /v1/admin/settings/indexing deve aceitar o token admin (204)').toBe(204);
}

// Lê a saída textual de uma rota do frontend (robots.txt / sitemap.xml) sem cache.
async function fetchText(request: APIRequestContext, path: string): Promise<string> {
  const res = await request.get(path, { headers: { 'cache-control': 'no-cache' } });
  expect(res.ok(), `${path} deve responder 200`).toBeTruthy();
  return res.text();
}

test.describe('US-03 — interruptor global de indexação', () => {
  test.skip(
    !ADMIN_TOKEN,
    'requer E2E_ADMIN_TOKEN (JWT admin do Supabase) para alternar o interruptor global',
  );

  // Espelha o finally do teste de integração: restaura o estado ligado (default operacional).
  test.afterAll(async ({ request }) => {
    if (ADMIN_TOKEN) await setIndexing(request, true).catch(() => undefined);
  });

  test('OFF proíbe indexação em robots, sitemap e detalhe; ON volta a permitir', async ({
    request,
    page,
  }) => {
    // --- Pré-condição: ligado → captura um slug elegível e confirma que ON expõe a indexação. ---
    await setIndexing(request, true);

    let eligibleSlug = '';
    await expect
      .poll(
        async () => {
          const xml = await fetchText(request, '/sitemap.xml');
          eligibleSlug = xml.match(/\/iscas\/([^<]+)</)?.[1] ?? '';
          return eligibleSlug;
        },
        { message: 'sitemap deve listar iscas elegíveis quando ligado', timeout: PROPAGATION },
      )
      .not.toBe('');

    const robotsOn = (await fetchText(request, '/robots.txt')).toLowerCase();
    expect(robotsOn).toContain('allow: /');
    expect(robotsOn).toContain('sitemap:');

    const detailOn = await page.goto(`/iscas/${eligibleSlug}`);
    expect(detailOn?.status()).toBe(200);
    expect((await detailOn!.text()).toLowerCase()).not.toContain('noindex');

    // --- Desligar → tudo passa a proibir indexação. ---
    await setIndexing(request, false);

    // robots.txt: proíbe a raiz, sem linha Allow nem Sitemap.
    await expect
      .poll(async () => fetchText(request, '/robots.txt'), {
        message: 'robots deve proibir a raiz quando desligado',
        timeout: PROPAGATION,
      })
      .toMatch(/disallow:\s*\/\s*$/im);
    const robotsOff = (await fetchText(request, '/robots.txt')).toLowerCase();
    expect(robotsOff).not.toContain('allow: /');
    expect(robotsOff).not.toContain('sitemap:');

    // sitemap.xml: vazio (sem entradas <url>).
    await expect
      .poll(async () => (await fetchText(request, '/sitemap.xml')).includes('<url>'), {
        message: 'sitemap deve ficar vazio quando desligado',
        timeout: PROPAGATION,
      })
      .toBe(false);

    // Detalhe: o HTML servido (SSR) traz a meta noindex.
    const detailOff = await page.goto(`/iscas/${eligibleSlug}`);
    expect(detailOff?.status()).toBe(200);
    expect((await detailOff!.text()).toLowerCase()).toContain('noindex');
  });
});

test.describe('US-03 — perfis são sempre noindex (FR-018)', () => {
  test.skip(
    !PROFILE_USERNAME,
    'requer E2E_PROFILE_USERNAME (perfil público existente) para validar a meta noindex',
  );

  test('o perfil público nunca é indexável, independentemente do interruptor', async ({ page }) => {
    const res = await page.goto(`/u/${PROFILE_USERNAME}`);
    expect(res?.status(), 'o perfil indicado deve existir (200)').toBe(200);
    expect((await res!.text()).toLowerCase()).toContain('noindex');
  });
});
