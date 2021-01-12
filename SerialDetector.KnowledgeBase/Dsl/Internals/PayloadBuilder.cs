namespace SerialDetector.KnowledgeBase.Internals
{
    internal class PayloadBuilder<TGadget>
        where TGadget : IGadget, new()
    {
        private readonly Context context;

        public PayloadBuilder(Context context)
        {
            this.context = context;
        }

        public Payload Format<TFormatter>()
            where TFormatter : IFormatter, new()
        {
            var gadget = new TGadget();
            var formatter = new TFormatter();
            var payload = formatter.GeneratePayload(gadget.Build(context.PayloadCommand));
            
            bool interrupt = false;    // we can use it for generation payload from the commandline w/o testing    
            context.RaisePayloadGenerationCompleted(
                PayloadGenerationMode.GadgetBased,
                payload, 
                ref interrupt);

            return payload;
        }
    }
}