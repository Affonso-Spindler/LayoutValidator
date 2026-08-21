export interface RegraCampo {
  chaveRegra: string;
  parametrosJson?: Record<string, unknown> | null;
}

export interface Campo {
  nome: string;
  ordem?: number;
  regras: RegraCampo[];
}

export interface Layout {
  codigo: string;
  nome: string;
  delimitador: string;
  campos: Campo[];
}
