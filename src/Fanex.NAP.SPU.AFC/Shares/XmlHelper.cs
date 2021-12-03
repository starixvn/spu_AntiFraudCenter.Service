namespace Fanex.NAP.SPU.AFC.Shares
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    public static class XmlHelper
    {
        private static IDictionary<Type, IEnumerable<Func<object, object>>> typeLookup = new Dictionary<Type, IEnumerable<Func<object, object>>>();

        public static XElement ToXmlObj<T>(this T o) => ToXObject(o, typeof(T));

        public static string ToXmlString<T>(this T o) => o.ToXmlObj().ToString();

        private static XElement ToXObject(object o, Type t)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                var generic_param = t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];

                RegisterType(generic_param);

                var list = o as IEnumerable;

                var xml_parts = list.Cast<object>().Select(a => ToXObject(a, generic_param));

                return new XElement("Root", xml_parts);
            }
            else
            {
                RegisterType(t);

                var children =
                    typeLookup[t]
                    .Select(f => f(o));

                var name = t.GetCustomAttribute<XNameAttribute>()?.ElementName ?? t.Name;

                return new XElement(name, children);
            }
        }

        public static void RegisterType<T>() => RegisterType(typeof(T));

        private static void RegisterType(Type t)
        {
            if (typeLookup.ContainsKey(t))
            {
                return;
            }

            var props =
                t
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod != null && p.GetCustomAttribute<XmlIgnoreAttribute>() == null);

            var q = new Queue<Func<object, object>>();

            foreach (var prop in props)
            {
                Func<object, object> f = null;

                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType == typeof(decimal))
                {
                    f = CreatePrimitivePropReader(prop);
                }
                else
                {
                    RegisterType(prop.PropertyType);
                    f = CreateCompositePropReader(prop);
                }

                q.Enqueue(f);
            }

            typeLookup.Add(t, q);
        }

        private static Func<object, object> CreatePrimitivePropReader(PropertyInfo prop)
        {
            var arg_expr = Expression.Parameter(typeof(object));

            var obj_expr = Expression.Convert(arg_expr, prop.DeclaringType);

            var prop_expr = Expression.Convert(Expression.Property(obj_expr, prop), typeof(object));

            var attr_name = prop.GetCustomAttribute<XNameAttribute>()?.ElementName ?? prop.Name;

            var attribute_expr =
                Expression.New(
                    typeof(XAttribute).GetConstructor(new[] { typeof(XName), typeof(object) }),
                    Expression.Constant((XName)attr_name), prop_expr);

            var lambda_expr = Expression.Lambda<Func<object, object>>(attribute_expr, arg_expr);

            return lambda_expr.Compile();
        }

        private static Func<object, object> CreateCompositePropReader(PropertyInfo prop)
        {
            var arg_expr = Expression.Parameter(typeof(object));

            var obj_expr = Expression.Convert(arg_expr, prop.DeclaringType);

            var prop_expr = Expression.Convert(Expression.Property(obj_expr, prop), typeof(object));

            var f = Expression.Lambda<Func<object, object>>(prop_expr, arg_expr).Compile();

            return obj => ToXObject(f(obj), prop.PropertyType);
        }
    }
}