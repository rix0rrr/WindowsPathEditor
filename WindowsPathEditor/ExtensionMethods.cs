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

        /// <summary>
        /// Perform an action on an object, then return it
        /// </summary>
        public static T Tap<T>(this T x, Action<T> action)
        {
            action(x);
            return x;
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

        /// <summary>
        /// Make raising PropertyChanged events more elegant
        /// </summary>
        public static bool ChangeAndNotify<T>(this PropertyChangedEventHandler handler, ref T field, T value, Expression<Func<T>> memberExpression)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            Notify<T>(handler, memberExpression);

            field = value;
            return true;
        }
    }
}