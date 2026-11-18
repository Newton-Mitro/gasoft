import { Routes } from '@angular/router';

export const JOURNAL_ENTRIES_ROUTES: Routes = [
  { path: 'general-ledger/journal-entries/create', loadComponent: () => import('./journal-entries-page/journal-entries.page').then(module => module.JournalEntriesPageComponent), data: { page: 'journal-entries', view: 'create' } },
  { path: 'general-ledger/journal-entries/:recordId/edit', loadComponent: () => import('./journal-entries-page/journal-entries.page').then(module => module.JournalEntriesPageComponent), data: { page: 'journal-entries', view: 'edit' } },
  { path: 'general-ledger/journal-entries', loadComponent: () => import('./journal-entries-page/journal-entries.page').then(module => module.JournalEntriesPageComponent), data: { page: 'journal-entries' } },
];
