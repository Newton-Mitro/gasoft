import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PosSalePayload, PosSaleResult, PosSettings } from './pos.model';

@Injectable({ providedIn: 'root' })
export class PosService {
  constructor(private readonly api: ApiService) {}

  getSettings(): Observable<PosSettings> {
    return this.api.get<PosSettings>('pos/settings', true);
  }

  saveSettings(settings: PosSettings): Observable<PosSettings> {
    return this.api.put<PosSettings>('pos/settings', settings, true);
  }

  checkout(payload: PosSalePayload): Observable<PosSaleResult> {
    return this.api.post<PosSaleResult>('pos/checkout', payload, true);
  }
}
