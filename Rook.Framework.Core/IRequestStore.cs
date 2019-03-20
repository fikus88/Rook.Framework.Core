using System;
using System.Collections.Generic;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Backplane;

namespace Rook.Framework.Core
{
    public interface IRequestStore
    {
        Func<Guid> CreateUniqueId { get; set; }
        BusResponse<TSolution> PublishAndWaitForTypedResponse<TNeed, TSolution>(Message<TNeed, TSolution> message, ResponseStyle responseStyle = ResponseStyle.WholeSolution, Func<string, bool> solutionMatchFunction = null);
        JsonBusResponse PublishAndWaitForResponse<TNeed, TSolution>(Message<TNeed, TSolution> message, ResponseStyle responseStyle = ResponseStyle.WholeSolution,
            Func<string, bool> solutionMatchFunction = null);
        List<string> Methods { get; }
    }
}
