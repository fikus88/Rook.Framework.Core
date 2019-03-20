namespace Rook.Framework.Core.Application.Bus
{
    public static class QueueConstants
    {
        public const string DefaultRoutingKey = "A.*";
        public const string ExchangeName = "mup_exchange";
        public const ushort QueueHeartBeatTimeOutInSeconds = 30;
    }
}