namespace SerialDetector.KnowledgeBase
{
    public abstract class TemplateBase
    {
        public void Initialize(Context context)
        {
            Template = new Template(context);
        }
        
        private protected Template Template { get; private set; }
    }
}