using System;
using System.Linq;
using NLog.Targets;
using Xunit;

namespace NLog_OpenTracing_Tests
{
    public class NLogOpenTracingTests
    {
        [Fact]
        public void NLogOpenTracingRuntime()
        {
            var tracer = new OpenTracing.Mock.MockTracer();
            var logFactory = new NLog.LogFactory();
            var logConfig = new NLog.Config.LoggingConfiguration(logFactory);
            var logTarget = new NLog.Targets.OpenTracingTarget(tracer);
            logConfig.AddRuleForAllLevels(logTarget);
            logFactory.Configuration = logConfig;

            using (var parentScope = tracer.BuildSpan("Parent").StartActive(finishSpanOnDispose: true))
            {
                var logger = logFactory.GetCurrentClassLogger();
                logger.Info("Hello World");
            }

            var spans = tracer.FinishedSpans();
            Assert.Single(spans);
            var logEntries = spans.First().LogEntries;
            Assert.Single(logEntries);
            Assert.Equal("Hello World", logEntries.First().Fields["message"]);
        }

        [Fact]
        public void NLogOpenTracingFromConfig()
        {
            var logFactory = new NLog.LogFactory();
            var nlogConfig = @"<nlog>
<targets>
<target name='openTracing' type='openTracing'>
   <contextproperty name='threadid' layout='${threadid}' />
</target>
</targets>
<rules>
<logger name='*' writeTo='openTracing' />
</rules>
</nlog>";
            System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(new System.IO.StringReader(nlogConfig));
            var logConfig = new NLog.Config.XmlLoggingConfiguration(xmlReader, null, logFactory);
            logFactory.Configuration = logConfig;

            var tracer = new OpenTracing.Mock.MockTracer();
            OpenTracing.Util.GlobalTracer.RegisterIfAbsent(tracer);

            using (var parentScope = tracer.BuildSpan("Parent").StartActive(finishSpanOnDispose: true))
            {
                var logger = logFactory.GetCurrentClassLogger();
                logger.Info("Hello World");
            }

            var spans = tracer.FinishedSpans();
            Assert.Single(spans);
            var logEntries = spans.First().LogEntries;
            Assert.Single(logEntries);
            Assert.Equal("Hello World", logEntries.First().Fields["message"]);
        }
    }
}
