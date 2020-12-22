using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS2Discord
{
    public partial class Settings : Form
    {
        public static Watcher watcher;
        public Settings(Watcher watch)
        {
            watcher = watch;
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            var ipBoxes = Properties.Settings.Default.ps2IP.Split('.');
            ipMaskedTextBox1.FirstBox.Text = ipBoxes[0];
            ipMaskedTextBox1.SecondBox.Text = ipBoxes[1];
            ipMaskedTextBox1.ThirdBox.Text = ipBoxes[2];
            ipMaskedTextBox1.FourthBox.Text = ipBoxes[3];
            checkBox1.Checked = Properties.Settings.Default.runAtStartup;
            checkBox2.Checked = Properties.Settings.Default.minimizeToTray;
            checkBox3.Checked = Properties.Settings.Default.startMinimized;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ps2IP = ipMaskedTextBox1.FirstBox.Text + "." + ipMaskedTextBox1.SecondBox.Text + "." + ipMaskedTextBox1.ThirdBox.Text + "." + ipMaskedTextBox1.FourthBox.Text;
            Properties.Settings.Default.runAtStartup= checkBox1.Checked;
            Properties.Settings.Default.minimizeToTray = checkBox2.Checked;
            Properties.Settings.Default.startMinimized = checkBox3.Checked;
            WshShell wshShell = new WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut;
            string startUpFolderPath =
              Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (Properties.Settings.Default.runAtStartup)
            {
                // Create the shortcut
                shortcut =
                  (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(
                    startUpFolderPath + "\\" +
                    Application.ProductName + ".lnk");

                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.WorkingDirectory = Application.StartupPath;
                shortcut.Description = "Launch My Application";
                shortcut.IconLocation = Application.StartupPath + @"\ps2.ico";
                shortcut.Save();
            }
            else {
                if (System.IO.File.Exists(startUpFolderPath + "\\" +
                    Application.ProductName + ".lnk"))
                {
                    System.IO.File.Delete(startUpFolderPath + "\\" +
                    Application.ProductName + ".lnk");
                }
            }
            watcher.ps2IP = IPAddress.Parse(Properties.Settings.Default.ps2IP);
            Properties.Settings.Default.Save();
            this.Close();
            this.Dispose();
        }
    }
}
