import './admin.css';
import { redirect } from 'next/navigation';
import { getSupabaseServerClient } from '../../lib/supabase/server';
import { AdminNav } from '../../components/admin/AdminNav';

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
      <main className="flex-1 px-8 py-8">{children}</main>
    </div>
  );
}
