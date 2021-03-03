using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Parser;
using Parser.ExpressionParser;
using Parser.FlowParser;
using Parser.FlowParser.ActionExecutors;

namespace TestProject
{
    public class UnitTest
    {
        private ServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFlowRunner();

            serviceCollection.Configure<FlowSettings>(x => { x.FailOnUnknownAction = false; });

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Test]
        public async Task Test()
        {
            // Arrange
            const string firstname = "John";
            const string lastname = "Doe";
            var fullname = $"{firstname} {lastname}";
            var expectedNoteSubject = new ValueContainer("Greet our new Contact");
            var expectedNoteText = new ValueContainer($"With the name of {fullname}");

            var flowPath = new Uri(System.IO.Path.GetFullPath(@"flows/2752dde1-2bb2-4e63-9273-a4f82de375f2.json"));

            var flowRunner = _serviceProvider.GetRequiredService<IFlowRunner>();
            flowRunner.InitializeFlowRunner(flowPath.AbsolutePath);

            // Act
            var flowReport = await flowRunner.Trigger(new ValueContainer(new Dictionary<string, ValueContainer>
            {
                {
                    "body", new ValueContainer(new Dictionary<string, ValueContainer>
                    {
                        {"contactid", new ValueContainer(Guid.NewGuid())},
                        {"fullname", new ValueContainer(fullname)},
                        {"lastname", new ValueContainer(lastname)}
                    })
                }
            }));

            // Assert
            // Action is expected to have been executed
            Assert.IsTrue(flowReport.ActionStates.ContainsKey("Create_a_new_row_-_Create_greeting_note"));

            // Action is expected to not have been executed
            Assert.IsFalse(flowReport.ActionStates.ContainsKey("Send_me_an_email_notification"));

            // Checking action input parameters
            var greetingCardItems = flowReport.ActionStates["Create_a_new_row_-_Create_greeting_note"]
                .ActionInput?["parameters"]?["item"];
            Assert.IsNotNull(greetingCardItems);
            Assert.AreEqual(expectedNoteSubject, greetingCardItems["subject"]);
            Assert.AreEqual(expectedNoteText, greetingCardItems["notetext"]);
        }

        public class CreateGreetingNote : OpenApiConnectionActionExecutorBase
        {
            public CreateGreetingNote(IExpressionEngine expressionEngine) : base(expressionEngine)
            {
            }

            public override Task<ActionResult> Execute()
            {
                var guid = Guid.NewGuid();
                var subject = Inputs["paramters"]["subject"];
                var text = Parameters["text"]; // Parameters is equivalent to Inputs["parameters"] 

                return Task.FromResult(new ActionResult
                {
                    ActionOutput = new ValueContainer(new Dictionary<string, ValueContainer>
                    {
                        {"body/annotationid", new ValueContainer(guid.ToString())},
                        {"body/subject", new ValueContainer(subject)},
                        {"body/notetext", new ValueContainer(text)}
                    })
                });
            }
        }

        public class SendEmailNotification : DefaultBaseActionExecutor
        {
            public const string FlowName = "Send_me_an_email_notification";

            public override Task<ActionResult> Execute()
            {
                return Task.FromResult(new ActionResult());
            }
        }
    }
}