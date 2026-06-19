'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LayoutDashboard, Fish, Tag, Waves, Users, ScrollText, Settings, type LucideIcon } from 'lucide-react';

import { cn } from '@infolure/design-system';

const NAV: { href: string; label: string; icon: LucideIcon }[] = [
  { href: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/admin/lures', label: 'Iscas', icon: Fish },
  { href: '/admin/brands', label: 'Marcas', icon: Tag },
  { href: '/admin/species', label: 'Espécies', icon: Waves },
  { href: '/admin/users', label: 'Utilizadores', icon: Users },
  { href: '/admin/audit', label: 'Auditoria', icon: ScrollText },
  { href: '/admin/settings', label: 'Definições', icon: Settings },
];

export function AdminNav() {
  const pathname = usePathname();

  return (
    <nav className="grid gap-1.5 px-1">
      {NAV.map(({ href, label, icon: Icon }) => {
        const active = href === '/admin' ? pathname === '/admin' : pathname.startsWith(href);
        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? 'page' : undefined}
            className={cn(
              'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
              'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
              active
                ? 'bg-primary/10 text-primary'
                : 'text-muted-foreground hover:bg-secondary hover:text-foreground'
            )}
          >
            <Icon className={cn('h-[1.15rem] w-[1.15rem] shrink-0', active && 'text-primary')} />
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
