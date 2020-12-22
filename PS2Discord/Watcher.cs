using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PS2Discord
{
    public class Watcher
    {
        public Watcher(IPAddress ps2IP, System.Timers.Timer pingTimer, DiscordRpcClient client, Form1 form)
        {
            this.ps2IP = ps2IP;
            this.client = client;
            this.pinger = pingTimer;
            this.form = form;
            SmbSessionWatcher.EventArrived += new EventArrivedEventHandler(HandleEvent);
            SmbOpenFileWatcher.EventArrived += new EventArrivedEventHandler(HandleEvent);
            SmbSessionWatcher.EventArrived += new EventArrivedEventHandler(HandleLogic);
            SmbOpenFileWatcher.EventArrived += new EventArrivedEventHandler(HandleLogic);
            states = new enums();
            states.currentState = enums.state.Disconnected;
            gamesLoaded = new List<string>();
        }
        private Form1 form { get; set; }
        private System.Timers.Timer pinger { get; set; }
        private DiscordRpcClient client { get; set; }
        private DiscordRPC.RichPresence presence = new DiscordRPC.RichPresence();
        public string? filePath { get; set; }
        public string? clientComputerName { get; set; }
        public ulong? sessionID { get; set; }
        public uint? secondsExisted { get; set; }
        public uint? secondsIdle { get; set; }
        public string? clientComputerNameS { get; set; }
        public ulong? sessionIDS { get; set; }
        public ulong? filesOpen { get; set; }
        public IPAddress ps2IP { get; set; }
        public IPAddress localhost { get { return IPAddress.Parse("127.0.0.1"); } }

        public string nowPlayingName { get; set; }
        public string nowPlayingSerial { get; set; }

        public enums states { get; set; }

        private static ManagementScope scope = new ManagementScope(@"\\.\root\Microsoft\Windows\SMB");
        private static WqlEventQuery query = new WqlEventQuery(

           @"SELECT * 
  FROM 
      __InstanceOperationEvent WITHIN 1 
  WHERE 
      TargetInstance ISA 'MSFT_SmbOpenFile'"

           );
        private static WqlEventQuery query2 = new WqlEventQuery(

@"SELECT * 
  FROM 
      __InstanceOperationEvent WITHIN 1 
  WHERE 
      TargetInstance ISA 'MSFT_SmbSession'"

);

        public ManagementEventWatcher SmbSessionWatcher = new ManagementEventWatcher(scope, query2);
        public ManagementEventWatcher SmbOpenFileWatcher = new ManagementEventWatcher(scope, query);

        string? lastFileOpen;
        public List<string> gamesLoaded { get; set; }
        public Timestamps time;

        // trebuie rescrisa logica la detectarea jocurilor sa includa nowplaying si gameloaded.last
        private void HandleEvent(object sender,
  EventArrivedEventArgs e)
        {

            var instanceDescription = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
            if (instanceDescription.ClassPath.ClassName == "MSFT_SmbOpenFile")
            {
                try
                {
                    clientComputerName = (string)instanceDescription.GetPropertyValue("ClientComputerName").ToString(); // It may throw an except
                    if (clientComputerName == ps2IP.ToString())
                    {
                        if (filePath != null)
                        {
                            lastFileOpen = filePath;
                            filePath = instanceDescription.GetPropertyValue("Path").ToString();
                        }// It may throw an except
                        sessionID = Convert.ToUInt64(instanceDescription.GetPropertyValue("SessionId").ToString());
                    }
                }
                catch (Exception ex) { }
            }
            else if (instanceDescription.ClassPath.ClassName == "MSFT_SmbSession")
            {
                try
                {
                    clientComputerNameS = instanceDescription.GetPropertyValue("ClientComputerName").ToString(); // It may throw an except
                    if (clientComputerName == ps2IP.ToString())
                    {
                        sessionIDS = Convert.ToUInt64(instanceDescription.GetPropertyValue("SessionId").ToString()); // It may throw an except
                        secondsExisted = Convert.ToUInt32(instanceDescription.GetPropertyValue("SecondsExists").ToString()); // It may throw an except
                        secondsIdle = Convert.ToUInt32(instanceDescription.GetPropertyValue("SecondsIdle").ToString()); // It may throw an except
                        filesOpen = Convert.ToUInt64(instanceDescription.GetPropertyValue("NumOpens")); // It may throw an except
                    }
                }
                catch (Exception ex) { }
            }
        }
        private ulong? currentSession;
        private string lastPlayed;
        private void HandleLogic(object sender, EventArrivedEventArgs e) {

            if (filesOpen > 0 && clientComputerNameS == ps2IP.ToString())
            {
                if ((states.currentState == enums.state.Disconnected || states.currentState == enums.state.Awaiting_Connection) && !pinger.Enabled)
                {
                    states.currentState = enums.state.Connected;
                    pinger.Enabled = true;
                    try
                    {
                        client.Initialize();
                    }
                    catch (Exception ex) { }
                }
                if (clientComputerName == clientComputerNameS && filePath != String.Empty && lastFileOpen != filePath && lastFileOpen != null)
                {
                    if (!filePath.Contains(".VCD") && !filePath.Contains(".iso"))
                        form.Logger("PS2 has accessed: " + filePath);
                    try
                    {
                        var arrayPath = filePath.Split('\\');
                        var arrayPathDot = arrayPath[arrayPath.Length - 1].Split('.');
                        var name = arrayPathDot[arrayPathDot.Length - 2];
                        if (!gamesLoaded.Contains(filePath) && (filePath.Contains(".VCD") || filePath.Contains(".iso")))
                        gamesLoaded.Add(filePath);
                        if (filePath == gamesLoaded.Last() || (lastPlayed == name && currentSession !=sessionIDS))
                        {
                            if (nowPlayingName != name)
                            {
                                var serial = (arrayPathDot.First() + arrayPathDot[1]).Replace('_', ' ');
                                if (filePath.Contains(".iso"))
                                {
                                    form.Logger("PS2 is now playing: " + filePath);
                                    states.nowPlayingType = enums.playing.PS2;
                                }
                                if (filePath.Contains(".VCD"))
                                {
                                    form.Logger("PS2 is now playing PS1 game: " + filePath);
                                    states.nowPlayingType = enums.playing.PS1;
                                }
                                time = Timestamps.Now;
                                presence.Timestamps = time;
                                nowPlayingName = name;
                                lastPlayed = name;
                                nowPlayingSerial = serial;
                                currentSession = sessionIDS;
                            }
                        }
                    }catch(Exception ex) { }
                }
            }
            else if (filesOpen > 0 && clientComputerNameS == ps2IP.ToString() && filePath == null)
            {
                form.Logger("Ps2 is playing something but cannot get the filepath, waiting for next file request");
            }
            else if (filesOpen <= 0 && clientComputerNameS == ps2IP.ToString())
            {
                filePath = String.Empty;
            }
            if ((secondsExisted % 420) == 0 && currentSession == sessionIDS) {
                gamesLoaded.Clear();
            }
        }
    }

}
