import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';
export type ThemeColor = 'forest' | 'ocean' | 'violet' | 'amber' | 'rose';

export interface ThemeColorOption {
  id: ThemeColor;
  label: string;
  value: string;
}

const MODE_STORAGE_KEY = 'gasoft.themeMode';
const COLOR_STORAGE_KEY = 'gasoft.themeColor';

export const THEME_COLORS: ThemeColorOption[] = [
  { id: 'forest', label: 'Forest', value: '#1a5944' },
  { id: 'ocean', label: 'Ocean', value: '#2563a6' },
  { id: 'violet', label: 'Violet', value: '#7554a5' },
  { id: 'amber', label: 'Amber', value: '#9a641e' },
  { id: 'rose', label: 'Rose', value: '#a64363' },
];

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(readMode());
  readonly color = signal<ThemeColor>(readColor());
  readonly colors = THEME_COLORS;
  private readonly document = inject(DOCUMENT);
  private readonly colorScheme = typeof window.matchMedia === 'function'
    ? window.matchMedia('(prefers-color-scheme: dark)')
    : null;

  constructor() {
    this.apply();
    this.colorScheme?.addEventListener('change', () => {
      if (this.mode() === 'system') this.apply();
    });
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    localStorage.setItem(MODE_STORAGE_KEY, mode);
    this.apply();
  }

  setColor(color: ThemeColor): void {
    if (!THEME_COLORS.some(option => option.id === color)) {
      throw new Error(`Unsupported theme color: ${color}`);
    }
    this.color.set(color);
    localStorage.setItem(COLOR_STORAGE_KEY, color);
    this.apply();
  }

  private apply(): void {
    const resolvedMode = this.mode() === 'system'
      ? this.colorScheme?.matches ? 'dark' : 'light'
      : this.mode();
    this.document.documentElement.dataset['theme'] = resolvedMode;
    this.document.documentElement.dataset['accent'] = this.color();
  }
}

function readMode(): ThemeMode {
  const stored = localStorage.getItem(MODE_STORAGE_KEY);
  return stored === 'light' || stored === 'dark' || stored === 'system' ? stored : 'system';
}

function readColor(): ThemeColor {
  const stored = localStorage.getItem(COLOR_STORAGE_KEY);
  return THEME_COLORS.some(option => option.id === stored) ? stored as ThemeColor : 'forest';
}
