import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Layout } from '../../models/layout.model';
import { RegraDisponivel } from '../../models/regra.model';
import { LayoutService } from '../../services/layout.service';
import { RegraService } from '../../services/regra.service';

@Component({
  selector: 'app-layout-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './layout-form.html',
  styleUrl: './layout-form.css',
})
export class LayoutForm {
  private readonly fb = inject(FormBuilder);
  private readonly layoutService = inject(LayoutService);
  private readonly regraService = inject(RegraService);
  private readonly snackBar = inject(MatSnackBar);

  readonly layoutParaEditar = input<Layout | null>(null);
  readonly salvo = output<void>();

  readonly catalogo = signal<RegraDisponivel[]>([]);
  readonly salvando = signal(false);
  readonly editando = signal(false);
  readonly errosDeCadastro = signal<string[]>([]);

  readonly form: FormGroup = this.fb.group({
    codigo: this.fb.control({ value: '', disabled: false }, [Validators.required, Validators.maxLength(20)]),
    nome: this.fb.control('', Validators.required),
    delimitador: this.fb.control(';', Validators.required),
    campos: this.fb.array([]),
  });

  constructor() {
    this.regraService.listarCatalogo().subscribe((catalogo) => this.catalogo.set(catalogo));

    effect(() => {
      const layout = this.layoutParaEditar();
      this.resetForm(layout);
    });
  }

  get camposArray(): FormArray {
    return this.form.get('campos') as FormArray;
  }

  regrasArray(campoIndex: number): FormArray {
    return this.camposArray.at(campoIndex).get('regras') as FormArray;
  }

  parametrosGroup(campoIndex: number, regraIndex: number): FormGroup {
    return this.regrasArray(campoIndex).at(regraIndex).get('parametros') as FormGroup;
  }

  parametrosEsperados(chaveRegra: string): RegraDisponivel['parametrosEsperados'] {
    return this.catalogo().find((r) => r.chave === chaveRegra)?.parametrosEsperados ?? [];
  }

  resetForm(layout: Layout | null): void {
    this.errosDeCadastro.set([]);
    this.camposArray.clear();
    this.editando.set(layout !== null);

    if (layout) {
      this.form.patchValue({ codigo: layout.codigo, nome: layout.nome, delimitador: layout.delimitador });
      this.form.get('codigo')?.disable();
      for (const campo of layout.campos) {
        this.camposArray.push(this.criarCampoGroup(campo.nome, campo.regras));
      }
    } else {
      this.form.reset({ codigo: '', nome: '', delimitador: ';' });
      this.form.get('codigo')?.enable();
    }

    if (this.camposArray.length === 0) {
      this.addCampo();
    }
  }

  private criarCampoGroup(nome = '', regras: Layout['campos'][number]['regras'] = []): FormGroup {
    const regrasArray = this.fb.array(regras.map((r) => this.criarRegraGroup(r.chaveRegra, r.parametrosJson)));
    if (regrasArray.length === 0) {
      regrasArray.push(this.criarRegraGroup());
    }
    return this.fb.group({
      nome: this.fb.control(nome, Validators.required),
      regras: regrasArray,
    });
  }

  private criarRegraGroup(chaveRegra = '', parametrosExistentes: Record<string, unknown> | null = null): FormGroup {
    const grupo = this.fb.group({
      chaveRegra: this.fb.control(chaveRegra, Validators.required),
      parametros: this.fb.group({}),
    });
    if (chaveRegra) {
      this.preencherParametros(grupo, chaveRegra, parametrosExistentes);
    }
    return grupo;
  }

  addCampo(): void {
    this.camposArray.push(this.criarCampoGroup());
  }

  removeCampo(campoIndex: number): void {
    this.camposArray.removeAt(campoIndex);
  }

  addRegra(campoIndex: number): void {
    this.regrasArray(campoIndex).push(this.criarRegraGroup());
  }

  removeRegra(campoIndex: number, regraIndex: number): void {
    this.regrasArray(campoIndex).removeAt(regraIndex);
  }

  onRegraChange(campoIndex: number, regraIndex: number, chaveRegra: string): void {
    const regraGroup = this.regrasArray(campoIndex).at(regraIndex) as FormGroup;
    this.preencherParametros(regraGroup, chaveRegra);
  }

  private preencherParametros(
    regraGroup: FormGroup,
    chaveRegra: string,
    valoresExistentes: Record<string, unknown> | null = null,
  ): void {
    const parametros = this.fb.group({});
    for (const esperado of this.parametrosEsperados(chaveRegra)) {
      const valorBruto = valoresExistentes?.[esperado.nome];
      const valorInicial =
        esperado.tipo === 'ListaDeTexto' && Array.isArray(valorBruto)
          ? valorBruto.join(', ')
          : (valorBruto ?? '');
      const validadores = esperado.obrigatorio ? [Validators.required] : [];
      parametros.addControl(esperado.nome, this.fb.control(valorInicial, validadores));
    }
    regraGroup.setControl('parametros', parametros);
  }

  private montarParametrosJson(chaveRegra: string, parametros: FormGroup): Record<string, unknown> | null {
    const esperados = this.parametrosEsperados(chaveRegra);
    if (esperados.length === 0) {
      return null;
    }
    const resultado: Record<string, unknown> = {};
    for (const esperado of esperados) {
      const valor = parametros.get(esperado.nome)?.value;
      if (esperado.tipo === 'ListaDeTexto') {
        resultado[esperado.nome] = String(valor ?? '')
          .split(',')
          .map((v) => v.trim())
          .filter((v) => v.length > 0);
      } else if (esperado.tipo === 'Inteiro' || esperado.tipo === 'Decimal') {
        resultado[esperado.nome] = valor === '' || valor === null ? null : Number(valor);
      } else {
        resultado[esperado.nome] = valor;
      }
    }
    return resultado;
  }

  cancelarEdicao(): void {
    this.resetForm(null);
    this.salvo.emit();
  }

  salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const valor = this.form.getRawValue();
    const layout: Layout = {
      codigo: valor.codigo,
      nome: valor.nome,
      delimitador: valor.delimitador,
      campos: (valor.campos as Array<{ nome: string; regras: Array<{ chaveRegra: string; parametros: FormGroup }> }>).map(
        (campo, campoIndex) => ({
          nome: campo.nome,
          regras: campo.regras.map((regra, regraIndex) => ({
            chaveRegra: regra.chaveRegra,
            parametrosJson: this.montarParametrosJson(regra.chaveRegra, this.parametrosGroup(campoIndex, regraIndex)),
          })),
        }),
      ),
    };

    this.errosDeCadastro.set([]);
    this.salvando.set(true);
    const operacao = this.editando()
      ? this.layoutService.atualizar(layout.codigo, layout)
      : this.layoutService.criar(layout);

    operacao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.snackBar.open(this.editando() ? 'Layout atualizado.' : 'Layout cadastrado.', 'OK', { duration: 3000 });
        this.resetForm(null);
        this.salvo.emit();
      },
      error: (erro) => {
        this.salvando.set(false);
        const corpo = erro?.error;
        if (corpo?.erros?.length) {
          this.errosDeCadastro.set(corpo.erros);
        } else if (corpo?.erro) {
          this.errosDeCadastro.set([corpo.erro]);
        } else {
          this.snackBar.open('Erro inesperado ao salvar o layout.', 'OK', { duration: 4000 });
        }
      },
    });
  }
}
