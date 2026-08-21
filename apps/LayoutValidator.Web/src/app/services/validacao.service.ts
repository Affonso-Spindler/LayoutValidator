import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';
import { ResultadoValidacao } from '../models/validacao.model';

@Injectable({ providedIn: 'root' })
export class ValidacaoService {
  private readonly http = inject(HttpClient);

  validarLinha(codigo: string, linha: string): Observable<ResultadoValidacao> {
    return this.http.post<ResultadoValidacao>(
      `${API_BASE_URL}/layouts/${codigo}/validar`,
      { linha },
    );
  }
}
