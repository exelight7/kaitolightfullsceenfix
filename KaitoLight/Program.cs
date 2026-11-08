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
                    MessageBox.Show("KaitoLight شغّال من قبل — تلقاه بجنب الساعة في شريط المهام.", "KaitoLight",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var startHidden = Array.Exists(args, a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
                Application.Run(new MainForm(startHidden));
            }
        }
    }
}
