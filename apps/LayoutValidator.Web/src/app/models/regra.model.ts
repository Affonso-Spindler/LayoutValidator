export type TipoParametro = 'Inteiro' | 'Decimal' | 'Texto' | 'ListaDeTexto';

export interface ParametroEsperado {
  nome: string;
  tipo: TipoParametro;
  obrigatorio: boolean;
}

export interface RegraDisponivel {
  chave: string;
  parametrosEsperados: ParametroEsperado[];
}
