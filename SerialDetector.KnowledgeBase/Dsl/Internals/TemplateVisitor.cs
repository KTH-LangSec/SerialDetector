using System.Linq.Expressions;

namespace SerialDetector.KnowledgeBase.Internals
{
    internal class TemplateVisitor : ExpressionVisitor
    {
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            return base.VisitInvocation(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return base.VisitMethodCall(node);
        }
    }
}