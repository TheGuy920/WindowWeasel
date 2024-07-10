using System.Diagnostics;
using System.Windows.Controls;
using WindowWeasel.WinAPI;
using Timer = System.Threading.Timer;

namespace WindowWeasel;

internal class WWindow : Grid, IWeaselWindow
{
    /// <summary>
    /// 
    /// </summary>
    public Process AssociatedProcess { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public SpecialWindow? MainWindow { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public Timer WindowClock { get; private set; }

    private readonly List<nint> allWindows = new(50);
    private readonly SynchronizationContext context;

    /// <summary>
    /// 
    /// </summary>
    private IEnumerable<nint> ProcessWindowHandles =>
        Win32.EnumerateProcessWindows(this.AssociatedProcess.Id).Where(Win32.IsMainWindow);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="process"></param>
    public WWindow(Process process)
    {
        this.AssociatedProcess = process;
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        this.context = SynchronizationContext.Current!;
        this.WindowClock = new Timer(this.WindowClockCallback, null, 0, 50);

        while (this.allWindows.Count <= 0)
        {
            Task.Delay(50).Wait();
            this.WindowClockCallback(null);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnExit(object? sender, EventArgs args)
    {
        this.WindowClock.Change(Timeout.Infinite, Timeout.Infinite);
        this.WindowClock.Dispose();

        this.AssociatedProcess.CloseMainWindow(); // try to be nice
        this.AssociatedProcess.Kill();
        this.AssociatedProcess.WaitForExit();
    }

    /// <summary>
    /// Dont rely on WinAPI for detecting window changes
    /// </summary>
    /// <param name="state"></param>
    private void WindowClockCallback(object? state)
    {
        if (this.MainWindow is null)
        {
            if (this.allWindows.Count > 0)
                return;

            var window = this.ProcessWindowHandles.FirstOrDefault(IntPtr.Zero);
            if (window != IntPtr.Zero)
            {
                this.allWindows.Add(window);
                this.context.Post(_ =>
                {
                    Debug.WriteLine($"[+] New MAIN: {window} = \"{Win32.GetWindowTitle(window)}\"");
                    this.MainWindow = new SpecialWindow(window, this.context);
                    this.Children.Add(this.MainWindow);
                }, null);
            }
        }
        else
        {
            if (!Win32.IsWindow(this.MainWindow.Handle) || !Win32.IsWindowVisible(this.MainWindow.Handle))
            {
                Debug.WriteLine($"[-] Main window closed ({Win32.GetWindowTitle(this.MainWindow.Handle)}), resetting...");
                this.allWindows.Clear();
                this.MainWindow = null;
                return;
            }

            var newWindows = this.ProcessWindowHandles.Except(this.allWindows);
            this.allWindows.AddRange(newWindows);

            foreach (var window in newWindows)
            {
                Debug.WriteLine($"[+] New CHILD: {window} = \"{Win32.GetWindowTitle(window)}\"");
                this.MainWindow.NewChildAvailable(this.MainWindow.Handle, window);
            }
        }
    }
}
