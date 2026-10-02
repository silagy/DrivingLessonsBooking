import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { App } from './app';
import { LanguageService } from './core/language.service';

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

  it('puts toasts in the top-left corner in Hebrew', async () => {
    //given
    const fixture = TestBed.createComponent(App);

    //when
    await fixture.whenStable();

    //then
    const toast = fixture.debugElement.query(By.directive(Toast)).componentInstance as Toast;
    expect(toast.position).toBe('top-left');
  });

  it('puts toasts in the top-right corner in English', async () => {
    //given
    localStorage.setItem('app_lang', 'en');
    const fixture = TestBed.createComponent(App);

    //when
    await fixture.whenStable();

    //then
    const toast = fixture.debugElement.query(By.directive(Toast)).componentInstance as Toast;
    expect(toast.position).toBe('top-right');
  });

  it('moves toasts to the top-left corner when switching from English to Hebrew', async () => {
    //given
    localStorage.setItem('app_lang', 'en');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    //when
    TestBed.inject(LanguageService).use('he');
    await fixture.whenStable();

    //then
    const container = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.p-toast')!;
    expect([container.style.left, container.style.right]).toEqual(['20px', '']);
  });
});
