import pt from '../messages/pt.json';
import en from '../messages/en.json';
import es from '../messages/es.json';

// i18n simples baseado em dicionários (PT-PT primário, EN fallback, ES secundário).
// Princípio V + NFR localização. Sem dependência externa para o MVP (YAGNI).

export const locales = ['pt', 'en', 'es'] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = 'pt';

const dictionaries: Record<Locale, Record<string, string>> = { pt, en, es };

export function getDictionary(locale: Locale): Record<string, string> {
  return dictionaries[locale] ?? dictionaries[defaultLocale];
}

/** Traduz uma chave; faz fallback para PT e, por fim, devolve a própria chave. */
export function t(locale: Locale, key: string): string {
  return getDictionary(locale)[key] ?? dictionaries[defaultLocale][key] ?? key;
}
