using System;
using System.Linq.Expressions;
using SerialDetector.KnowledgeBase.Internals;

namespace SerialDetector.KnowledgeBase
{
    internal class Template
    {
        private readonly Context context;

        public Template(Context context)
        {
            this.context = context;
        }
        
        public TemplateBuilder<T> Of<T>() => 
            new TemplateBuilder<T>(context);
        
        public void CreateBySignature(Expression<Action<It>> expression) => 
            new TemplateBuilder<object>(context).CreateBySignature(expression);

        public TResult CreateBySignature<TResult>(Expression<Func<It, TResult>> expression) => 
            new TemplateBuilder<object>(context).CreateBySignature(expression);
        
        public void CreateByName(Expression<Action<It>> expression) => 
            new TemplateBuilder<object>(context).CreateByName(expression);

        public TResult CreateByName<TResult>(Expression<Func<It, TResult>> expression) => 
            new TemplateBuilder<object>(context).CreateByName(expression);
    }
}