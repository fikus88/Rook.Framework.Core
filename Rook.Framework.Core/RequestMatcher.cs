using System;
using System.Linq;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core
{
    internal class RequestMatcher : IRequestMatcher
    {
        private readonly AutoDictionary<Guid, RequestData> requests = new AutoDictionary<Guid, RequestData>();

        public bool RegisterWaitHandle(Guid uuid, DataWaitHandle handle, ResponseStyle responseStyle)
        {
            lock (requests)
            {
                if (requests.ContainsKey(uuid))
                {
                    requests[uuid].SetHandleValue(handle);
                    requests[uuid].ResponseStyle = responseStyle;
                }
                else
                    requests[uuid] = new RequestData(uuid, handle, responseStyle);

                if (!requests[uuid].Set) return false;

                ProcessRemovals();
                return true;
            }
        }

        public bool RegisterMessageWrapper(Guid uuid, MessageWrapper wrapper)
        {
            lock (requests)
            {
                if (requests.ContainsKey(uuid))
                    requests[uuid].SetWrapperValue(wrapper);
                else
                    requests[uuid] = new RequestData(uuid, wrapper);

                if (!requests[uuid].Set) return false;

                ProcessRemovals();
                return true;
            }
        }

        private void ProcessRemovals()
        {
            Guid[] removals = requests.Where(kvp => kvp.Value.Set || kvp.Value.Expired).Select(kvp => kvp.Key).ToArray();
            foreach (Guid guid in removals)
                requests.Remove(guid);
        }

        internal class RequestData
        {
            internal Guid Uuid;
            private readonly DateTime createdAt = DateTime.UtcNow;
            internal bool Set;
            internal bool Expired => DateTime.UtcNow - createdAt > TimeSpan.FromMinutes(1);

            private DataWaitHandle handle;
            private MessageWrapper wrapper;

            internal void SetHandleValue(DataWaitHandle value)
            {
                    handle = value;
                    if (wrapper == null) return;

                    SetHandle();
            }

            internal void SetWrapperValue(MessageWrapper value)
            {
                    wrapper = value;
                    if (handle == null) return;

                    SetHandle();
            }

            public ResponseStyle ResponseStyle { get; set; }

            private void SetHandle()
            {
                Set = handle.Set(
                    ResponseStyle == ResponseStyle.WholeSolution ?
                    wrapper.SolutionJson : wrapper.FirstOrDefaultJson, wrapper.ErrorsJson);
            }

            public RequestData(Guid uuid, DataWaitHandle handle, ResponseStyle responseStyle)
            {
                Uuid = uuid;
                SetHandleValue(handle);
                ResponseStyle = responseStyle;
            }

            public RequestData(Guid uuid, MessageWrapper wrapper)
            {
                Uuid = uuid;
                SetWrapperValue(wrapper);
            }
        }
    }
}