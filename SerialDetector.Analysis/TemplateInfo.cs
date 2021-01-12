using System;

namespace SerialDetector.Analysis
{
    public class TemplateInfo
    {
        public TemplateInfo(MethodUniqueSignature method, Version requiredOlderVersion)
        {
            Method = method;
            RequiredOlderVersion = requiredOlderVersion;
        }

        public MethodUniqueSignature Method { get; }
        public Version RequiredOlderVersion { get; }

        public override string ToString() => $"{Method}, v{RequiredOlderVersion}";
    }
}