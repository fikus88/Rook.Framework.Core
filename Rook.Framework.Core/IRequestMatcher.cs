using System;

namespace Rook.Framework.Core
{
    public interface IRequestMatcher
    {
        bool RegisterWaitHandle(Guid uuid, DataWaitHandle handle, ResponseStyle responseStyle);
        bool RegisterMessageWrapper(Guid uuid, MessageWrapper wrapper);
    }
}