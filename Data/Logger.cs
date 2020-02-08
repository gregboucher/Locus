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

        public void WriteLog(Exception ex)
        {
            try
            {
                string dir = _dir + "\\LogFile.txt";
                using (StreamWriter writer = new StreamWriter(dir, true, System.Text.Encoding.UTF8))
                {
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        writer.WriteLine(
                            "[" + DateTime.Now.ToString("dd-MM-yy HH:mm:ss") + "]"
                            + Environment.NewLine
                            + ex.ToString()
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
