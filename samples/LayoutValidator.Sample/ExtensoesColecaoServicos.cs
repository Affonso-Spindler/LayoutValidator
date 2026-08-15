using FluentValidation;
using LayoutValidator.Core;
using LayoutValidator.Sample.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LayoutValidator.Sample;

public static class ExtensoesColecaoServicos
{
    public static IServiceCollection AdicionarValidadorLayoutPessoa(this IServiceCollection servicos)
    {
        servicos.AddValidatorsFromAssemblyContaining<PessoaValidador>();
        servicos.AddSingleton<ILayoutMapper<PessoaRaw, Pessoa>, PessoaMapper>();
        servicos.AddScoped<IValidadorLayout<Pessoa>, PessoaValidadorLayout>();
        return servicos;
    }
}
