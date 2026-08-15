using FluentValidation;
using LayoutValidator.Core;
using Microsoft.Extensions.DependencyInjection;

namespace LayoutValidator.LayoutFuncionario;

public static class ExtensoesColecaoServicos
{
    public static IServiceCollection AdicionarValidadorLayoutFuncionario(this IServiceCollection servicos)
    {
        servicos.AddValidatorsFromAssemblyContaining<FuncionarioValidador>();
        servicos.AddSingleton<ILayoutMapper<FuncionarioRaw, Funcionario>, FuncionarioMapper>();
        servicos.AddScoped<IValidadorLayout<Funcionario>, FuncionarioValidadorLayout>();
        return servicos;
    }
}
