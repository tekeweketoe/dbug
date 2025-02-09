using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Guna.UI2.Native.WinApi;

namespace Loader
{

    public partial class Overlay : Form
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        public Overlay()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = false;
        }
        public void UpdateOverlayPosition()
        {
            var gameWindow = Process.GetProcessesByName("cs2")[0].MainWindowHandle;
            RECT rect;
            GetWindowRect(gameWindow, out rect);
            this.Location = new Point(rect.Left, rect.Top);
            this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
        private void Overlay_Load(object sender, EventArgs e)
        {
            // Any initialization code you want to run when the form loads
        }
    }
}