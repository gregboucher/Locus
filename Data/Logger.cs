using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Locus.Data
{
    public class Logger : ILogger
    {
        private readonly string _dir;

        public Logger(IWebHostEnvironment env)
        {
            _dir = env.ContentRootPath + "\\Log";
            try
            {
                Directory.CreateDirectory(_dir);
            }
            catch
            {
                //TODO
            }
        }

        public void WriteLog(LogEntry entry)
        {
            try
            {
                string dir = _dir + "\\LogFile.txt";
                using (StreamWriter writer = new StreamWriter(dir, true, System.Text.Encoding.UTF8))
                {
                    if (!string.IsNullOrEmpty(entry.Message) & !string.IsNullOrEmpty(entry.Path))
                    {
                        writer.WriteLine(
                            "[" + DateTime.Now.ToString("dd-MM-yy HH:mm:ss") + "]"
                            + Environment.NewLine
                            + "Path: " + entry.Path
                            + Environment.NewLine
                            + "Error: " + entry.Message
                            + Environment.NewLine);
                    }
                }
            }
            catch
            {
                //TODO
            }
        }
    }
}
