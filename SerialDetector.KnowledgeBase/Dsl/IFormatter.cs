namespace SerialDetector.KnowledgeBase
{
    public interface IFormatter
    {
        Payload GeneratePayload(object gadget);
    }
}