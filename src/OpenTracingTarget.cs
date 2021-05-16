namespace NLog.Targets
{
    [Target("OpenTracing")]
    public class OpenTracingTarget : TargetWithContext
    {
        private readonly OpenTracing.ITracer _tracer;

        public bool SetTagErrorOnException { get; set; }

        public OpenTracingTarget()
            :this(null)
        {
        }

        public OpenTracingTarget(OpenTracing.ITracer tracer)
        {
            _tracer = tracer ?? OpenTracing.Util.GlobalTracer.Instance;
            ContextProperties.Add(new TargetPropertyWithContext("component", "${logger}"));
            ContextProperties.Add(new TargetPropertyWithContext("level", "${level}"));
            ContextProperties.Add(new TargetPropertyWithContext("message", "${message}"));
            ContextProperties.Add(new TargetPropertyWithContext("event", "${event-properties:EventId_Name}") { IncludeEmptyValue = false });
            ContextProperties.Add(new TargetPropertyWithContext("eventid", "${event-properties:EventId_Id}") { IncludeEmptyValue = false });
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var span = _tracer.ActiveSpan;
            if (span == null)
                return;

            if (SetTagErrorOnException && logEvent.Exception != null)
            {
                span.SetTag(OpenTracing.Tag.Tags.Error, true);
            }

            var fields = GetAllProperties(logEvent);
            if (logEvent.Exception != null)
            {
                fields[OpenTracing.LogFields.ErrorKind] = logEvent.Exception.GetType().ToString();
                fields[OpenTracing.LogFields.ErrorObject] = logEvent.Exception;
            }

            span.Log(fields);
        }
    }
}
