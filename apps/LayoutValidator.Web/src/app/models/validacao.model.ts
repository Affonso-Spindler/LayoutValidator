export interface ErroDeCampo {
  campo: string;
  valorRaw: string;
  regra: string;
  mensagem: string;
}

export interface ResultadoValidacao {
  aderente: boolean;
  erros: ErroDeCampo[];
}
