using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Rook.Framework.Core.Application.Subscribe
{
    public interface IMessageSubscriber
    {
	    Task ConsumeMessage(object sender, BasicDeliverEventArgs eventDetails);
        int GetInProgressMessageCount();
	}
}
