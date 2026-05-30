import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the page heading', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Find Conveyancing Solicitors');
  });

  it('should start with no locations selected', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance.selectedLocations()).toEqual([]);
  });

  it('canSearch should be false when no locations selected', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance.canSearch()).toBeFalsy();
  });

  it('canSearch should be true when locations are selected', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.selectedLocations.set(['London']);
    expect(fixture.componentInstance.canSearch()).toBeTruthy();
  });

  it('onLocationsChange updates selectedLocations signal', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.onLocationsChange(['Leeds', 'Bristol']);
    expect(fixture.componentInstance.selectedLocations()).toEqual(['Leeds', 'Bristol']);
  });
});
