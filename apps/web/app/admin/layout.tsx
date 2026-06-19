import './admin.css';
import { redirect } from 'next/navigation';
import { getSupabaseServerClient } from '../../lib/supabase/server';
import { getMe } from '../../lib/admin';
import { AdminNav } from '../../components/admin/AdminNav';
import { AdminUserMenu } from '../../components/admin/AdminUserMenu';

export const dynamic = 'force-dynamic';
export const metadata = { title: 'Administração — Infolure' };

// T032/T008 — gate de sessão. A autorização admin (role) é verificada no backend por requisição;
// as páginas mostram "sem acesso" em 403.
export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  let hasSession = false;
  try {
    const supabase = await getSupabaseServerClient();
    const { data } = await supabase.auth.getSession();
    hasSession = !!data.session;
  } catch {
    hasSession = false; // Supabase não configurado
  }
  if (!hasSession) redirect('/login?returnUrl=/admin');

  // Feature 007 (US1) — identidade da sessão atual; se não resolver, o painel continua funcional.
  const meRes = await getMe();
  const me = meRes.ok ? meRes.data : null;

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <aside className="flex w-64 shrink-0 flex-col gap-8 border-r border-border bg-card px-4 py-6">
        <div className="flex items-center gap-2.5 px-2">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-sm font-bold text-primary-foreground">
            i
          </span>
          <span className="text-base font-semibold tracking-tight">Infolure Admin</span>
        </div>
        <AdminNav />
      </aside>
      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-end gap-4 border-b border-border bg-card px-8 py-3">
          <AdminUserMenu
            displayName={me?.display_name}
            username={me?.username}
            email={me?.email}
            role={me?.role}
          />
        </header>
        <main className="flex-1 px-8 py-8">{children}</main>
      </div>
    </div>
  );
}
