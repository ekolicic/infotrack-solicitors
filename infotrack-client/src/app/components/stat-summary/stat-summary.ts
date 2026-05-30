import { Component, input, computed } from '@angular/core';
import { LocationResult } from '../../models/solicitor.model';

@Component({
  selector: 'it-stat-summary',
  standalone: true,
  template: `
    <div class="stats-bar" role="region" aria-label="Search results summary">
      <div class="stat-item">
        <span class="stat-number">{{ totalSolicitors() }}</span>
        <span class="stat-label">Solicitors Found</span>
      </div>
      <div class="stat-item">
        <span class="stat-number">{{ results().length }}</span>
        <span class="stat-label">Locations</span>
      </div>
      @if (avgRating() !== null) {
        <div class="stat-item">
          <span class="stat-number">{{ avgRating()!.toFixed(1) }} ★</span>
          <span class="stat-label">Avg. Rating</span>
        </div>
      }
      @if (topLocation()) {
        <div class="stat-item">
          <span class="stat-number">{{ topLocation() }}</span>
          <span class="stat-label">Most Solicitors</span>
        </div>
      }
    </div>
  `,
  styleUrl: './stat-summary.css',
})
export class StatSummaryComponent {
  readonly results = input.required<LocationResult[]>();

  readonly totalSolicitors = computed(() =>
    this.results().reduce((acc, r) => acc + r.solicitors.length, 0)
  );

  readonly avgRating = computed(() => {
    const rated = this.results()
      .flatMap(r => r.solicitors)
      .filter(s => s.starRating != null)
      .map(s => s.starRating!);
    return rated.length > 0
      ? rated.reduce((a, b) => a + b, 0) / rated.length
      : null;
  });

  readonly topLocation = computed(() => {
    if (this.results().length < 2) return null;
    return [...this.results()]
      .sort((a, b) => b.solicitors.length - a.solicitors.length)[0]?.location ?? null;
  });
}
