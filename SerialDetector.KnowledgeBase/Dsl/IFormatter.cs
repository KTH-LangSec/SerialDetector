namespace SerialDetector.KnowledgeBase
{
    internal interface IFormatter
    {
        Payload GeneratePayload(object gadget);
    }
}