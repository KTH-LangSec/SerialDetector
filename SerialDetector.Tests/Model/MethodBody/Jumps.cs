using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework.Internal;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace SerialDetector.Tests.Model.MethodBody
{
    internal sealed class Jumps
    {
        internal class MyClass
        {
            public int field;

            public string bar;

            public MyClass cl;
            
            public MyClass cl2;

            public object obj;
        }

        private MyClass myClass;
        private int field;
        private static int staticField;
        private static MyClass myClassStatic;

        public void Aaa(MyClass x, MyClass tainedObj)
        {
            Fff(x, x, tainedObj, true, true);
            //x.cl (tainedObj)
        }

        public object RecursionMaterializingField()
        {
            MyClass v = null;
            RecursionMaterializingField(ref v);
            SideEffect(v.obj);
            return v;
        }

        private void RecursionMaterializingField(ref MyClass c)
        {
            c = myClass;
            c = c.cl;
            c.cl.obj = new StringBuilder();
        }
        
        public void FewSSCalls()
        {
            var a = ReturnSS();
            MiddleFunc();
            MiddleFunc2();
            var c = new StringBuilder();
            SideEffect(a);
        }
        
        private object ReturnSS() => new StringBuilder();

        private void MiddleFunc() => ReturnSS();

        private void MiddleFunc2() => MiddleFunc();
        

       
        public void Fff(MyClass a, MyClass b, MyClass tainedObj, bool cond1, bool cond)
        {
            a.cl = new MyClass();    //(1)
            //if (cond)
            b.cl = tainedObj;        //(2)
            a.cl = new MyClass();    //(3)
            
            if (cond1)
                b.cl = tainedObj;        //(?)

            
            if(!cond)
                a.cl = new MyClass();    //(3)
            else
                a.cl = new MyClass();    //(3)
        }
        
        public void AccessPath(MyClass o)
        {
            myClass.cl.cl.cl.cl = o;
            myClass.cl.cl.cl = myClass.cl.cl.cl.cl;
            myClassStatic = myClass.cl;
            SideEffect(myClass.cl.cl.cl);
        }
        
        public void MergeArguments(MyClass o, MyClass p2)
        {
            p2.cl.obj = new StringBuilder();
            AliasToMergedArgs(o, p2, new MyClass());
            SideEffect(o.cl.cl.obj);
        }

        public void MergeArguments2(MyClass o, MyClass p2)
        {
            p2.cl.obj = new StringBuilder();
            
            AliasToMergedArgs(o, new MyClass(), p2);
            SideEffect(o.cl.cl.obj);
        }
        
        private void AliasToMergedArgs(MyClass arg1, MyClass arg2, MyClass arg3)
        {
            var a = arg2;
            a = arg3;
            arg1.cl = a;
        }

        public void MergeEntitiesBug()
        {
            var t = new StringBuilder();
            var my1 = new MyClass();
            my1.cl = new MyClass();
            my1.cl.obj = t;
            var my2 = new MyClass();
            var ret = AliasToMergedEntities(my1, my2, new MyClass());
            SideEffect(ret.obj);
            SideEffect(my2.cl.obj);
        }

        private MyClass AliasToMergedEntities(MyClass arg1, MyClass arg2, MyClass arg3)
        {
            var a = arg1.cl;
            arg3.cl = a;    // to materialize arg1.cl 

            var k = arg1.cl2;
            k = arg1.cl;
            arg2.cl = k;    // cl and cl2 will be merged here and updated only for the slot k

            return a;
        }

        public void AccessPathThroughCall(MyClass o, MyClass p2)
        {
            p2.cl.obj = new StringBuilder();
            AccessPathThroughCtor(o, p2);
            SideEffect(o.cl.cl.cl.obj);
        }

        public void AccessPathThroughCtor(MyClass p1, MyClass p2)
        {
            var a = new MyClass();
            a.cl = p2;
            p1.cl = a;
        }

        public void AccessPathThroughCallByStatic(MyClass o, MyClass p2)
        {
            SetTaintValue(p2);
            this.AccessPathByArg(p2);
            this.myClass.cl.cl.cl = this.myClass.cl.cl.cl.cl;
            this.AccessPathByStatic();
            this.AccessPathByStatic();
            //SideEffect(p2.cl.obj);
            SideEffect(myClassStatic.cl.obj);
            //SideEffect(this.myClass.cl.cl.cl.obj);
        }

        private void SetTaintValue(MyClass p1)
        {
            p1.cl.obj = new StringBuilder();
        }

        private void AccessPathByArg(MyClass p1)
        {
            var array = new MyClass[5];
            // we ignore an index number for now 
            array[1] = p1.cl;
            this.myClass.cl.cl.cl.cl = array[0];
        }
        
        private void AccessPathByStatic()
        {
            myClassStatic = this.myClass.cl;
            myClassStatic.cl = myClassStatic.cl.cl;
        }
        
        public void AccessPathRecursion(MyClass o, MyClass p2)
        {
            p2.cl.obj = new StringBuilder();
            this.myClass.cl = p2.cl;
            this.AccessPathRecursion2(p2);
            //this.myClass.cl = p2.cl;
            SideEffect(this.myClass.cl.cl.cl.obj);
        }
        
        private void AccessPathRecursion2(MyClass p1)
        {
            this.myClass.cl.cl.cl = this.myClass.cl;
        }
        
        public void MethodCallWithoutRet(int a, int b, int c)
        {
            myClass.field = c;
            field = a;
            staticField = b;
            var v = 5;
            SideEffect(a, field, v, "MyStr", null, staticField, myClass.field, myClass);
        }

        public void VirtualMethodCall(ICreator creator)
        {
            new StringBuilderCreatorA();
            object obj = creator.CreateTaintedValue("");
            SideEffect(obj);
        }

        public object CallAllocate(RuntimeType type)
        {
            return Allocate(type);
        }
        
        public void IfStatement(int a)
        {
            if (a == 5)
            {
                SideEffect("1");
            }
            else
            {
                SideEffect("2");
            }
            
            SideEffect("3");
        }

        public void Arrays(object a)
        {
            var array = new MyClass[10];
            //array[0] = array[5];
            array[5].obj = a;
            SideEffect(array[0].obj);
        }

        
        public void Aliaces(object a)
        {
            var v1 = a;
            var v2 = v1;
            SideEffect(v2);
        }
        
        public void IfStatementForField(int a, MyClass c1, MyClass c2)
        {
            if (a == 5)
            {
                myClass = c1;
                myClass.bar = "A";
            }
            else
            {
                myClass = c2;
                myClass.bar = "B";
            }
            
            SideEffect(myClass, myClass.bar);
        }
        
        private object f1;
        private object f2;

        public void Bar()
        {
            f2 = new StringBuilder();
            Foo();
            SideEffect(f1);
        }
        
        public void Foo()
        {
            this.f1 = this.f2;
        }

        public object ReturnTaintedCase()
        {
            var l1 = new MyClass {obj = new StringBuilder()};
            var f = Return(l1);
            SideEffect(f);

            var v = Return2(l1.obj);
            SideEffect(v.obj);
            
            var n = Return3(null, l1, true);
            SideEffect(n.obj);

            return f;
        }

        private object Return(MyClass arg) => arg.obj;

        private MyClass Return2(object obj) => new MyClass {obj = obj};

        private MyClass Return3(MyClass obj1, MyClass obj2, bool flag)
        {
            if (flag)
                return obj1;
            
            return obj2;
        }
        
        public void TernaryOperator(int a)
        {
            SideEffect(a == 10 ? "1" : "2");
        }

        public void FieldRewrite()
        {
            SideEffect(myClass, myClass.bar);
            myClass = CreateMyClass();
            SideEffect(myClass, myClass.bar);
        }
        
        public void FieldRewrite2()
        {
            myClass.bar = "A";
            var a = myClass;
            SideEffect(myClass, myClass.bar);
            myClass.bar = "B";
            SideEffect(myClass, a.bar);
        }

        public MyClass CreateMyClass()
        {
            var obj = new MyClass();
            obj.bar = "MyBar";
            return obj;
        }

        public void RecursiveObjectGraph()
        {
            var obj = this.myClass;
            obj.cl = obj;
            obj.obj = new StringBuilder();
            SideEffect(obj.cl.cl.cl.cl.cl.cl.cl.obj);
        }
        
        public void AssignToOutParameter()
        {
            object obj1 = new object();
            AssignToOutParameterImpl(obj1, out var obj2);
        }

        private void AssignToOutParameterImpl(object o, out object value)
        {
            value = new StringBuilder();
            SideEffect(value);
        }
        
        public void ReturnFromOutParameter()
        {
            ReturnFromOutParameterImpl(out var obj);
            SideEffect(obj);
        }

        private void ReturnFromOutParameterImpl(out object value)
        {
            value = new StringBuilder();
        }
        
        public void ReturnFromRefParameter()
        {
            var obj = new StringBuilder();
            ReturnFromRefParameterImpl(ref obj);
            SideEffect(obj);
        }

        private StringBuilder stringBuilderField;
        
        private void ReturnFromRefParameterImpl(ref StringBuilder value)
        {
            value = stringBuilderField;
        }

        public void NotReturnFromParameter()
        {
            object obj = new object();
            NotReturnFromParameterImpl(obj);
            SideEffect(obj);
        }

        private void NotReturnFromParameterImpl(object value)
        {
            value = new StringBuilder();    
        }


        public void RecursiveMergingExecutionContext(object obj)
        {
            var c1 = new MyClass();
            c1.cl.cl = c1;
            
            var c2 = new MyClass();
            c2.cl.cl = c2;


            var c3 = c1;
            c3 = c2;
            
            RecursiveMergingSummary(c3, new object());
        }

        private void RecursiveMergingSummary(MyClass arg1, object arg2)
        {
            arg1.obj = arg2;
        }

        private static object obj1;
        public void RecursiveSummaryApplying(MyClass p)
        {
            obj1 = new object();
        }

        private void RecursiveSummaryCreating(MyClass arg1)
        {
            myClass.cl = arg1;
            myClass.cl.cl = arg1;
        }


        public void SecondSideEffectCallShouldBeTainted()
        {
            var obj = new StringBuilder();
            SideEffectWrapper(obj);
            SideEffectWrapper(new object());
            SideEffectWrapper2(obj);
        }

        private void SideEffectWrapper2(object obj)
        {
            SideEffectWrapper(obj);
        }

        private void SideEffectWrapper(object obj)
        {
            SideEffect(obj);
        }

        public void ForeachBody()
        {
            foreach (var a in new [] {"1", "2"})
            {
                f2 = new StringBuilder();
                SideEffect(f2);
            }

/*
            try
            {
            }
            catch(Exception)
            {
            }
*/
            bar = new MyClass();
        }

        private MyClass foo;
        private MyClass bar;

        private void CreateAlias()
        {
            this.foo = this.bar;
        }

        public void EntryPoint(string input)
        {
            CreateAlias();
            this.bar.obj = CreateObject(input);
            ExternalMethod(this.foo.obj);
        }

        private object CreateObject(string str) => new StringBuilder();

        private void ExternalMethod(object obj) => SideEffect(obj);

        public static void Throw(int errorCode)
        {
            switch (errorCode)
            {
                case 2:
                case 3:
                case 15002:
                case 15007:
                case 15027:
                case 15028:
                    throw new EventLogNotFoundException();
                case 5:
                    throw new UnauthorizedAccessException();
                case 13:
                case 15005:
                    throw new EventLogInvalidDataException();
                case 1223:
                case 1818:
                    throw new OperationCanceledException();
                case 15011:
                case 15012:
                    throw new EventLogReadingException();
                case 15037:
                    throw new EventLogProviderDisabledException();
                default:
                    throw new EventLogException();
            }
        }

        private UriKind m_UriKind;
        
        public object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            Uri uri)
        {
/*
            string uriString = value as string;
            if (uriString != null)
                return (object) new Uri(uriString, this.m_UriKind);
            Uri uri = value as Uri;
            if (uri != (Uri) null)
*/

                return (object) new Uri(
                    uri.OriginalString, 
                    this.m_UriKind == UriKind.RelativeOrAbsolute 
                        ? (uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative) 
                        : this.m_UriKind);
//            return null; //base.ConvertFrom(context, culture, value);
        }

        private void SideEffect(int i0, int i1 = 0, int i2 = 0, string s3 = null, object o4 = null,
            int i5 = 0, int i6 = 0, object o7 = null) {}

        private void SideEffect(string s) {}

        private static void SideEffect(object o, string s = null) {}
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object Allocate(RuntimeType type);
    }
}