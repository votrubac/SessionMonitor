using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace SessionMonitor
{
    public partial class SessionMonitorService : ServiceBase
    {
        const int defaultGracePeriodInMinutes = 10;
        const string eventLogSource = "User Session Monitor";
        const string eventLogName = "User Session Monitor";
        const string processName = "UiPath.Agent";
        readonly Timer timer = new Timer();

        int lockedSessionId = -1;

        public SessionMonitorService()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists(eventLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventLogSource, eventLogName);
            }
            eventLog1.Source = eventLogSource;
            eventLog1.Log = eventLogName;

            // 10 minutes interval.
            var gracePeriod = defaultGracePeriodInMinutes;
            int.TryParse(ConfigurationManager.AppSettings["SessionLockGracePeriodInMinutes"], out gracePeriod);
            timer.Interval = gracePeriod * 60 * 1000;
            timer.AutoReset = false;
            timer.Enabled = false;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);

            eventLog1.WriteEntry($"Using {gracePeriod} minute(s) as the locked session grace period.");
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            eventLog1.WriteEntry("Session lock grace period ended.");
            try
            {
                KillProcess(lockedSessionId);
                lockedSessionId = -1;
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry($"Exception while handling locked session, code: {ex.HResult}, message: {ex.Message}.", EventLogEntryType.Error);
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
            eventLog1.WriteEntry($"Session status chagned, reason: {changeDescription.Reason.ToString()}, session id: {changeDescription.SessionId}.");

            try
            {
                if (changeDescription.Reason == SessionChangeReason.SessionLock)
                {
                    lockedSessionId = changeDescription.SessionId;
                    timer.Start();
                }
                else
                {
                    // Any session change event discard the locked session status.
                    lockedSessionId = -1;
                    timer.Stop();
                }

                if (changeDescription.Reason == SessionChangeReason.ConsoleDisconnect ||
                    changeDescription.Reason == SessionChangeReason.RemoteDisconnect)
                {
                    KillProcess(changeDescription.SessionId);
                }
                
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry($"Exception while handing session change, code: {ex.HResult}, message: {ex.Message}.", EventLogEntryType.Error);
            }
        }

        private void KillProcess(int sessionId)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                if (process.SessionId == sessionId)
                {
                    eventLog1.WriteEntry($"Killing the process {process.ProcessName} for session {process.SessionId}.");
                    process.Kill();
                }
            }
        }
    }
}
