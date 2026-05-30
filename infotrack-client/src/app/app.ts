import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { SolicitorService } from './services/solicitor.service';
import { LocationResult } from './models/solicitor.model';
import { LocationPickerComponent } from './components/location-picker/location-picker';
import { ResultsViewComponent } from './components/results-view/results-view';

type AppState = 'idle' | 'loading' | 'success' | 'error';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LocationPickerComponent, ResultsViewComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly service = inject(SolicitorService);

  readonly availableLocations = signal<string[]>([]);
  readonly selectedLocations = signal<string[]>([]);
  readonly results = signal<LocationResult[]>([]);
  readonly state = signal<AppState>('idle');
  readonly errorMessage = signal<string>('');
  readonly lastSearchedAt = signal<Date | null>(null);
  readonly locationsLoading = signal(true);

  readonly hasResults = computed(() => this.results().length > 0);
  readonly isLoading = computed(() => this.state() === 'loading');
  readonly canSearch = computed(() => this.selectedLocations().length > 0 && !this.isLoading());
  readonly lastSearchedLabel = computed(() => {
    const d = this.lastSearchedAt();
    if (!d) return '';
    return d.toLocaleString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  });

  ngOnInit(): void {
    this.service.getLocations().subscribe({
      next: locs => {
        this.availableLocations.set([...locs].sort());
        this.locationsLoading.set(false);
      },
      error: () => this.locationsLoading.set(false),
    });
  }

  onLocationsChange(locations: string[]): void {
    this.selectedLocations.set(locations);
  }

  onLocationAdded(location: string): void {
    const trimmed = location.trim();
    if (!trimmed) return;
    this.availableLocations.update(locs => {
      if (locs.some(l => l.toLowerCase() === trimmed.toLowerCase())) return locs;
      return [...locs, trimmed].sort();
    });
    this.selectedLocations.update(sel =>
      sel.some(l => l.toLowerCase() === trimmed.toLowerCase()) ? sel : [...sel, trimmed]
    );
  }

  onSearch(refresh = false): void {
    if (!this.canSearch()) return;
    this.state.set('loading');
    this.errorMessage.set('');

    this.service.getSolicitors(this.selectedLocations(), refresh).subscribe({
      next: results => {
        this.results.set(results);
        this.state.set('success');
        this.lastSearchedAt.set(new Date());
      },
      error: err => {
        this.state.set('error');
        this.errorMessage.set(
          err?.status === 0
            ? 'Could not reach the API. Is the .NET server running?'
            : `Error ${err?.status ?? ''}: ${err?.message ?? 'Unknown error'}`
        );
      },
    });
  }

  onRefresh(): void {
    this.onSearch(true);
  }
}
