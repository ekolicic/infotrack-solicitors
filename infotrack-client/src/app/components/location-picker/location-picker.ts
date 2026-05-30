import {
  Component, input, output, signal, computed,
  HostListener, ElementRef, inject
} from '@angular/core';

@Component({
  selector: 'it-location-picker',
  standalone: true,
  template: `
    <div class="picker-wrapper" [class.open]="isOpen()">
      <button
        type="button"
        class="picker-toggle"
        [disabled]="loading()"
        [attr.aria-expanded]="isOpen()"
        [attr.aria-label]="'Select locations. Currently: ' + label()"
        (click)="toggleOpen($event)">
        <span>{{ loading() ? 'Loading locations…' : label() }}</span>
        <span class="picker-chevron" [class.rotated]="isOpen()" aria-hidden="true">▾</span>
      </button>

      @if (isOpen()) {
        <div class="picker-dropdown" role="listbox" aria-multiselectable="true"
             aria-label="Location options">
          <div class="picker-actions">
            <button type="button" class="link-btn" (click)="selectAll()"
                    aria-label="Select all locations">All</button>
            <span class="separator" aria-hidden="true">·</span>
            <button type="button" class="link-btn" (click)="clearAll()"
                    aria-label="Clear all selections">None</button>
          </div>

          <ul class="picker-list" role="group">
            @for (loc of locations(); track loc) {
              <li role="option" [attr.aria-selected]="isSelected(loc)">
                <label class="picker-item" [class.selected]="isSelected(loc)">
                  <input
                    type="checkbox"
                    class="picker-checkbox"
                    [checked]="isSelected(loc)"
                    [attr.aria-label]="loc"
                    (change)="toggle(loc)" />
                  <span class="picker-label">{{ loc }}</span>
                  @if (isSelected(loc)) {
                    <span class="picker-tick" aria-hidden="true">✓</span>
                  }
                </label>
              </li>
            }
          </ul>

          <div class="picker-add" (click)="$event.stopPropagation()">
            <input
              #customInput
              type="text"
              class="picker-add-input"
              placeholder="Add a location…"
              maxlength="60"
              aria-label="Add a custom location"
              (keydown.enter)="addCustom(customInput)" />
            <button
              type="button"
              class="picker-add-btn"
              aria-label="Add custom location"
              (click)="addCustom(customInput)">
              Add
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styleUrl: './location-picker.css',
})
export class LocationPickerComponent {
  readonly locations = input.required<string[]>();
  readonly selected  = input<string[]>([]);
  readonly loading   = input<boolean>(false);

  readonly selectionChange = output<string[]>();
  readonly locationAdded   = output<string>();

  private readonly el = inject(ElementRef);
  readonly isOpen = signal(false);

  readonly label = computed(() => {
    const sel = this.selected();
    if (sel.length === 0) return 'Select locations…';
    if (sel.length === 1) return sel[0];
    return `${sel.length} locations selected`;
  });

  isSelected(loc: string): boolean {
    return this.selected().includes(loc);
  }

  toggle(loc: string): void {
    const current = [...this.selected()];
    const idx = current.indexOf(loc);
    if (idx >= 0) current.splice(idx, 1);
    else current.push(loc);
    this.selectionChange.emit(current);
  }

  selectAll(): void  { this.selectionChange.emit([...this.locations()]); }
  clearAll(): void   { this.selectionChange.emit([]); }

  addCustom(input: HTMLInputElement): void {
    const value = input.value.trim();
    if (!value) return;
    this.locationAdded.emit(value);
    input.value = '';
  }

  toggleOpen(event: Event): void {
    event.stopPropagation();
    this.isOpen.update(v => !v);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isOpen()) return;
    if (!this.el.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }
}
