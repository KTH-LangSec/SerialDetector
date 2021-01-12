using System.Collections.Generic;
using SerialDetector.Analysis;

namespace SerialDetector
{
    public class TemplateGroup
    {
        public TemplateGroup(string name, List<TemplateInfo> templates)
        {
            Name = name;
            Templates = templates;
        }

        public string Name { get; }
        public List<TemplateInfo> Templates { get; }
    }
}