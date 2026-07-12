import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    const storage = new Map<string, string>();
    Object.defineProperty(globalThis, 'localStorage', {
      configurable: true,
      value: {
        getItem: (key: string) => storage.get(key) ?? null,
        setItem: (key: string, value: string) => void storage.set(key, value),
        removeItem: (key: string) => void storage.delete(key),
        clear: () => storage.clear(),
      } as Storage,
    });

    await TestBed.configureTestingModule({
      imports: [App, TranslocoTestingModule.forRoot({ langs: { en: {}, he: {} } })],
      providers: [provideZonelessChangeDetection(), provideRouter([]), MessageService],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the router outlet and toast host', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
    expect(compiled.querySelector('p-toast')).toBeTruthy();
  });
});
