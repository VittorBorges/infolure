import { redirect } from 'next/navigation';
import { getSupabaseServerClient } from '../../../lib/supabase/server';
import { SettingsForm } from '../../../components/settings/SettingsForm';
import { AuthProviders } from '../../../components/settings/AuthProviders';

export const dynamic = 'force-dynamic';
export const metadata = { title: 'Definições — Infolure' };

// US-07 (T076) — página de settings: nome/avatar (SettingsForm) + linking de provedores
// (AuthProviders, T052) + apagar conta (RGPD, T077).
export default async function SettingsPage() {
  const supabase = await getSupabaseServerClient();
  const { data } = await supabase.auth.getSession();
  if (!data.session) redirect('/login?returnUrl=/conta/settings');

  return (
    <div style={{ padding: '1.5rem', display: 'grid', gap: '2rem', maxWidth: 480 }}>
      <h1>Definições</h1>
      <SettingsForm />
      <AuthProviders />
    </div>
  );
}
