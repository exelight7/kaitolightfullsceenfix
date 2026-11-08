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
            using (var mutex = new Mutex(true, "KaitoLight.SingleInstance", out var isNew))
            {
                if (!isNew)
                {
                    MessageBox.Show("البرنامج يعمل بالفعل. يمكنك الوصول إليه من منطقة الإشعارات بجوار الساعة.", "KaitoLight",
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
