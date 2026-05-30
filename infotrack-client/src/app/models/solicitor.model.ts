export interface Solicitor {
  name: string;
  address: string | null;
  telephone: string | null;
  website: string | null;
  location: string;
  starRating: number | null;
  reviewCount: number | null;
  isNewlyDiscovered: boolean;
  scrapedAt: string;
}

export interface LocationResult {
  location: string;
  solicitors: Solicitor[];
  fromCache: boolean;
  cachedAt: string;
  newlyDiscoveredCount: number;
  errorMessage: string | null;
}

export type SortField = 'name' | 'starRating' | 'reviewCount';
export type SortDir = 'asc' | 'desc';
