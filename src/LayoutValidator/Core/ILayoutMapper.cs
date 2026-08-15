namespace LayoutValidator.Core;

public interface ILayoutMapper<in TRaw, out T>
{
    T Map(TRaw raw);
}
