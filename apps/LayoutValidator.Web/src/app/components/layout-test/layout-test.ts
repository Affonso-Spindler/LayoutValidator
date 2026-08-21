import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Layout } from '../../models/layout.model';
import { ResultadoValidacao } from '../../models/validacao.model';
import { LayoutService } from '../../services/layout.service';
import { ValidacaoService } from '../../services/validacao.service';

interface ResultadoLinha {
  linha: string;
  resultado?: ResultadoValidacao;
  erro?: string;
}

@Component({
  selector: 'app-layout-test',
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './layout-test.html',
  styleUrl: './layout-test.css',
})
export class LayoutTest implements OnInit {
  private readonly layoutService = inject(LayoutService);
  private readonly validacaoService = inject(ValidacaoService);
  private readonly snackBar = inject(MatSnackBar);

  readonly layouts = signal<Layout[]>([]);
  readonly codigoSelecionado = signal<string | null>(null);
  readonly linhas = signal('');
  readonly testando = signal(false);
  readonly resultados = signal<ResultadoLinha[]>([]);

  ngOnInit(): void {
    this.recarregar();
  }

  recarregar(): void {
    this.layoutService.listar().subscribe({
      next: (layouts) => this.layouts.set(layouts),
      error: () => this.snackBar.open('Erro ao carregar layouts.', 'OK', { duration: 4000 }),
    });
  }

  testar(): void {
    const codigo = this.codigoSelecionado();
    if (!codigo) {
      this.snackBar.open('Selecione um layout.', 'OK', { duration: 3000 });
      return;
    }

    const linhasParaTestar = this.linhas()
      .split('\n')
      .map((linha) => linha.trim())
      .filter((linha) => linha.length > 0);

    if (linhasParaTestar.length === 0) {
      this.snackBar.open('Cole ao menos uma linha para testar.', 'OK', { duration: 3000 });
      return;
    }

    this.testando.set(true);
    this.resultados.set([]);

    const chamadas = linhasParaTestar.map((linha) =>
      this.validacaoService.validarLinha(codigo, linha).pipe(
        catchError((erro) => of({ erro: erro?.error?.erro ?? 'Erro ao validar esta linha.' } as { erro: string })),
      ),
    );

    forkJoin(chamadas).subscribe((respostas) => {
      this.testando.set(false);
      this.resultados.set(
        respostas.map((resposta, indice) => {
          const linha = linhasParaTestar[indice];
          return 'erro' in resposta
            ? { linha, erro: resposta.erro }
            : { linha, resultado: resposta as ResultadoValidacao };
        }),
      );
    });
  }
}
