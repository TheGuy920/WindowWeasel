using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using WindowWeasel.WinAPI;
using Panel = System.Windows.Forms.Panel;

namespace WindowWeasel;

internal class SpecialWindow : Grid
{
    public readonly IntPtr Handle;

    private readonly List<SpecialWindow> childWindows;
    private readonly SynchronizationContext context;

    public SpecialWindow(nint handle, SynchronizationContext context)
    {
        this.Handle = handle;
        this.context = context;
        this.SizeChanged += this.SpecialWindowSizeChanged;

        // Debug.WriteLine($"[+] Captured window: {handle} = \"{Win32.GetWindowTitle(handle)}\" [{this.Children.Count}]");
        // Debug.WriteLine($"Collecting child windows...");
        this.childWindows = Win32.GetChildWindowHandles(handle)
                            .Where(wh => Win32.IsTargetWindow(wh, handle))    
                            .Select(wh => new SpecialWindow(wh, this.context))
                            .ToList();
        
        this.CaptureWindow(handle);
    }

    private void CaptureWindow(nint handle)
    {
        if (this.context == null)
            throw new InvalidOperationException("SynchronizationContext is null");

        // Validate window handle
        if (handle == IntPtr.Zero)
            return;

        var host = new WindowsFormsHost()
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            Background = System.Windows.Media.Brushes.Transparent
        };
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };

        Win32.SetParent(handle, panel.Handle);
        Win32.RemoveTitleBar(handle);
        Win32.MoveWindow(handle, 0, 0, (int)this.ActualWidth, (int)this.ActualHeight, true);

        // Remove border and caption
        int style = Win32.GetWindowLong(handle, Win32.GWL_STYLE);
        style &= ~Win32.WS_BORDER;
        style &= ~Win32.WS_CAPTION;
        style &= ~Win32.WS_THICKFRAME;
        _ = Win32.SetWindowLong(handle, Win32.GWL_STYLE, style);
            
        host.Child = panel;
        this.Children.Add(host);
    }

    private void SpecialWindowSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) =>
        Win32.MoveWindow(this.Handle, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, true);


    public void NewChildAvailable(nint parent, nint child)
    {
        // Creates a new SpecialWindow on the UI thread
        if (parent == Handle)
            this.context.Post(_ => this.childWindows.Add(new SpecialWindow(child, this.context)), null);
        // Trickles down the tree
        else
            foreach (var child_window in childWindows)
                child_window.NewChildAvailable(parent, child);
    }
}
