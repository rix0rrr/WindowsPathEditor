using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WindowsPathEditor
{
    /// <summary>
    /// Helper extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Do an action for each element in a collection
        /// </summary>
        public static void Each<T>(this IEnumerable<T> xs, Action<T> action)
        {
            foreach (var x in xs) action(x);
        }

        public static void Notify<T>(this PropertyChangedEventHandler handler, Expression<Func<T>> memberExpression)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            var body = memberExpression.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentException("Lambda must return a property.");
            }

            var vmExpression = body.Expression as ConstantExpression;
            if (vmExpression != null)
            {
                LambdaExpression lambda = Expression.Lambda(vmExpression);
                Delegate vmFunc = lambda.Compile();
                object sender = vmFunc.DynamicInvoke();

                if (handler != null)
                {
                    handler(sender, new PropertyChangedEventArgs(body.Member.Name));
                }
            }
        }
    }
}