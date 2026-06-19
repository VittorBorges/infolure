'use client';

import { useState } from 'react';
import { Badge, Button } from '@infolure/design-system';
import { logout } from '../../lib/auth-actions';

// Feature 007 — identidade da sessão atual no painel admin (US1) + botão de terminar sessão (US2).
// A identidade chega por props (obtida server-side via GET /v1/me).
export interface AdminUserMenuProps {
  displayName?: string | null;
  username?: string | null;
  email?: string | null;
  role?: string | null;
}

// FR-002 — nome legível: display_name → username → email. Nunca o UUID (que nem chega ao cliente).
function resolveName(p: AdminUserMenuProps): string {
  return p.displayName?.trim() || p.username?.trim() || p.email?.trim() || '—';
}

export function AdminUserMenu(props: AdminUserMenuProps) {
  const name = resolveName(props);
  const role = props.role?.trim() || '—'; // FR-003 / edge case: valor neutro se ausente
  const email = props.email?.trim() || null;

  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // FR-006..FR-009 — terminar sessão: progresso, sem duplo-clique, redireção e erro compreensível.
  async function onLogout() {
    setError(null);
    setPending(true);
    const r = await logout();
    if (r.ok) {
      window.location.href = '/login'; // mantém pending=true; a página é substituída
    } else {
      setPending(false);
      setError('Não foi possível terminar a sessão. Tente novamente.');
    }
  }

  return (
    <div className="flex items-center gap-3" aria-label="Sessão do utilizador">
      <div className="flex flex-col items-end leading-tight">
        <span className="text-sm font-medium">{name}</span>
        {email && email !== name && (
          <span className="text-xs text-muted-foreground">{email}</span>
        )}
      </div>
      <Badge variant={role === 'admin' ? 'default' : 'secondary'}>{role}</Badge>
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={onLogout}
        disabled={pending}
        aria-label="Terminar sessão"
      >
        {pending ? 'A terminar…' : 'Terminar sessão'}
      </Button>
      {error && (
        <span role="alert" className="text-xs text-destructive">{error}</span>
      )}
    </div>
  );
}
