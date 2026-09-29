/*
using System.Windows;

namespace RadishNotepad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
#endif
        }
    }

}
*/
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RadishNotepad
{
    public partial class App : Application
    {
        private const string PipeName = "RadishNotepad_Pipe_UniqueKey12345";
        private const string MutexName = "RadishNotepad_Mutex_UniqueKey12345";
        private static Mutex appMutex;

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
#endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Try to acquire the system-wide Mutex
            appMutex = new Mutex(true, MutexName, out bool isFirstInstance);

            if (!isFirstInstance)
            {
                // ANOTHER INSTANCE IS ALREADY RUNNING:
                // Send the command line args to the existing instance via Named Pipe
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    SendArgsToFirstInstance(args[1]);
                }

                // Close this second instance immediately
                Shutdown();
                return;
            }

            // THIS IS THE FIRST INSTANCE:
            base.OnStartup(e);

            // Start listening for subsequent file open requests on a background thread
            StartPipeServer();
        }

        private void SendArgsToFirstInstance(string filePath)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(500); // 500ms timeout
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(filePath);
                    }
                }
            }
            catch
            {
                // Handle or ignore timeout/connection issues
            }
        }

        private void StartPipeServer()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                        {
                            server.WaitForConnection();
                            using (var reader = new StreamReader(server))
                            {
                                string filePath = reader.ReadLine();
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    // Switch to the UI thread to open the file and bring window to front
                                    Dispatcher.Invoke(() =>
                                    {
                                        if (Current.MainWindow is MainWindow mainWindow)
                                        {
                                            mainWindow.OpenFilePath(filePath);

                                            // Restore and bring the existing window to focus
                                            if (mainWindow.WindowState == WindowState.Minimized)
                                            {
                                                mainWindow.WindowState = WindowState.Normal;
                                            }
                                            mainWindow.Activate();
                                            mainWindow.Topmost = true;  // Temporary focus boost
                                            mainWindow.Topmost = false;
                                        }
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Handle potential pipe server errors
                    }
                }
            });
        }
    }
}