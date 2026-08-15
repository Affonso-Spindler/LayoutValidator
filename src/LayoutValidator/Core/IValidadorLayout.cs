namespace LayoutValidator.Core;

public interface IValidadorLayout<T>
{
    IEnumerable<ResultadoValidacaoRegistro<T>> Validar(TextReader leitor);
}
