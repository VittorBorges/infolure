'use client';

// @infolure/design-system — barrel público.
// API idêntica à da feature 003 (sem alteração de uso); ver contracts/package-api.md.
// 'use client' no topo: os componentes são leaf-UI (vários usam hooks/Radix); marcar o pacote
// como client é o adequado para um UI kit e preserva as fronteiras RSC nos consumidores.

export { cn } from './lib/utils';

export { Button, buttonVariants } from './components/button';
export type { ButtonProps } from './components/button';
export {
  Card,
  CardHeader,
  CardFooter,
  CardTitle,
  CardDescription,
  CardContent,
} from './components/card';
export {
  Table,
  TableHeader,
  TableBody,
  TableHead,
  TableRow,
  TableCell,
} from './components/table';
export { Input } from './components/input';
export { Label } from './components/label';
export { Badge, badgeVariants } from './components/badge';
export type { BadgeProps } from './components/badge';
export {
  Dialog,
  DialogPortal,
  DialogOverlay,
  DialogClose,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
} from './components/dialog';
export {
  Select,
  SelectGroup,
  SelectValue,
  SelectTrigger,
  SelectContent,
  SelectLabel,
  SelectItem,
  SelectScrollUpButton,
  SelectScrollDownButton,
} from './components/select';
