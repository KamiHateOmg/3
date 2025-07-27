using System;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace PremiumLoader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Enable console for debugging
                AllocConsole();
                Console.WriteLine("Starting loader [LOGI N  PAGE RIGHT HERE NOR,MALLY]...");
                
                base.OnStartup(e);
                
                // Set up global exception handling
                this.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                
                Console.WriteLine("Checking administrator privileges...");
                CheckAdministratorPrivileges();
                
                Console.WriteLine("Startup completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"STARTUP ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Startup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.ReadKey(); // Keep console open
                Shutdown();
            }
        }

        private void CheckAdministratorPrivileges()
        {
            try
            {
                Console.WriteLine("Checking if running as administrator...");
                
                if (!IsRunningAsAdministrator())
                {
                    Console.WriteLine("Not running as administrator - showing warning.");
                    var result = MessageBox.Show(
                        "This application requires administrator privileges for loading functionality.\n\n" +
                        "Would you like to continue without elevated privileges? (Loading may fail)",
                        "Administrator Privileges Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.No)
                    {
                        Console.WriteLine("User chose to exit due to admin privileges.");
                        Shutdown();
                    }
                }
                else
                {
                    Console.WriteLine("Running as administrator - OK.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking admin privileges: {ex.Message}");
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking admin role: {ex.Message}");
                return false;
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"DISPATCHER EXCEPTION: {e.Exception.Message}");
            Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
            
            MessageBox.Show(
                $"An unexpected error occurred:\n{e.Exception.Message}",
                "Premium Loader - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Console.WriteLine($"UNHANDLED EXCEPTION: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                MessageBox.Show(
                    $"A critical error occurred:\n{ex.Message}",
                    "Premium Loader - Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}