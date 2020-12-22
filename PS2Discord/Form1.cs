using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using DiscordRPC;

namespace PS2Discord
{
    public partial class Form1 : Form
    {
        public static Watcher currentWatcher;
        public static System.Timers.Timer pinger = new System.Timers.Timer(5000);
        public static System.Timers.Timer updateUI = new System.Timers.Timer(1000);

        public DiscordRpcClient client { get; set; }
        public RichPresence presence = new RichPresence();

        public Form1()
        {
            discordClient();
            checkForFirstRun();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel1.Text = currentWatcher.ps2IP.ToString();
            }
            catch (Exception ex) { }
            Logger("Logger started.");
            backgroundWorker1.RunWorkerAsync();
            Logger("Background worker started. ");
            pinger.Elapsed += new ElapsedEventHandler(PingerEventProcessor);
            updateUI.Elapsed += new ElapsedEventHandler(updateUITimer);
            updateUI.Enabled = true;
            updateUI.Start();
            statusStrip1.ShowItemToolTips = true;
            toolStripStatusLabel1.ToolTipText = "Right click to copy IP to clipboard";

        }
        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && Properties.Settings.Default.minimizeToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                notifyIcon1.Icon = null;
                notifyIcon1.Dispose();
            }
        }
        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Show();
            }
        }
        private void copyIPtoClipboar(object sender, EventArgs e) {
                Clipboard.SetText(toolStripStatusLabel1.Text);
        }
        

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            checkIfmeHide();
            Logger("SMB events added");
            currentWatcher.SmbOpenFileWatcher.Start();
            Logger("SMBOpenFileWatcher has started");
            currentWatcher.SmbSessionWatcher.Start();
            Logger("SMBSSessionWatcher has started");
            Logger("Awaiting events...");
            Ping p = new Ping();
            PingReply r;
            r = p.Send(currentWatcher.ps2IP);
            if (r.Status == IPStatus.Success)
            {
                currentWatcher.states.currentState = enums.state.Awaiting_Connection;
            }
            else
            {
                currentWatcher.states.currentState = enums.state.Disconnected;
            }
            updateUIFunction();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            Application.Exit();
        }
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Settings thisSettings = new Settings(currentWatcher);
            thisSettings.Show();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings thisSettings = new Settings(currentWatcher);
            thisSettings.Show();
        }

        private void checkForFirstRun()
        {
            //if (Debugger.IsAttached)
            //    Properties.Settings.Default.Reset();
            if (Properties.Settings.Default.firstRun)
            {
                this.Hide();
                firstRun run = new firstRun();
                if (run.ShowDialog(this) == DialogResult.OK)
                {
                    Properties.Settings.Default.firstRun = false;
                    Properties.Settings.Default.Save();
                    var x = Properties.Settings.Default.ps2IP;
                    currentWatcher = new Watcher(IPAddress.Parse(Properties.Settings.Default.ps2IP), pinger, client, this);
                }
                else {
                    this.Close();
                }
                run.Dispose();
            }
            else
            {
                currentWatcher = new Watcher(IPAddress.Parse(Properties.Settings.Default.ps2IP), pinger, client, this);
            }
        }
        private void checkIfmeHide()
        {
            if (Properties.Settings.Default.startMinimized)
            {
                this.InvokeEx(x => this.Hide());
            }
            else
            {
                this.InvokeEx(x => this.Show());
            }
        }
        private void discordClient()
        {
            client = new DiscordRpcClient("786900929684832277", autoEvents: true);
            client.SetPresence(presence);
        }

        public void Logger(string log)
        {
            this.textBox1.InvokeEx(tx => tx.Text += "[" + DateTime.Now + "] " + log + System.Environment.NewLine);
        }

        private void PingerEventProcessor(object sender, ElapsedEventArgs e)
        {
            Ping p = new Ping();
            PingReply r;
            r = p.Send(currentWatcher.ps2IP);
            if (r.Status == IPStatus.Success)
            {
                currentWatcher.states.currentState = enums.state.Connected;
                this.InvokeEx(x=> this.timer1.Enabled = false);
            }
            else
            {
                for (var i = 0; i < (currentWatcher.gamesLoaded.Count - 1); i++)
                {
                    if(i != (currentWatcher.gamesLoaded.Count-2))
                    currentWatcher.gamesLoaded.RemoveAt(i);
                }
                currentWatcher.nowPlayingName = null;
                currentWatcher.nowPlayingSerial = null;
                currentWatcher.states.nowPlayingType = enums.playing.nothing;
                currentWatcher.states.currentState = enums.state.Disconnected;
                client.ClearPresence();
                this.InvokeEx(x => this.timer1.Enabled = true);
                pinger.Stop();
            }
        }
        private void updateUITimer(object sender, ElapsedEventArgs e)
        {
            updateUIFunction();
        }

        private void updateUIFunction()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    toolStripStatusLabel1.Text = currentWatcher.ps2IP.ToString();
                    if (currentWatcher.states.currentState == enums.state.Connected) { toolStripStatusLabel2.Image = Properties.Resources.Connected; toolStripStatusLabel2.ToolTipText = "PS2 is connected to the network"; }
                    else if (currentWatcher.states.currentState == enums.state.Disconnected) { toolStripStatusLabel2.Image = Properties.Resources.Disconnected; toolStripStatusLabel2.ToolTipText = "PS2 is disconnected to the network"; }
                    else if (currentWatcher.states.currentState == enums.state.Awaiting_Connection) { toolStripStatusLabel2.Image = Properties.Resources.Awaiting_Connection; toolStripStatusLabel2.ToolTipText = "PS2 is connected to the network but not files were requested yet" + System.Environment.NewLine + "If you are playing a game before opening up this app, it won't show."; }

                    if (currentWatcher.nowPlayingName == null)
                    {
                        toolStripStatusLabel3.Text = "";
                        playingToolStripMenuItem.Visible = false;
                        try
                        {
                            currentWatcher.time = null;
                            client.ClearPresence();
                        }
                        catch (Exception ex) { }
                    }
                    else {
                        playingToolStripMenuItem.Visible = true;
                        playingToolStripMenuItem.Text = "Playing: " + currentWatcher.nowPlayingSerial;
                        toolStripStatusLabel3.Text = "Playing: " + currentWatcher.nowPlayingName + " " + currentWatcher.states.nowPlayingType;
                        presence.Details = currentWatcher.nowPlayingName;
                        presence.State = currentWatcher.nowPlayingSerial;
                        presence.WithTimestamps(currentWatcher.time);
                        string play = "";
                        string logo = "";
                        if (currentWatcher.states.nowPlayingType == enums.playing.PS2)
                        {
                            play = "Now playing a Playstation 2 Game";
                            logo = "ps2logo";
                        }
                        else {
                            play = "Now playing a Playstation 1 Game";
                            logo = "ps1logo";
                        }
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = "pslogo",
                            SmallImageKey = logo,
                            SmallImageText = play,
                            
                        };
                        try
                        {
                            client.SetPresence(presence);
                        }
                        catch (Exception ex) { }
                    }
                    client.Invoke();
                }));
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentWatcher.gamesLoaded.Clear();
        }
    }
    public static class ISynchronizeInvokeExtensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }
    }

}

