import { apiGet } from './api';

// Tipos de domínio do read-path (derivados do contrato; mantidos enxutos para o frontend).

export interface LureCard {
  id: string;
  slug: string;
  name: string;
  brand?: string;
  lure_type: string;
  water_type?: string;
  weight_g?: number;
  primary_image_url?: string;
  primary_color_hex?: string;
  target_species: string[];
  price_avg_eur?: number;
  favorites_count: number;
  is_favorited?: boolean | null;
}

export interface FacetValue {
  value: string;
  count: number;
}

export interface CatalogFacets {
  lure_types: FacetValue[];
  brands: FacetValue[];
  water_types: FacetValue[];
  species: FacetValue[];
}

export interface LureListResponse {
  data: LureCard[];
  meta: { total: number; page: number; per_page: number; facets: CatalogFacets };
}

export interface SuggestResponse {
  suggestions: { slug: string; name: string; brand?: string; type: string }[];
}

export interface LureDetail extends LureCard {
  description?: string;
  length_mm?: number;
  depth_min_m?: number;
  depth_max_m?: number;
  hook_size?: string;
  hook_type?: string;
  hook_count?: number;
  material?: string;
  colors: { id: string; name: string; hex_primary?: string; hex_secondary?: string; pattern?: string }[];
  images: { url: string; color_id?: string; is_primary: boolean }[];
  target_species_detail: { slug: string; common_name: string; confidence?: string }[];
  pricing?: {
    avg_eur?: number;
    min_eur?: number;
    max_eur?: number;
    updated_at?: string;
    retailers: { retailer: string; url?: string; price_eur: number; in_stock: boolean }[];
  } | null;
  avg_rating?: number | null;
  reviews_count: number;
}

export function fetchLures(query: string): Promise<LureListResponse> {
  return apiGet<LureListResponse>(`/v1/lures${query ? `?${query}` : ''}`, { cache: 'no-store' });
}

export function fetchLureDetail(slug: string, locale = 'pt'): Promise<LureDetail> {
  return apiGet<LureDetail>(`/v1/lures/${encodeURIComponent(slug)}?locale=${locale}`, {
    cache: 'no-store',
  });
}
