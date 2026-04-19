using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VRise.Tools
{
    /// <summary>
    /// Thread-safe logger for packet / event parsing errors. All messages land in
    /// parse_errors.log (rotated at 10 MB) so a changed game EventCode can't crash
    /// the sniffer thread and the user still has a forensic trail of what failed.
    /// Repeated identical errors within a short window are collapsed into a single
    /// entry with a suppressed-count tail, to keep the file readable when a broken
    /// event fires on every packet.
    /// </summary>
    public static class ParseErrorLogger
    {
        private const string LogFile = "parse_errors.log";
        private const string RotatedFile = "parse_errors.log.old";
        private const long MaxFileSize = 10L * 1024 * 1024;
        private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(5);

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, DedupState> _dedup =
            new Dictionary<string, DedupState>();

        public static void Log(string tag, Exception ex,
            IReadOnlyDictionary<byte, object> parameters = null)
        {
            if (ex == null)
            {
                LogWarning(tag, "<null exception>");
                return;
            }

            string signature = (tag ?? "?") + "|" + ex.GetType().FullName + "|" + (ex.Message ?? "");
            string body = BuildEntry(tag, ex, parameters);
            WriteDeduplicated(signature, body);
        }

        public static void LogWarning(string tag, string message)
        {
            string signature = "WARN|" + (tag ?? "?") + "|" + (message ?? "");
            string body = "=== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                          " [" + (tag ?? "Parse") + "] " + (message ?? "");
            WriteDeduplicated(signature, body);
        }

        private static void WriteDeduplicated(string signature, string body)
        {
            string toWrite;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (_dedup.TryGetValue(signature, out var state) &&
                    now - state.LastWritten < DedupWindow)
                {
                    state.SuppressedCount++;
                    _dedup[signature] = state;
                    return;
                }

                int suppressed = 0;
                if (_dedup.TryGetValue(signature, out var prev))
                    suppressed = prev.SuppressedCount;

                _dedup[signature] = new DedupState
                {
                    LastWritten = now,
                    SuppressedCount = 0
                };

                toWrite = suppressed > 0
                    ? "(suppressed " + suppressed + " duplicate(s) of previous entry)\n" + body
                    : body;
            }

            try
            {
                lock (_lock)
                {
                    RotateIfNeeded();
                    File.AppendAllText(LogFile, toWrite + "\n", Encoding.UTF8);
                }
            }
            catch
            {
                // Never let the logger itself crash the caller thread.
            }

            // Mirror to console for live debugging sessions.
            try { Console.WriteLine(toWrite); } catch { }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                var fi = new FileInfo(LogFile);
                if (fi.Exists && fi.Length > MaxFileSize)
                {
                    if (File.Exists(RotatedFile)) File.Delete(RotatedFile);
                    File.Move(LogFile, RotatedFile);
                }
            }
            catch
            {
                // Rotation failure is non-fatal — worst case the file keeps growing.
            }
        }

        private static string BuildEntry(string tag, Exception ex,
            IReadOnlyDictionary<byte, object> parameters)
        {
            var sb = new StringBuilder();
            sb.Append("=== ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
              .Append(" [").Append(tag ?? "Parse").Append("] ===\n");

            if (parameters != null && parameters.Count > 0)
            {
                sb.Append("params: ");
                var first = true;
                foreach (var kvp in parameters)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    var typeName = kvp.Value?.GetType().Name ?? "null";
                    if (kvp.Value is Array arr) typeName += "[" + arr.Length + "]";
                    sb.Append(kvp.Key).Append(':').Append(typeName);
                }
                sb.Append('\n');
            }

            int depth = 0;
            for (Exception cur = ex; cur != null; cur = cur.InnerException, depth++)
            {
                var indent = new string(' ', depth * 2);
                sb.Append(indent).Append('[').Append(depth).Append("] ")
                  .Append(cur.GetType().FullName).Append(": ")
                  .Append(cur.Message).Append('\n');

                if (!string.IsNullOrEmpty(cur.StackTrace))
                {
                    var lines = cur.StackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    int max = Math.Min(lines.Length, 8);
                    for (int i = 0; i < max; i++)
                        sb.Append(indent).Append("   ").Append(lines[i].TrimEnd()).Append('\n');
                    if (lines.Length > max)
                        sb.Append(indent).Append("   ... (").Append(lines.Length - max)
                          .Append(" more frames)\n");
                }
            }
            return sb.ToString().TrimEnd();
        }

        private struct DedupState
        {
            public DateTime LastWritten;
            public int SuppressedCount;
        }
    }
}
