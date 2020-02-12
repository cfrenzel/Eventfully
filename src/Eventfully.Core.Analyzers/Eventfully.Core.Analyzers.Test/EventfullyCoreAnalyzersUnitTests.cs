using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Eventfully.Core.Analyzers;

namespace Eventfully.Core.Analyzers.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void Should_be_no_diagnostics_by_default()
        {
            var test = @"";
            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void Should_find_events_missing_mapping_for_saga()
        {
            var test = 
     @"public class PizzaFulfillmentProcess : ProcessManagerMachine<PizzaFulfillmentStatus, Guid>,
       ITriggeredBy<PizzaOrderedEvent>,
       IMachineMessageHandler<PizzaPaidForEvent>,
       IMachineMessageHandler<PizzaPreparedEvent>,
       IMachineMessageHandler<PizzaDeliveredEvent>
       {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PizzaFulfillmentProcess> _log;
        private readonly IMessagingClient _messageClient;
          
        public PizzaFulfillmentProcess(ApplicationDbContext db, IMessagingClient messageClient, ILogger<PizzaFulfillmentProcess> log)
        {
            _db = db;
            _log = log;
            _messageClient = messageClient;
                  
            this.MapIdFor<PizzaPaidForEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaPreparedEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaDeliveredEvent>((m, md) => m.OrderId);
        }
        }";
       
            var expected = new DiagnosticResult
            {
                Id = "EventfullyCoreAnalyzers",
                Message = "Missing this.MapIdFor<PizzaOrderedEvent> in constructor",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 2, 8)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest =
     @"public class PizzaFulfillmentProcess : ProcessManagerMachine<PizzaFulfillmentStatus, Guid>,
       ITriggeredBy<PizzaOrderedEvent>,
       IMachineMessageHandler<PizzaPaidForEvent>,
       IMachineMessageHandler<PizzaPreparedEvent>,
       IMachineMessageHandler<PizzaDeliveredEvent>
       {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PizzaFulfillmentProcess> _log;
        private readonly IMessagingClient _messageClient;
          
        public PizzaFulfillmentProcess(ApplicationDbContext db, IMessagingClient messageClient, ILogger<PizzaFulfillmentProcess> log)
        {
            _db = db;
            _log = log;
            _messageClient = messageClient;
                  
            this.MapIdFor<PizzaPaidForEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaPreparedEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaDeliveredEvent>((m, md) => m.OrderId);
            this.MapIdFor<PizzaOrderedEvent>((m, md) => m.);
        }
        }";
            VerifyCSharpFix(test, fixtest);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void Should_find_events_mapped_but_not_handled()
        {
            throw new NotImplementedException();
        }
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EventfullyCoreAnalyzersCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EventfullyCoreAnalyzersAnalyzer();
        }
    }
}
