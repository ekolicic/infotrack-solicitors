import { Component, input, signal } from '@angular/core';
import { LocationResult, Solicitor, SortField, SortDir } from '../../models/solicitor.model';
import { StatSummaryComponent } from '../stat-summary/stat-summary';

@Component({
  selector: 'it-results-view',
  standalone: true,
  imports: [StatSummaryComponent],
  template: `
    <section aria-label="Search results">
      <it-stat-summary [results]="results()" />

      <div class="toolbar" role="toolbar" aria-label="Sort options">
        <label for="sortField" class="sort-label">Sort by</label>
        <select id="sortField" class="sort-select"
                [value]="sortField()"
                (change)="setSortField($event)"
                aria-label="Sort field">
          <option value="name">Name</option>
          <option value="starRating">Rating</option>
          <!-- <option value="reviewCount">Reviews</option> -->
        </select>
        <button type="button" class="sort-dir-btn"
                [attr.aria-label]="'Sort direction: ' + (sortDir() === 'asc' ? 'ascending' : 'descending')"
                (click)="toggleDir()">
          {{ sortDir() === 'asc' ? '↑ Asc' : '↓ Desc' }}
        </button>
      </div>

      @if (results().length === 1) {
        <div class="table-wrap">
          <table class="results-table" aria-label="{{ results()[0].location }} solicitors">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Address</th>
                <th scope="col">Telephone</th>
                <th scope="col">Rating</th>
              </tr>
            </thead>
            <tbody>
              @for (sol of sorted(results()[0].solicitors); track sol.name) {
                <tr>
                  <td class="col-name">
                    @if (sol.website) {
                      <a [href]="sol.website" target="_blank" rel="noopener noreferrer"
                         [attr.aria-label]="sol.name + ' website'">{{ sol.name }}</a>
                    } @else {
                      {{ sol.name }}
                    }
                  </td>
                  <td class="col-address">
                    @if (sol.address) {
                      <a [href]="mapsUrl(sol.address)" target="_blank" rel="noopener noreferrer"
                         [attr.aria-label]="'View ' + sol.name + ' on Google Maps'">
                        {{ sol.address }}
                      </a>
                    }
                  </td>
                  <td class="col-tel">
                    @if (sol.telephone) {
                      <a [href]="'tel:' + sol.telephone"
                         [attr.aria-label]="'Call ' + sol.name">
                        {{ formatPhone(sol.telephone) }}
                      </a>
                    }
                  </td>
                  <td class="col-rating">
                    @if (sol.starRating != null) {
                      <span class="stars" aria-hidden="true">
                        @for (star of starItems(sol.starRating); track $index) {
                          <span [class]="'star star-' + star"></span>
                        }
                      </span>
                      <span class="sr-only">{{ sol.starRating }} out of 5</span>
                      <span class="rating-val">{{ sol.starRating.toFixed(1) }}</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <div class="accordion">
          @for (loc of results(); track loc.location) {
            <div class="accordion-item">
              <button
                type="button"
                class="accordion-header"
                [attr.aria-expanded]="isExpanded(loc.location)"
                [attr.aria-controls]="'panel-' + loc.location"
                [id]="'header-' + loc.location"
                (click)="toggleAccordion(loc.location)">
                <div class="acc-left">
                  <span class="acc-location">{{ loc.location }}</span>
                  <span class="acc-count">
                    {{ loc.solicitors.length }} solicitor{{ loc.solicitors.length !== 1 ? 's' : '' }}
                  </span>
                </div>
                <span class="acc-chevron" [class.open]="isExpanded(loc.location)" aria-hidden="true">▾</span>
              </button>

              @if (isExpanded(loc.location)) {
                <div class="accordion-panel"
                     [id]="'panel-' + loc.location"
                     role="region"
                     [attr.aria-labelledby]="'header-' + loc.location">
                  @if (loc.errorMessage) {
                    <p class="location-error" role="alert">{{ loc.errorMessage }}</p>
                  } @else {
                  <div class="table-wrap">
                    <table class="results-table" [attr.aria-label]="loc.location + ' solicitors'">
                      <thead>
                        <tr>
                          <th scope="col">Name</th>
                          <th scope="col">Address</th>
                          <th scope="col">Telephone</th>
                          <th scope="col">Rating</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (sol of sorted(loc.solicitors); track sol.name) {
                          <tr>
                            <td class="col-name">
                              @if (sol.website) {
                                <a [href]="sol.website" target="_blank" rel="noopener noreferrer"
                                   [attr.aria-label]="sol.name + ' website'">{{ sol.name }}</a>
                              } @else {
                                {{ sol.name }}
                              }
                            </td>
                            <td class="col-address">
                              @if (sol.address) {
                                <a [href]="mapsUrl(sol.address)" target="_blank" rel="noopener noreferrer"
                                   [attr.aria-label]="'View ' + sol.name + ' on Google Maps'">
                                  {{ sol.address }}
                                </a>
                              }
                            </td>
                            <td class="col-tel">
                              @if (sol.telephone) {
                                <a [href]="'tel:' + sol.telephone"
                                   [attr.aria-label]="'Call ' + sol.name">
                                  {{ formatPhone(sol.telephone) }}
                                </a>
                              }
                            </td>
                            <td class="col-rating">
                              @if (sol.starRating != null) {
                                <span class="stars" aria-hidden="true">
                                  @for (star of starItems(sol.starRating); track $index) {
                                    <span [class]="'star star-' + star"></span>
                                  }
                                </span>
                                <span class="sr-only">{{ sol.starRating }} out of 5</span>
                                <span class="rating-val">{{ sol.starRating.toFixed(1) }}</span>
                              }
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </section>
  `,
  styleUrl: './results-view.css',
})
export class ResultsViewComponent {
  readonly results = input.required<LocationResult[]>();

