import { getSupabaseBrowserClient } from './supabase/client';

// Feature 007 (US2) — terminar sessão no cliente. Invalida a sessão Supabase no browser;
// a redireção e o tratamento de erro ficam a cargo de quem chama (ver AdminUserMenu).
export async function logout(): Promise<{ ok: boolean }> {
  try {
    const supabase = getSupabaseBrowserClient();
    const { error } = await supabase.auth.signOut();
    return { ok: !error };
  } catch {
    return { ok: false };
  }
}
