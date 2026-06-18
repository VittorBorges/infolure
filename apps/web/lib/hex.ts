// Feature 005 — validação/normalização de códigos HTML (hex) partilhada pela UI do formulário.
// Aceita #RGB ou #RRGGBB. NÃO deduplica (duplicados na mesma cor são permitidos — textura diferente).

const HEX_RE = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

export function isValidHex(value: string): boolean {
  return HEX_RE.test(value.trim());
}

export function normalizeHex(value: string): string {
  return value.trim().toLowerCase();
}
