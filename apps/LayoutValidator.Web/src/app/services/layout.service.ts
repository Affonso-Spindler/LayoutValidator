import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';
import { Layout } from '../models/layout.model';

@Injectable({ providedIn: 'root' })
export class LayoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/layouts`;

  listar(): Observable<Layout[]> {
    return this.http.get<Layout[]>(this.baseUrl);
  }

  obter(codigo: string): Observable<Layout> {
    return this.http.get<Layout>(`${this.baseUrl}/${codigo}`);
  }

  criar(layout: Layout): Observable<Layout> {
    return this.http.post<Layout>(this.baseUrl, layout);
  }

  atualizar(codigo: string, layout: Layout): Observable<Layout> {
    return this.http.put<Layout>(`${this.baseUrl}/${codigo}`, layout);
  }

  remover(codigo: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${codigo}`);
  }
}
