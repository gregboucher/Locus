using System;

namespace Locus.Data
{
    public interface ILogger
    {
        public void WriteLog(Exception ex);
    }
}
