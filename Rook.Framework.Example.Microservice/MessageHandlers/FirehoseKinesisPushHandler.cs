using System;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Example.Microservice.Mongo;
using Rook.Framework.Example.Microservice.Objects;

namespace Rook.Framework.Example.Microservice.MessageHandlers
{
	[Handler("FirehoseKinesisTest", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution)]
	public class FirehoseKinesisPushHandler : IMessageHandler2<FirehoseDataSampleNeed, bool>
	{
		private readonly ITestRepository _testRepository;

		public FirehoseKinesisPushHandler(ITestRepository testRepository)
		{
			_testRepository = testRepository;
		}

		public CompletionAction Handle(Message<FirehoseDataSampleNeed, bool> message)
		{
			message.Solution = new[] {true};

			_testRepository.Put(message.Need.ToFirehoseDataSample());

			return CompletionAction.Republish;
		}
	}

	public class FirehoseDataSampleNeed
	{
		public int IdInt { get; set; }
		
		
		public string Name { get; set; }

		public FirehoseDataSample ToFirehoseDataSample()
		{
			return new FirehoseDataSample()
			{
				IdInt = IdInt,
				Name = Name
			};
		}
	}
}