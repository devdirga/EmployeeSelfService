using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public class LogBuilder
    {
        public List<LogEntry> Entries { get; } = new List<LogEntry>();

        public void Add(LogLevel level, string module, string message)
        {
            Entries.Add(new LogEntry()
            {
                Timestamp = DateTime.Now,
                Level = level,
                Module = module,
                Message = message
            });
        }

        public void Add(LogLevel level, string message)
        {
            Add(level, "", message);
        }

        Dictionary<int, LogTrack> _tracked = new Dictionary<int, LogTrack>();
        public int BeginTrack(LogLevel level, string module, string message)
        {
            var rand = new Random();
            var rval = rand.Next();
            while (_tracked.ContainsKey(rval))
                rval = rand.Next();
            _tracked.Add(rval, new LogTrack()
            {
                TrackBegin = DateTime.Now,
                Level = level,
                Module = module,
                Message = message
            });
            return rval;
        }
        public int BeginTrack(LogLevel level, string message)
        {
            return BeginTrack(level, "", message);
        }

        public void EndTrack(int id)
        {
            if (!_tracked.ContainsKey(id)) return;
            var tr = _tracked[id];
            _tracked.Remove(id);
            var e = tr.ToEntry(DateTime.Now);
            Entries.Add(e);
        }


        public override string ToString()
        {
            return string.Join("\r\n", Entries.Select(e => e.ToString()));
        }
        public string ToString(string format)
        {
            return string.Join("\r\n", Entries.Select(e => e.ToString(format)));
        }

        public void SaveTo(string path, string format = null)
        {
            using (var fo = File.CreateText(path))
            {
                string content = "";
                if (!string.IsNullOrWhiteSpace(format))
                    content = ToString(format);
                else
                    content = ToString();
                fo.Write(content);
                fo.Flush();
            }
        }
    }

    public struct LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return ToString("[%l] %dt %o: %m");
        }

        public string ToString(string format)
        {
            format = format ?? "";
            format = format.Replace("%dt", Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff"));
            format = format.Replace("%d", Timestamp.ToString("yyyy-MM-dd"));
            format = format.Replace("%t", Timestamp.ToString("hh:mm:ss.fff"));
            format = format.Replace("%o", Module ?? "");
            format = format.Replace("%m", Message ?? "");
            format = format.Replace("%l", Level.ToString());
            return format;
        }
    }

    public struct LogTrack
    {
        public DateTime TrackBegin { get; set; }
        public DateTime TrackEnd { get; set; }
        public LogLevel Level { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }

        public LogEntry ToEntry(DateTime? trackEnd = null)
        {
            if (trackEnd.HasValue) TrackEnd = trackEnd.Value;
            var delta = TrackEnd - TrackBegin;
            return new LogEntry()
            {
                Timestamp = DateTime.Now,
                Level = Level,
                Module = Module,
                Message = Message + " (" + Math.Round(delta.TotalMilliseconds, 2) + "ms)"
            };
        }
    }

    public enum LogLevel
    {
        Error,
        Warning,
        Notice,
        Debug,
        Verbose
    }
}
