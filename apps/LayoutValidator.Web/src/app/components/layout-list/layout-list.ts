import { Component, OnInit, inject, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Layout } from '../../models/layout.model';
import { LayoutService } from '../../services/layout.service';

@Component({
  selector: 'app-layout-list',
  imports: [MatButtonModule, MatIconModule, MatTableModule],
  templateUrl: './layout-list.html',
  styleUrl: './layout-list.css',
})
export class LayoutList implements OnInit {
  private readonly layoutService = inject(LayoutService);
  private readonly snackBar = inject(MatSnackBar);

  readonly editar = output<Layout>();

  readonly layouts = signal<Layout[]>([]);
  readonly carregando = signal(false);

  readonly colunas = ['codigo', 'nome', 'delimitador', 'numeroDeCampos', 'acoes'];

  ngOnInit(): void {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.layoutService.listar().subscribe({
      next: (layouts) => {
        this.layouts.set(layouts);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.snackBar.open('Erro ao carregar os layouts.', 'OK', { duration: 4000 });
      },
    });
  }

  onEditar(layout: Layout): void {
    this.editar.emit(layout);
  }

  remover(layout: Layout): void {
    if (!window.confirm(`Remover o layout "${layout.codigo}"? Essa ação não pode ser desfeita.`)) {
      return;
    }
    this.layoutService.remover(layout.codigo).subscribe({
      next: () => {
        this.snackBar.open('Layout removido.', 'OK', { duration: 3000 });
        this.recarregar();
      },
      error: () => this.snackBar.open('Erro ao remover o layout.', 'OK', { duration: 4000 }),
    });
  }
}
