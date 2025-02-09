using Loader;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Drawing;

using System.Threading;

using System.Windows.Forms;

namespace KeyAuth
{
    public partial class Main : Form
    {
        private ESP esp = new ESP();
        private bool espEnabled = false;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();






        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private void WriteDebug(string message)
        {
            System.IO.File.AppendAllText("debug.txt", $"{DateTime.Now}: {message}\n");
        }
        public Main()
        {


            InitializeComponent();
            WriteDebug("Main form constructor started");

            esp = new ESP();
            WriteDebug("ESP object created");




            WriteDebug("ESP checkbox setup complete");

            Task.Run(() => InitializeESP());
            WriteDebug("ESP initialization started");

            StartESPLoop();
            WriteDebug("ESP loop started");
        }



        private async Task InitializeESP()
        {
            await Task.Run(() =>
            {
                try
                {
                    GameProcess.Initialize();
                    esp.Initialize();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}\nMake sure CS2 is running and you have administrator privileges.");
                }
            });
        }




        private void StartESPLoop()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (esp.espEnabled)
                        {
                            WriteDebug($"ESP Drawing Attempt - Process ID: {GameProcess.Process.Id}");
                            WriteDebug($"Memory Reading from: {(long)GameProcess.ClientDll:X}");
                            esp.DrawESP();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebug($"ESP Drawing Error: {ex.Message}");
                    }
                    Thread.Sleep(1);
                }
            });
        }








        // Add this method
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            base.OnFormClosing(e);
        }

        /*
        * 
        * WATCH THIS VIDEO TO SETUP APPLICATION: https://www.youtube.com/watch?v=RfDTdiBq4_o
        * 
	     * READ HERE TO LEARN ABOUT KEYAUTH FUNCTIONS https://github.com/KeyAuth/KeyAuth-CSHARP-Example#keyauthapp-instance-definition
		 *
        */

        string chatchannel = "test"; // chat channel name, must be set in order to send/retrieve messages




        private void Main_Load(object sender, EventArgs e)
        {
            userDataField.Items.Add($"Username: {Login.KeyAuthApp.user_data.username}");
            userDataField.Items.Add($"License: {Login.KeyAuthApp.user_data.subscriptions[0].key}"); // this can be used if the user used a license, username, and password for register. It'll display the license assigned to the user
            userDataField.Items.Add($"Expires: {UnixTimeToDateTime(long.Parse(Login.KeyAuthApp.user_data.subscriptions[0].expiry))}");
            userDataField.Items.Add($"Subscription: {Login.KeyAuthApp.user_data.subscriptions[0].subscription}");
            userDataField.Items.Add($"IP: {Login.KeyAuthApp.user_data.ip}");
            userDataField.Items.Add($"HWID: {Login.KeyAuthApp.user_data.hwid}");
            userDataField.Items.Add($"Creation Date: {UnixTimeToDateTime(long.Parse(Login.KeyAuthApp.user_data.createdate))}");
            userDataField.Items.Add($"Last Login: {UnixTimeToDateTime(long.Parse(Login.KeyAuthApp.user_data.lastlogin))}");
            userDataField.Items.Add($"Time Left: {expirydaysleft()}");

            var onlineUsers = Login.KeyAuthApp.fetchOnline();
            if (onlineUsers != null)
            {
                Console.Write("\n Online users: ");
                foreach (var user in onlineUsers)
                {
                    onlineUsersField.Items.Add(user.credential + ", ");
                }
                Console.WriteLine("\n");
            }
        }

        public static bool SubExist(string name, int len)
        {
            for (var i = 0; i < len; i++)
            {
                if (Login.KeyAuthApp.user_data.subscriptions[i].subscription == name)
                {
                    return true;
                }
            }
            return false;
        }
        public string expirydaysleft()
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(long.Parse(Login.KeyAuthApp.user_data.subscriptions[0].expiry)).ToLocalTime();
            TimeSpan difference = dtDateTime - DateTime.Now;
            return Convert.ToString(difference.Days + " Days " + difference.Hours + " Hours Left");
        }

        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            try
            {
                dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            }
            catch
            {
                dtDateTime = DateTime.MaxValue;
            }
            return dtDateTime;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            chatroomGrid.Rows.Clear();
            timer1.Interval = 15000; // get chat messages every 15 seconds
            if (!String.IsNullOrEmpty(chatchannel))
            {
                var messages = Login.KeyAuthApp.chatget(chatchannel);
                if (messages == null)
                {
                    chatroomGrid.Rows.Insert(0, "KeyAuth", "No Chat Messages", UnixTimeToDateTime(DateTimeOffset.Now.ToUnixTimeSeconds()));
                }
                else
                {
                    foreach (var message in messages)
                    {
                        chatroomGrid.Rows.Insert(0, message.author, message.message, UnixTimeToDateTime(long.Parse(message.timestamp)));
                    }
                }
            }
            else
            {
                timer1.Stop();
                chatroomGrid.Rows.Insert(0, "KeyAuth", "No Chat Messages", UnixTimeToDateTime(DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
        }

        private void sendWebhookBtn_Click_1(object sender, EventArgs e)
        {
            Login.KeyAuthApp.webhook(webhookID.Text, webhookBaseURL.Text);
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void setUserVarBtn_Click_1(object sender, EventArgs e)
        {
            Login.KeyAuthApp.setvar(varField.Text, varDataField.Text);
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void fetchUserVarBtn_Click_1(object sender, EventArgs e)
        {
            Login.KeyAuthApp.getvar(varField.Text);
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void sendLogDataBtn_Click(object sender, EventArgs e)
        {
            Login.KeyAuthApp.log(logDataField.Text);
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void checkSessionBtn_Click_1(object sender, EventArgs e)
        {
            Login.KeyAuthApp.check();
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void fetchGlobalVariableBtn_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(Login.KeyAuthApp.var(globalVariableField.Text));
            MessageBox.Show(Login.KeyAuthApp.response.message); // optional since it'll show the response in the var (if it's valid or not)
        }

        private void sendMsgBtn_Click_1(object sender, EventArgs e)
        {
            if (Login.KeyAuthApp.chatsend(chatMsgField.Text, chatchannel))
            {
                chatroomGrid.Rows.Insert(0, Login.KeyAuthApp.user_data.username, chatMsgField.Text, UnixTimeToDateTime(DateTimeOffset.Now.ToUnixTimeSeconds()));
            }
            else
            {
                MessageBox.Show(Login.KeyAuthApp.response.message);
            }
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Login.KeyAuthApp.logout(); // ends the sessions once the application closes
            Environment.Exit(0);
        }

        private void minBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

        private void downloadFileBtn_Click(object sender, EventArgs e)
        {
            byte[] result = Login.KeyAuthApp.download("");
            if (!Login.KeyAuthApp.response.success)
            {
                Console.WriteLine("\n Status: " + Login.KeyAuthApp.response.message);
                Thread.Sleep(2500);
                Environment.Exit(0);
            }
            else
                File.WriteAllBytes($@"{filePathField.Text}" + $"\\{fileExtensionField.Text}", result);
        }

        private void enableTfaBtn_Click(object sender, EventArgs e)
        {
            string code = string.IsNullOrEmpty(tfaField.Text) ? null : tfaField.Text;

            Login.KeyAuthApp.enable2fa(code);

            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void disableTfaBtn_Click(object sender, EventArgs e)
        {
            Login.KeyAuthApp.disable2fa(tfaField.Text);
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }

        private void banBtn_Click(object sender, EventArgs e)
        {
            Login.KeyAuthApp.ban("Testing ban function");
            MessageBox.Show(Login.KeyAuthApp.response.message);
        }
        public void ToggleESP(bool enabled)
        {
            espEnabled = enabled;
            WriteDebug($"ESP Toggle called - New state: {enabled}");
            WriteDebug($"Client.dll base: {(long)GameProcess.ClientDll:X}");
            WriteDebug($"EntityList offset: {Offsets.dwEntityList:X}");
            WriteDebug($"LocalPlayer offset: {Offsets.dwLocalPlayer:X}");
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            if (esp.espEnabled && GameProcess.IsRunning())
            {
                esp.DrawESP();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                esp.Initialize();
                esp.espEnabled = true;
                timer2.Start();  // Start continuous updates
            }
            else
            {
                esp.espEnabled = false;
                timer2.Stop();
            }
        }
    }
}