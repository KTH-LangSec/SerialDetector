// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
#pragma warning disable 649

namespace SerialDetector.Tests.Model.MethodBody
{
    internal sealed class TaintedSamples
    {
        private interface IInterfaceWithoutImpl
        {
            bool TryGetValue(string key, out string value);
        }
        
        public void InputTaintedSuccess(string arg)
        {
            var v = arg;
            var t = CreateTaintedObject(v);
            SideEffect(t);
        }

        private string[] array;
        public void InputTaintedByArraySuccess()
        {
            InputTaintedByArrayInternal();
        }
        
        public void InputTaintedByArrayInternal()
        {
            var t = CreateTaintedObject(array[0]);
            SideEffect(t);
        }

        private string field;
        public void InputTaintedFieldSuccess()
        {
            var t = CTOCallInternal(field);
            SideEffect(t);
        }
        
        public void InputTaintedFieldByCallSuccess()
        {
            GetField(out var v);
            var t = CTOCallInternal(v);
            SideEffect(t);
        }

        private void GetField(out string v)
        {
            v = field;
        }
        
        public void InputTaintedByExternalCallSuccess(string arg)
        {
            var v = arg.Replace("<", "");
            var t = CreateTaintedObject(v);
            SideEffect(t);
        }
        
        public void InputTaintedByExternal2CallsSuccess(string arg)
        {
            InputTaintedByExternalCallSuccess(arg);
        }
        
        public void InputTaintedByCallSuccess(string arg)
        {
            InputTaintedSuccess(arg);
        }

        public void InputTaintedBy2CallsSuccess(string arg)
        {
            var t = CTOCallInternal(arg);
            SECallInternal(t);
        }

        private Dictionary<string, string> dictionary;
        public void InputTaintedByDictionaryOutParamSuccess()
        {
            if (this.dictionary.TryGetValue("CONST", out var v))
            {
                var t = CreateTaintedObject(v);
                SideEffect(t);
            }
        }
        
        private IInterfaceWithoutImpl pureInterface;
        public void InputTaintedByOutParamSuccess()
        {
            if (pureInterface.TryGetValue("CONST", out var v))
            {
                var t = CreateTaintedObject(v);
                SideEffect(t);
            }
        }

        public void InputTaintedByOutParamAndCallSuccess()
        {
            InputTaintedByOutParamSuccess();
        }
        
        public void InputTaintedFail()
        {
            var t = CreateTaintedObject(typeof(Jumps).ToString());
            SideEffect(t);
        }
        
        public void InputTaintedByCallFail()
        {
            var t = CTOCallInternal(typeof(Jumps).ToString());
            SECallInternal(t);
        }

        public void UnsafeArrayCopyTaintedSuccess(int[] arg)
        {
            var v = new int[arg.Length];
            UnsafeArrayCopy(arg, v);
            var t = CreateTaintedObject(v[0]);
            SideEffect(t);
        }

        private unsafe void UnsafeArrayCopy(int[] source, int[] target)
        {
            fixed (int* pSource = source, pTarget = target)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    pTarget[i] = pSource[i];
                }
            }            
        }

        public void UnsafeArrayCopyTaintedSuccess2(byte[] arg)
        {
            var v = new byte[arg.Length];
            UnsafeArrayCopy2(arg, v);
            var t = CreateTaintedObject(v[0]);
            SideEffect(t);
        }
        
        private unsafe void UnsafeArrayCopy2(byte[] source, byte[] target)
        {
            fixed (byte* pSource = source, pTarget = target)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    *(pTarget - i) = *(pSource - i);
                }
            }            
        }
        
        public void UnsafeStringCopyTaintedSuccess(string arg)
        {
            var copyArg = UnsafeStringCopy(arg);
            var t = CTOCallInternal(copyArg);
            SideEffect(t);
        }
        
        private unsafe string UnsafeStringCopy(string str)
        {
            var newStr = new String(' ', 10);
            fixed (char* pSource = str, pTarget = newStr)
            {
                for (int i = 0; i < 10 && i < str.Length; i++)
                {
                    *(pTarget + i) = *(pSource + i);
                }
            }

            return newStr;
        }
        
        public void UnsafeStringCopy2TaintedSuccess(string arg)
        {
            var copyArg = UnsafeStringCopy2(arg);
            var t = CreateTaintedObject(copyArg);
            SideEffect(t);
        }
        
        private unsafe string UnsafeStringCopy2(string str)
        {
            var a = new char[10]; //new String(' ', 10);
            fixed (char* pSource = str, pTarget = a)
            {
                __Memmove((byte*) pTarget, (byte*) pSource, 10);
            }

            return new String(a);
        }
        
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("QCall", CharSet = CharSet.Unicode)]
        private static extern unsafe void __Memmove(byte* dest, byte* src, ulong len);

        public void ToStringSuccess(string arg)
        {
            var t = CreateTaintedObject(arg.ToString());
            SideEffect(t);
        }
        
        public void StringBuilderTaintedSuccess(string arg)
        {
            var sb = new StringBuilder(arg);
            
            var t = CreateTaintedObject(sb.ToString());
            SideEffect(t);
        }
        
        public void FileTextTaintedSuccess(string arg)
        {
            //var t = CreateTaintedObject(File.ReadAllText("some-file.txt"));
            var t = CreateTaintedObject(File.ReadAllText(arg));
            SideEffect(t);
        }
        
        private object CTOCallInternal(object o)
        {
            return CreateTaintedObject(o);
        }

        private void SECallInternal(object o)
        {
            SideEffect(o);
        }
        
        private static object CreateTaintedObject(object obj) => new object();
        
        private static void SideEffect(object obj) {}
    }
}