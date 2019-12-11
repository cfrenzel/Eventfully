using System;
using System.Collections.Generic;
using System.Text;

namespace Eventfully.Filters
{
    /// <summary>
    /// Inspired by Jeremy Davis
    /// https://jermdavis.wordpress.com/2016/10/03/an-alternative-approach-to-pipelines/
    /// </summary>
    /// <typeparam name="INPUT"></typeparam>
    /// <typeparam name="OUTPUT"></typeparam>
    public interface IPipeline<INPUT, OUTPUT> : IPipelineStep<INPUT, OUTPUT>
    {
        new OUTPUT Process(INPUT input);
    }

    public interface IPipeline<INPUTOUTPUT> : IPipeline<INPUTOUTPUT, INPUTOUTPUT>, IPipelineStep<INPUTOUTPUT>
    {
        new INPUTOUTPUT Process(INPUTOUTPUT input);
    }

    public interface IPipelineStep<INPUT, OUTPUT>
    {
        OUTPUT Process(INPUT input);
    }

    public interface IPipelineStep<INPUTOUTPUT> : IPipelineStep<INPUTOUTPUT, INPUTOUTPUT>
    {
        new INPUTOUTPUT Process(INPUTOUTPUT input);
    }

    public class Pipeline<INPUT, OUTPUT> : IPipeline<INPUT, OUTPUT>
    {
        protected Func<INPUT, OUTPUT> Steps { get; set; }

        public Pipeline() { }

        public OUTPUT Process(INPUT input)
        {
            return Steps.Invoke(input);
        }
    }

    public class Pipeline<INPUTOUTPUT> : Pipeline<INPUTOUTPUT, INPUTOUTPUT>, IPipeline<INPUTOUTPUT>
    {
        public Pipeline() { }
        public Pipeline(IEnumerable<IPipelineStep<INPUTOUTPUT>> steps)
        {
            this.Steps = io =>
            {
                foreach (var step in steps)
                    io = io.Step(step);
                return io;
            };
        }
    }

    public static class PipelineExtensions
    {
        public static OUTPUT Step<INPUT, OUTPUT>(this INPUT input, IPipelineStep<INPUT, OUTPUT> step)
        {
            return step.Process(input);
        }
        public static INPUTOUTPUT Step<INPUTOUTPUT>(this INPUTOUTPUT input, IPipelineStep<INPUTOUTPUT> step)
        {
            if (step == null)
                return input;
            return step.Process(input);
        }
    }


}
