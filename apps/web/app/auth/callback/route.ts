import { NextResponse } from 'next/server';
import { getSupabaseServerClient } from '../../../lib/supabase/server';

export const dynamic = 'force-dynamic';

// Callback OAuth/email (US-04): troca o `code` pela sessão (PKCE) e encaminha.
// O Supabase valida o parâmetro `state` (anti-CSRF) durante esta troca.
export async function GET(request: Request) {
  const { searchParams, origin } = new URL(request.url);
  const code = searchParams.get('code');
  const next = searchParams.get('next') ?? '/escolher-username';

  if (code) {
    try {
      const supabase = await getSupabaseServerClient();
      const { error } = await supabase.auth.exchangeCodeForSession(code);
      if (!error) return NextResponse.redirect(`${origin}${next}`);
    } catch {
      // cai no erro abaixo
    }
  }

  return NextResponse.redirect(`${origin}/login?error=auth`);
}
