using KeyboardLayoutSwitcher.Presentation;
using WinFormsApp = System.Windows.Forms.Application;

namespace KeyboardLayoutSwitcher;

class Program
{
    [STAThread]
    static async Task Main()
    {
        WinFormsApp.EnableVisualStyles();
        WinFormsApp.SetCompatibleTextRenderingDefault(false);

        using var bootstrapper = new ApplicationBootstrapper();
        
        try
        {
            await bootstrapper.StartAsync();
            WinFormsApp.Run();
        }
        finally
        {
            await bootstrapper.StopAsync();
        }
    }
}