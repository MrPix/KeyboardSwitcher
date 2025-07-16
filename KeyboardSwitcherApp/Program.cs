using System.Windows.Forms;
using KeyboardLayoutSwitcher.Infrastructure.DependencyInjection;
using KeyboardLayoutSwitcher.Presentation.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace KeyboardLayoutSwitcher
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddKeyboardSwitcherServices();
            var serviceProvider = services.BuildServiceProvider();

            // Create and run the main application
            using var mainApp = serviceProvider.GetRequiredService<MainApplication>();
            mainApp.Start();

            // Run the application
            System.Windows.Forms.Application.Run();
        }
    }
}