using System.IO;
using SerialDetector.KnowledgeBase.Internals;

namespace SerialDetector.KnowledgeBase
{
    internal class It
    {
        private readonly Context context;

        public It(Context context)
        {
            this.context = context;
        }
        
        public PayloadBuilder<TGadget> IsPayloadOf<TGadget>()
            where TGadget : IGadget, new()
        {
            return new PayloadBuilder<TGadget>(context);
        }
    
        public Payload IsPayloadFrom(string fileName)
        {
            var payload = Payload.FromFile(fileName, context.PayloadCommand);

            bool interrupt = false;    // we can use it for generation payload from the commandline w/o testing    
            context.RaisePayloadGenerationCompleted(
                PayloadGenerationMode.PayloadFileBased,
                payload, 
                ref interrupt);

            return payload;
        }
    }
}