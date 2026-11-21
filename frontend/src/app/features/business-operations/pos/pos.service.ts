import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api.service';
import { PosSalePayload, PosSaleResult } from './pos.model';

@Injectable({ providedIn: 'root' })
export class PosService {
  constructor(private readonly api: ApiService) {}

  checkout(payload: PosSalePayload): Observable<PosSaleResult> {
    return this.api.post<PosSaleResult>('pos/checkout', payload, true);
  }
}
