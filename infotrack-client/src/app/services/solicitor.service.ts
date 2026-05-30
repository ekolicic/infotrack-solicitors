import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LocationResult } from '../models/solicitor.model';

@Injectable({ providedIn: 'root' })
export class SolicitorService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = '/api/solicitors';

  getLocations(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBase}/locations`);
  }

  getSolicitors(locations: string[], refresh = false): Observable<LocationResult[]> {
    let params = new HttpParams().set('refresh', String(refresh));
    for (const loc of locations) {
      params = params.append('locations', loc);
    }
    return this.http.get<LocationResult[]>(this.apiBase, { params });
  }

  invalidateCache(location: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/cache/${encodeURIComponent(location)}`);
  }

  invalidateAllCaches(): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/cache`);
  }
}
