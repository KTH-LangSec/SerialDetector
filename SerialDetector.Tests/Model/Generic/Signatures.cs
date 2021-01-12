using System;
using System.Collections.Generic;

namespace SerialDetector.Tests.Model.Generic
{
    internal sealed class Signatures
    {
        public static void A(int a, int b)
        {
        }

        public static void B<T, M>(IEnumerable<T> a, int b)
        {
            
        }

        public static void C<T, M>(IEnumerable<M> a, int b)
        {
        }

        public static void D<T, M>(IEnumerable<M> a, IEnumerable<T> b)
        {
        }

        public static void E<T, M>(IEnumerable<T> a, IEnumerable<M> b)
        {
        }

        public static void F<T, M>(Dictionary<T,M> a, Dictionary<M,T> b)
        {
        }

        public static void G<T, M>(IEnumerable<string> a)
        {
        }

        public static void H<T, M>(IEnumerable<int> a)
        {
        }

        public static void I<M>(IEnumerable<IEnumerable<M>> a)
        {
        }
        
        public static void J<T, M, R>(R r, M m, T t) 
            where T:IEnumerable<M>
        {
        }


        public class MyClass<T>
        {
            public static M A<M>(int a)
            {
                return default(M);
            }

            public static void B(int a)
            {
            }
            
            public M C<M>(T a, M b)
            {
                return default(M);
            }
        }

        public static void CallE()
        {
            E(new List<string>(), new List<int>());
        }
        
        public static void CallE2()
        {
            IEnumerable<IEnumerable<int>> a = new List<IEnumerable<int>>();
            E(a, new List<int>());
        }


        public static void CallMyClassC()
        {
            var a = new MyClass<string>();
            a.C<long>("", 1);
        }
    }
}