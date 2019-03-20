using System.Timers;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;

namespace Rook.Framework.Core.Application.Scaling
{
    internal class ScalingMessagePublisher : IStartStoppable
    {
        private readonly IQueueWrapper _queueWrapper;
        private readonly IConfigurationManager configurationManager;
        private bool _scalingDownMessageSent;

        private string _fqdsn;
        //_fqdsn is Fully qulified docker service name which is pipeline_env_stackname_servicename (Example:- blue_qa_relationshipserviceapi_relationshipserviceapi)
        private int _maxmessageload;
        private int _minmessageload;
        public StartupPriority StartupPriority => StartupPriority.Lowest;
        public Timer Timer { get; } = new Timer(5000);
        
        public ScalingMessagePublisher(
            IQueueWrapper queueWrapper,
            IConfigurationManager configurationManager)
        {
            _queueWrapper = queueWrapper;
            this.configurationManager = configurationManager;

        }
        public void Start()
        {
            _fqdsn = configurationManager.Get("FQDSN", "");
            _maxmessageload = configurationManager.Get("MAXMESSAGELOAD", 5);
            _minmessageload = configurationManager.Get("MINMESSAGELOAD", 1);

            if (_fqdsn != string.Empty)
            {
                Timer.Elapsed += LoadBalancer;
                Timer.AutoReset = true;
                Timer.Enabled = true;
            }

        }
        public void Stop()
        {
            Timer.Stop();
        }

        private void LoadBalancer(object sender, ElapsedEventArgs e)
        {
            var messagesToPublish = new Message<object, ScalingSolution>();
            ScalingNeed need = new ScalingNeed { ServiceName = _fqdsn };
            messagesToPublish.Need = need;

            if (_queueWrapper.MessageCount(ServiceInfo.QueueName) > _maxmessageload)
            {
                messagesToPublish.Method = "ScaleUp";
                _scalingDownMessageSent = false;
                _queueWrapper.PublishMessage(messagesToPublish);
            }
            else if (_queueWrapper.MessageCount(ServiceInfo.QueueName) <= _minmessageload)
            {
                if (!_scalingDownMessageSent)
                {
                    messagesToPublish.Method = "ScaleDown";
                    _scalingDownMessageSent = true;
                    _queueWrapper.PublishMessage(messagesToPublish);
                }
            }
        }
    }

}

