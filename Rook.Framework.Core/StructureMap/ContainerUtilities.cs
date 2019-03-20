using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StructureMap;

namespace Rook.Framework.Core.StructureMap
{
    public static class ContainerExtensions
    {
        public static Dictionary<Type, TAttr[]> GetAttributedTypes<TAttr>(this IContainer container, Type baseType) where TAttr : Attribute
        {
            return container.Model.AllInstances
                .Where(x => (x.PluginType.IsGenericType ? x.PluginType.GetGenericTypeDefinition() : x.PluginType) == baseType)
                .Select(x => x.ReturnedType)
                .ToDictionary(messageHandler => messageHandler, GetHandlerAttributes<TAttr>);
        }

        private static T[] GetHandlerAttributes<T>(Type messageHandler)
        {
            return messageHandler.GetTypeInfo().GetCustomAttributes()
                .Where(attr => attr.GetType() == typeof(T) || attr.GetType().IsSubclassOf(typeof(T))).Cast<T>().ToArray();
        }
    }
}
