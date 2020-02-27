using System;

namespace Locus.Data
{
    public class LocusException : InvalidOperationException
    {
        public LocusException()
        {
        }

        public LocusException(string message)
            : base(message)
        {
        }

        public LocusException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
