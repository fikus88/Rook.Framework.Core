using Rook.Framework.Core.Backplane;

namespace Rook.Framework.Core
{
    public class RequestMethodRegistrationConsumer : BackplaneConsumer<RequestMethodRegistration>
    {
        private readonly IRequestStore _requestStore;

        public RequestMethodRegistrationConsumer(IRequestStore requestStore)
        {
            _requestStore = requestStore;
        }

        public override void Consume(RequestMethodRegistration value)
        {
            if (!_requestStore.Methods.Contains(value.Method))
                _requestStore.Methods.Add(value.Method);
        }
    }
}