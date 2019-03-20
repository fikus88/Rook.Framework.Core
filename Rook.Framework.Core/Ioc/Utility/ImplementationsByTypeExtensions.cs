using System;
using System.Collections.Generic;
using System.Linq;

namespace Rook.Framework.Core.Ioc.Utility
{
    public static class ImplementationsByTypeExtensions
    {
        public static void AddImplementationFor(this Dictionary<Type, HashSet<Type>> allImplementations, Type @interface, Type implementation)
        {
            var entry = allImplementations.GetInterfaceAndImplementationsBy(@interface);
            if (!entry.Equals(default(KeyValuePair<Type, HashSet<Type>>)))
            {
                allImplementations[entry.Key].Add(implementation);
            }
            else
            {
                allImplementations.Add(@interface, new HashSet<Type> {implementation});
            }
        }

        public static bool ContainsInterface(this Dictionary<Type, HashSet<Type>> allImplementations, Type @interface)
        {
            return !allImplementations.GetInterfaceAndImplementationsBy(@interface).Equals(default(KeyValuePair<Type, HashSet<Type>>));
        }

        public static KeyValuePair<Type, HashSet<Type>> GetInterfaceAndImplementationsBy(this Dictionary<Type, HashSet<Type>> allImplementations, Type @interface)
        {
            var implementations = allImplementations.Where(x => x.Key == @interface || @interface.IsSameGenericTypeAs(x.Key)).ToList();

            return implementations.Count > 1 
                ? implementations.SingleOrDefault(i => @interface.AmountOfGenericArgumentsMatches(i.Key)) 
                : implementations.SingleOrDefault();
        }

        public static bool AmountOfGenericArgumentsMatches(this Type type, Type key)
        {
            return key.GetGenericParameterArguments().Length == type.GetGenericArguments().Length;
        }

        public static Type[] GetGenericParameterArguments(this Type type)
        {
            return type.GetGenericArguments().Where(x => x.IsGenericParameter).ToArray();
        }
        
        private static bool IsSameGenericTypeAs(this Type @interface, Type type)
        {
            var typeDefinition = @interface.IsGenericType ? @interface.GetGenericTypeDefinition() : @interface;

            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeDefinition &&
                   @interface.HasGenericTypeArgumentsThatMatch(type, typeDefinition);
        }

        private static bool HasGenericTypeArgumentsThatMatch(this Type @interface, Type type, Type typeDefinition)
        {
            return @interface.GetGenericArguments().Length == 0 || typeDefinition.GetGenericArguments().SequenceEqual(type.GenericTypeArguments) ||
                   @interface.HasGenericParameterTypeArgumentsThatMatch(type);
        }

        private static bool HasGenericParameterTypeArgumentsThatMatch(this Type @interface, Type type)
        {
            return @interface.GetGenericParameterArguments().Length > 0 &&
                   @interface.GetGenericParameterArguments().SequenceEqual(type.GetGenericParameterArguments());
        }
    }
}