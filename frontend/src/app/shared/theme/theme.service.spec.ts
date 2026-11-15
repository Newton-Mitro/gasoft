import { TestBed } from '@angular/core/testing';
import { ThemeColor, ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('gasoft.themeMode');
    localStorage.removeItem('gasoft.themeColor');
    delete document.documentElement.dataset['theme'];
    delete document.documentElement.dataset['accent'];
    TestBed.configureTestingModule({ providers: [ThemeService] });
  });

  afterEach(() => TestBed.resetTestingModule());

  it('persists the selected display mode and accent color on the document', () => {
    const theme = TestBed.inject(ThemeService);

    theme.setMode('dark');
    theme.setColor('ocean');

    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(document.documentElement.dataset['accent']).toBe('ocean');
    expect(localStorage.getItem('gasoft.themeMode')).toBe('dark');
    expect(localStorage.getItem('gasoft.themeColor')).toBe('ocean');
  });

  it('uses the system appearance by default and rejects unknown accents', () => {
    const theme = TestBed.inject(ThemeService);

    expect(theme.mode()).toBe('system');
    expect(() => theme.setColor('unknown' as ThemeColor)).toThrow('Unsupported theme color');
  });
});
