using System;

namespace Rook.Framework.Core.AnalyticsPump
{
    public class DataPumpException : Exception
    {
        public DataPumpException(string message):base(message)
        {            
        }
    }
}