  readonly sortField = signal<SortField>('starRating');
  readonly sortDir = signal<SortDir>('desc');
  private readonly expanded = signal<Set<string>>(new Set());

  isExpanded(location: string): boolean {
    return this.expanded().has(location);
  }

  toggleAccordion(location: string): void {
    this.expanded.update(set => {
      const next = new Set(set);
      if (next.has(location)) next.delete(location);
      else next.add(location);
      return next;
    });
  }

  setSortField(event: Event): void {
    this.sortField.set((event.target as HTMLSelectElement).value as SortField);
  }

  toggleDir(): void {
    this.sortDir.update(d => (d === 'asc' ? 'desc' : 'asc'));
  }

  mapsUrl(address: string): string {
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`;
  }

  formatPhone(tel: string): string {
    const digits = tel.replace(/\D/g, '');
    if (digits.length === 11 && digits.startsWith('0'))
      return `${digits.slice(0, 4)} ${digits.slice(4, 7)} ${digits.slice(7)}`;
    return tel;
  }

  starItems(rating: number): ('full' | 'half' | 'empty')[] {
    const full  = Math.floor(rating);
    const half  = rating % 1 >= 0.5 ? 1 : 0;
    const empty = 5 - full - half;
    return [
      ...Array(full).fill('full'),
      ...Array(half).fill('half'),
      ...Array(empty).fill('empty'),
    ];
  }

  sorted(solicitors: Solicitor[]): Solicitor[] {
    const field = this.sortField();
    const dir   = this.sortDir();
    return [...solicitors].sort((a, b) => {
      const av = a[field] ?? (dir === 'asc' ? Infinity : -Infinity);
      const bv = b[field] ?? (dir === 'asc' ? Infinity : -Infinity);
      if (typeof av === 'string' && typeof bv === 'string')
        return dir === 'asc' ? av.localeCompare(bv) : bv.localeCompare(av);
      return dir === 'asc' ? (av as number) - (bv as number) : (bv as number) - (av as number);
    });
  }
}
