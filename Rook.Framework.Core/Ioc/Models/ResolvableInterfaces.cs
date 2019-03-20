using System;
using System.Collections.Generic;
using System.Linq;
using Rook.Framework.Core.Ioc.Utility;

namespace Rook.Framework.Core.Ioc.Models
{
    internal class ResolvableInterfaces
    {
        private readonly Dictionary<Type, HashSet<Type>> _interfaces = new Dictionary<Type, HashSet<Type>>();

        internal bool ContainsType(Type @interface)
        {
            var containsResolvedInterface = _interfaces.ContainsInterface(@interface);

            if (!containsResolvedInterface && @interface.IsGenericType)
            {
                // could still possibly create from a generic class.
                return GetInterfaceThatMatchesGenericArgumentsFor(@interface) != null;
            }

            return containsResolvedInterface;
        }

        public void Add(Type @interface, Type implementation)
        {
            _interfaces.AddImplementationFor(@interface, implementation);
        }
        
        internal HashSet<Type> GetTypes(Type @interface)
        {
            var types = new HashSet<Type>();
            if (TryGetTypes(@interface, out KeyValuePair<Type, HashSet<Type>> value))
            {
                types = value.Value;
            }

            if (@interface.IsGenericType)
            {
                var key = GetInterfaceThatMatchesGenericArgumentsFor(@interface);
                if (key != null)
                    return _interfaces[key];
            }

            return types;
        }

        private bool TryGetTypes(Type @interface, out KeyValuePair<Type, HashSet<Type>> resolvableInterface)
        {
            resolvableInterface = _interfaces.GetInterfaceAndImplementationsBy(@interface);

            if (!resolvableInterface.Equals(default(KeyValuePair<Type, HashSet<Type>>)))
            {
                return true;
            }

            return false;
        }

        private Type GetInterfaceThatMatchesGenericArgumentsFor(Type @interface)
        {
            return _interfaces.Keys
                .Where(i => i.IsGenericType && i.GetGenericParameterArguments().Length > 0 
                                            && i.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())
                .SingleOrDefault(@interface.AmountOfGenericArgumentsMatches);
        }
    }
}