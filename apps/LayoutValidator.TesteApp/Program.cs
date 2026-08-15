using LayoutValidator.LayoutFuncionario;
using Microsoft.Extensions.DependencyInjection;

namespace LayoutValidator.TesteApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var servicos = new ServiceCollection()
            .AdicionarValidadorLayoutFuncionario()
            .AddTransient<TelaPrincipal>()
            .BuildServiceProvider();

        Application.Run(servicos.GetRequiredService<TelaPrincipal>());
    }
}
