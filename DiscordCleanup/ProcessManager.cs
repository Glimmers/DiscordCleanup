using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace DiscordCleanup
{
    public readonly struct DiscordProcess
    {
        public Process Process { get; }
        public int PID { get; }
        public string Name { get; }
        public long MemorySize { get; }

        public DiscordProcess(Process process, int pid, string name, long memorysize)
        {
            Process = process;
            PID = pid;
            Name = name;
            MemorySize = memorysize;
        }
    }

    class ProcessManager
    {
        private Process[] _processes;
        string _friendlyName;

        public ProcessManager(string friendlyName)
        {
            _friendlyName = friendlyName;
            _processes = Process.GetProcessesByName(friendlyName);
        }

        public void KillAll()
        {
            _processes = Process.GetProcessesByName(_friendlyName);

            // loop, killing running processes
            do
            {
                
                List<Task> toKill = new List<Task>();

                // Kill everything in parallel
                var processes = _processes.Select(
                    (t) => { return Task.Run(() => { if (!t.HasExited) t.Kill(); });  }
                    ).ToArray();

                // Wait until we're done.
                Task.WaitAll(processes);
                _processes = Process.GetProcessesByName(_friendlyName); // List any still running processes

            } while (_processes.Length != 0); // Loop if there are any stragglers
        }

        public Process[] Processes {
            get => _processes;
        }

        public DiscordProcess[] GetProcesses()
        {
            return _processes.Select((t) => 
                { return new DiscordProcess(t, t.Id, t.ProcessName, t.PrivateMemorySize64); })
                .ToArray();
        }
    }
}
