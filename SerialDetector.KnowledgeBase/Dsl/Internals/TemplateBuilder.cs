using System;
using System.Linq.Expressions;
using System.Reflection;
using SerialDetector.Analysis;

namespace SerialDetector.KnowledgeBase.Internals
{
    internal class TemplateBuilder<T>
    {
        private readonly Context context;
        private readonly It it;
        private Version requiredOlderVersion = new Version();

        public TemplateBuilder(Context context)
        {
            this.context = context;
            it = new It(context);
        }

        public TemplateBuilder<T> AssemblyVersionOlderThan(int major, int minor, int build = 0, int revision = 0)
        {
            requiredOlderVersion = new Version(major, minor, build, revision);
            return this;
        }

        public void CreateBySignature(Expression<Action<It>> expression)
        {
            switch (context.Mode)
            {
                case ExecutionMode.Analyze:
                    Analyze(expression, bySignature: true);
                    return;

                case ExecutionMode.Test:
                    Test(expression);
                    return;

                default:
                    throw new NotSupportedException($"Unknown mode {context.Mode.ToString()}");
            }
        }

        public TResult CreateBySignature<TResult>(Expression<Func<It, TResult>> expression)
        {
            switch (context.Mode)
            {
                case ExecutionMode.Analyze:
                    Analyze(expression, bySignature: true);
                    return default(TResult);

                case ExecutionMode.Test:
                    return Test(expression);

                default:
                    throw new NotSupportedException($"Unknown mode {context.Mode.ToString()}");
            }
        }
        
        public void CreateByName(Expression<Action<It>> expression)
        {
            switch (context.Mode)
            {
                case ExecutionMode.Analyze:
                    Analyze(expression, bySignature: false);
                    return;

                case ExecutionMode.Test:
                    Test(expression);
                    return;

                default:
                    throw new NotSupportedException($"Unknown mode {context.Mode.ToString()}");
            }
        }

        public TResult CreateByName<TResult>(Expression<Func<It, TResult>> expression)
        {
            switch (context.Mode)
            {
                case ExecutionMode.Analyze:
                    Analyze(expression, bySignature: false);
                    return default(TResult);

                case ExecutionMode.Test:
                    return Test(expression);

                default:
                    throw new NotSupportedException($"Unknown mode {context.Mode.ToString()}");
            }
        }

        private void Analyze(LambdaExpression expression, bool bySignature)
        {
            if (!(expression.Body is MethodCallExpression methodCall))
            {
                throw new NotSupportedException($"The template '{expression}' doesn't contain a method call");
            }
            
            if (bySignature)
            {
                context.Templates.Add(
                    new TemplateInfo(
                        methodCall.Method.CreateMethodUniqueSignature(),
                        requiredOlderVersion));
                
                return;
            }

            var method = methodCall.Method;
            foreach (var declaringTypeMethod in method.DeclaringType.GetMethods(
                BindingFlags.Instance | BindingFlags.Static | 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                if (declaringTypeMethod.Name != method.Name || declaringTypeMethod.IsPrivate)
                    continue;
                
                context.Templates.Add(
                    new TemplateInfo(
                        declaringTypeMethod.CreateMethodUniqueSignature(),
                        requiredOlderVersion));
            }
        }

        private void Test(Expression<Action<It>> expression)
        {
            var action = expression.Compile();
            action(it);
        }

        private TResult Test<TResult>(Expression<Func<It, TResult>> expression)
        {
            var func = expression.Compile();
            return func(it);
        }
    }
}