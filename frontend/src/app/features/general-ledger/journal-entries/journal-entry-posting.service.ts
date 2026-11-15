import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { JournalEntryPostResult } from './journal-entry.model';

@Injectable({ providedIn: 'root' })
export class JournalEntryPostingService {
  constructor(private readonly api: ApiService) {}

  postJournalEntry(id: number): Observable<JournalEntryPostResult> {
    return this.api.post<JournalEntryPostResult>(`journal-entries/${id}/post`, {}, true);
  }
}
