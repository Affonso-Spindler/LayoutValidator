import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';
import { RegraDisponivel } from '../models/regra.model';

@Injectable({ providedIn: 'root' })
export class RegraService {
  private readonly http = inject(HttpClient);
  private catalogo$?: Observable<RegraDisponivel[]>;

  listarCatalogo(): Observable<RegraDisponivel[]> {
    if (!this.catalogo$) {
      this.catalogo$ = this.http
        .get<RegraDisponivel[]>(`${API_BASE_URL}/regras`)
        .pipe(shareReplay(1));
    }
    return this.catalogo$;
  }
}
