using System;
using System.Threading;
using System.Windows.Forms;

namespace KaitoLight
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Only one copy at a time
            Store.Load();
            L.Init(Store.Lang);

            // after a language switch the old copy may still be closing; wait briefly for it
            var restarting = Array.Exists(args, a => a.Equals("--restart", StringComparison.OrdinalIgnoreCase));
            using (var mutex = new Mutex(false, "KaitoLight.SingleInstance"))
            {
                bool isNew;
                try
                {
                    isNew = mutex.WaitOne(restarting ? 5000 : 0);
                }
                catch (AbandonedMutexException)
                {
                    isNew = true;
                }
                if (!isNew)
                {
                    MessageBox.Show(L.AlreadyRunning, "KaitoLight",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Lives in the notification area; "--show" opens the window on start, "--tray" starts silently (used at sign-in)
                var showWindow = Array.Exists(args, a => a.Equals("--show", StringComparison.OrdinalIgnoreCase));
                var silent = Array.Exists(args, a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
                Application.Run(new MainForm(!showWindow, silent));
            }
        }
    }
}
