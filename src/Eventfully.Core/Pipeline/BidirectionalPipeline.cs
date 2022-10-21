using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Eventfully.Filters
{
    /// <summary>
    /// </summary>
    /// <typeparam name="INPUT"></typeparam>
    /// <typeparam name="OUTPUT"></typeparam>
    public interface IBidirectionalPipeline<INPUT, OUTPUT> : IBidirectionalPipelineStep<INPUT, OUTPUT>
    {
        new OUTPUT OnIncoming(INPUT input);
        new INPUT OnOutgoing(OUTPUT input);
    }

    public interface IBidirectionalPipeline<INPUTOUTPUT> : IBidirectionalPipeline<INPUTOUTPUT, INPUTOUTPUT>, IBidirectionalPipelineStep<INPUTOUTPUT>
    {
        new INPUTOUTPUT OnIncoming(INPUTOUTPUT input);
        new INPUTOUTPUT OnOutgoing(INPUTOUTPUT input);
    }

    public interface IBidirectionalPipelineStep<INPUT, OUTPUT>
    {
        OUTPUT OnIncoming(INPUT input);
        INPUT OnOutgoing(OUTPUT input);
    }

    public interface IBidirectionalPipelineStep<INPUTOUTPUT> : IBidirectionalPipelineStep<INPUTOUTPUT, INPUTOUTPUT>
    {
        new INPUTOUTPUT OnIncoming(INPUTOUTPUT input);
        new INPUTOUTPUT OnOutgoing(INPUTOUTPUT input);  
    }

    public class BidirectionalPipeline<INPUT, OUTPUT> : IBidirectionalPipeline<INPUT, OUTPUT>
    {
        protected Func<INPUT, OUTPUT> InSteps { get; set; }
        protected Func<OUTPUT, INPUT> OutSteps { get; set; } //reverse

        public BidirectionalPipeline() { }

        public OUTPUT OnIncoming(INPUT input) => InSteps.Invoke(input);
        
        public INPUT OnOutgoing( OUTPUT input) => OutSteps.Invoke(input);
    }

    public class BidirectionalPipeline<INPUTOUTPUT> : BidirectionalPipeline<INPUTOUTPUT, INPUTOUTPUT>, IBidirectionalPipeline<INPUTOUTPUT>
    {
        public BidirectionalPipeline() { }
        public BidirectionalPipeline(IEnumerable<IBidirectionalPipelineStep<INPUTOUTPUT>> steps)
        {

            this.InSteps = io =>
            {
                if (steps == null)
                    return io;
                foreach (var step in steps)
                    io = io.In(step);
                return io;
            };
            this.OutSteps = io =>
            {
                if (steps == null)
                    return io;
                foreach (var step in steps.Reverse())
                    io = io.Out(step);
                return io;
            };
        }
    }

   
    public static class BidirectionalPipelineExtensions
    {
      
        public static OUTPUT In<INPUT, OUTPUT>(this INPUT input, IBidirectionalPipelineStep<INPUT, OUTPUT> step)
        {
            return step.OnIncoming(input);
        }
        public static INPUTOUTPUT In<INPUTOUTPUT>(this INPUTOUTPUT input, IBidirectionalPipelineStep<INPUTOUTPUT> step)
        {
            if (step == null)
                return input;
            return step.OnIncoming(input);
        }
        public static INPUT Out<INPUT,OUTPUT>(this OUTPUT input, IBidirectionalPipelineStep<INPUT, OUTPUT> step)
        {
            return step.OnOutgoing(input);
        }
        public static INPUTOUTPUT Out<INPUTOUTPUT>(this INPUTOUTPUT input, IBidirectionalPipelineStep<INPUTOUTPUT> step)
        {
            if (step == null)
                return input;
            return step.OnOutgoing(input);
        }
    }


}
