import Link from 'next/link';
import { redirect } from 'next/navigation';
import { getSupabaseServerClient } from '../../lib/supabase/server';

export const dynamic = 'force-dynamic';
export const metadata = { title: 'Administração — Infolure' };

const NAV: { href: string; label: string }[] = [
  { href: '/admin', label: 'Dashboard' },
  { href: '/admin/lures', label: 'Iscas' },
  { href: '/admin/brands', label: 'Marcas' },
  { href: '/admin/species', label: 'Espécies' },
  { href: '/admin/users', label: 'Utilizadores' },
  { href: '/admin/audit', label: 'Auditoria' },
];

// T032 — gate de sessão. A autorização admin (role) é verificada no backend por requisição;
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
    <div style={{ display: 'flex', minHeight: '100vh' }}>
      <aside style={{ width: 200, borderRight: '1px solid #eee', padding: '1.5rem 1rem' }}>
        <strong style={{ display: 'block', marginBottom: '1rem' }}>Admin</strong>
        <nav style={{ display: 'grid', gap: '0.5rem' }}>
          {NAV.map((n) => (
            <Link key={n.href} href={n.href} style={{ textDecoration: 'none' }}>
              {n.label}
            </Link>
          ))}
        </nav>
      </aside>
      <main style={{ flex: 1, padding: '1.5rem' }}>{children}</main>
    </div>
  );
}
