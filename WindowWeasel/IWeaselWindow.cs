using System.Diagnostics;

namespace WindowWeasel;

public interface IWeaselWindow
{
    internal Process AssociatedProcess { get; }

    internal SpecialWindow? MainWindow { get; }

    internal System.Threading.Timer WindowClock { get; }
}
