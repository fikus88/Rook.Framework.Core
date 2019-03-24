# Rook.Framework.Core
Common Rook micro functionality

[![Build status](https://travis-ci.org/rookframework/Rook.Framework.Core.svg?branch=master)](https://travis-ci.org/rookframework/Rook.Framework.Core)
[![nuget](https://img.shields.io/nuget/v/Rook.Framework.Core.svg)](https://www.nuget.org/packages/Rook.Framework.Core/)

## Creating a new Rook Service
  
Begin by creating a new netcore console application, and including the Rook.Framework.Core Nuget Package. 

Open the Program.cs class and add the following using statment

    using Rook.Framework.Core.Common;
    using Rook.Framework.Core.Services;
    using Rook.Framework.Core.StructureMap;

Modify the main method

    private static void Main()
        {
            
            var container = Bootstrapper.Init();
            IService instance = container.GetInstance<IService>();

            Thread.CurrentThread.Name = $"{ServiceInfo.Name} Main Thread";

            instance.Start();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => instance.Stop();

            Thread.CurrentThread.IsBackground = true;
            
            while (true)
                Thread.Sleep(int.MaxValue);
            
        }
        
You can now focus on implimenting a handler for the message you are interested in. Create a new class for your handler inheriting the IMessageHandler interface, and decorate it with the Activiy Handler attribute

    [Handler("MyActivity", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution, ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist)]
    public class MyActivity : IMessageHandler<MyNeed, MySolution>
    {
    ....
    }

When you inherit the IMessageHandler interface you must pass it two POCO objects that represent the Need your service expects to recive and the Solution yours service will respond with. Feel free to create these objects. Here is an example 

    public class MyNeed
    {
        public Guid? MyId { get; set; }
        public Guid ParentId { get; set; }
        public SomeType SomeProperty { get; set; }
    }

You do not need to worry about the message meta data such as the correlationID etc. Just focus on your objects.   

Now you have your need and solution, and you activity to handle them, it is time to impliment the handle method of the IMessageHandler interface

     public CompletionAction Handle(Message<MyNeed, MySolution> message)
        {
            _logger.Debug(nameof(QueryRelationshipsMessageHandler), new LogItem("Event", nameof(Handle)), new LogItem("MessageId", message.Uuid.ToString()));
            
            try
            {

                if (ValidateMessage(message))
                {
                    var updatedMetadata = ProcessMessage(message); //call to a nother method to do stuff

                    if (updatedMetadata != null)
                    {
                        message.Solution = new[] {updatedMetadata};
                    }
                }
            }
            
            catch (Exception ex)
            {
                _logger.Exception(MyActivity, "Exception caught", ex, new LogItem("MessageId", message.Uuid.ToString()));
                message.Errors.Add(new ResponseError {Type = ResponseError.ErrorType.Server});
            }

            return CompletionAction.Republish;
        }
        
And that is about it in terms of code examples. You can of course impliment many message handlers in a single microservice, and it is important to include a docker file to build the image for your service. makesure the dockerfile is copied to the output directory on build 

    FROM microsoft/dotnet
    COPY . /app
    WORKDIR /app
    ENTRYPOINT ["dotnet", "Rook.Platform.MyService.dll"]

