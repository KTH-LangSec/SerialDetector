using System;

namespace SerialDetector.Experiments
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetUpAttribute : Attribute
    {
        public SetUpAttribute(string sensitiveSink, 
            uint virtualCallsLimit = 20, 
            bool enableStaticFields = false)
        {
            SensitiveSink = sensitiveSink;
            VirtualCallsLimit = virtualCallsLimit;
            EnableStaticFields = enableStaticFields;
        }
        
        public string SensitiveSink { get; }
        public uint VirtualCallsLimit { get; }
        public bool EnableStaticFields { get; }
    }
}