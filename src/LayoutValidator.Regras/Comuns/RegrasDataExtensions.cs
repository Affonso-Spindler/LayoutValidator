using FluentValidation;
using LayoutValidator.Regras.Predicados;

namespace LayoutValidator.Regras;

public static class RegrasDataExtensions
{
    public const string FormatoBrasileiro = "dd/MM/yyyy";

    public static IRuleBuilderOptions<T, string> Data<T>(this IRuleBuilder<T, string> regra, string formato = FormatoBrasileiro) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DataValida(valor, formato),
            "DataInvalida",
            $"'{{PropertyName}}' deve ser uma data válida no formato {formato}.");

    public static IRuleBuilderOptions<T, string> DataEntre<T>(
        this IRuleBuilder<T, string> regra,
        DateTime minimo,
        DateTime maximo,
        string formato = FormatoBrasileiro) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DataEntre(valor, minimo, maximo, formato),
            "DataForaDoIntervalo",
            $"'{{PropertyName}}' deve ser uma data entre {minimo.ToString(formato)} e {maximo.ToString(formato)}.");

    /// <summary>Data válida que não seja futura — hoje passa.</summary>
    public static IRuleBuilderOptions<T, string> DataNoPassado<T>(this IRuleBuilder<T, string> regra, string formato = FormatoBrasileiro) =>
        ConstrutorRegra.DeFormato(
            regra,
            valor => Formatos.DataNoPassado(valor, formato),
            "DataNoFuturo",
            "'{PropertyName}' não pode ser uma data futura.");
}
