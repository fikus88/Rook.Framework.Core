using System;
using Newtonsoft.Json;
using Rook.Framework.Core.AmazonKinesisFirehose;
using Rook.Framework.Core.AnalyticsPump;
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

			_testRepository.Put(new FirehoseDataSample()
			{
				Id = Guid.NewGuid(),
				Number = new Random().Next(1, int.MaxValue),
				Word = Guid.NewGuid().ToString()
			});
			
			return CompletionAction.Republish;
		}
	}

	public class FirehoseDataSampleNeed
	{
		public int Number { get; set; }
		public string Word { get; set; }
	}
}