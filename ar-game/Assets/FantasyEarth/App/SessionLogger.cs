using System;
using System.Globalization;
using System.IO;
using System.Text;
using FantasyEarth.Diagnostics;
using UnityEngine;

namespace FantasyEarth.App
{
    public sealed class SessionLogger : MonoBehaviour
    {
        private StreamWriter _writer;
        private double _nextWriteTime;

        private void Awake()
        {
            if (!Debug.isDebugBuild) { enabled = false; return; }
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "sessions");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".jsonl");
                _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            }
            catch (IOException) { DisableLogging(); }
            catch (UnauthorizedAccessException) { DisableLogging(); }
        }

        public void Record(SessionLogEntry entry, bool force)
        {
            if (!Debug.isDebugBuild || _writer == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!force && now < _nextWriteTime) return;
            try
            {
                _writer.WriteLine(SessionLogFormatter.ToJsonLine(entry));
                _nextWriteTime = now + 0.5;
            }
            catch (IOException) { DisableLogging(); }
        }

        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Close();
        private void OnDestroy() => Close();

        private void Flush()
        {
            if (_writer == null) return;
            try { _writer.Flush(); }
            catch (IOException) { DisableLogging(); }
        }

        private void Close()
        {
            if (_writer == null) return;
            try { _writer.Dispose(); }
            catch (IOException) { Debug.LogWarning("Session log could not be flushed."); }
            finally { _writer = null; }
        }

        private void DisableLogging()
        {
            Close();
            enabled = false;
            Debug.LogWarning("Private session logging is unavailable; the camera can continue.");
        }
    }
}
