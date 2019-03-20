using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Rook.Framework.Core.Ioc.Models;
using Rook.Framework.Core.Ioc.Utility;

namespace Rook.Framework.Core.IoC
{
    /// <summary>
    /// IoC Container
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Container
    {
        private static readonly ResolvableInterfaces ResolvableInterfaces = new ResolvableInterfaces();
        private static readonly Dictionary<Type, List<object>> ConstructedInstances = new Dictionary<Type, List<object>>();
        private static readonly object CustomAttributeCacheLock = new object();

        static Container()
        {
            Scan(Assembly.GetEntryAssembly());
        }

        private static Type[] Types { get; set; } = new Type[0];

        private static readonly List<Assembly> Assemblies = new List<Assembly>();

        private static readonly IList<string> AssemblyBlackList = new List<string> {"System.", "Microsoft."};

        /// <summary>
        /// Scans the given assemblies to generate mappings from interfaces to concrete types, where possible. Mappings will be created for each implementation of the interface. Generated mappings can be overwritten by making subsequent calls to Map
        /// </summary>
        /// <param name="assemblies">The assemblies to scan</param>
        public static void Scan(params Assembly[] assemblies)
        {
            var assembliesToScan = GetAssembliesToScanFrom(assemblies);

            if (assembliesToScan.Any())
            {
                var assemblyTypes = assembliesToScan.SelectMany(a => a.GetTypes()).ToList();
                Types = Types.Union(assemblyTypes).Distinct().ToArray();

                Type[] interfaces = Types.Where(t => t.GetTypeInfo().IsInterface).ToArray();
                Type[] concretes = assemblyTypes.Where(t => !t.GetTypeInfo().IsInterface).ToArray();

                foreach (Type @interface in interfaces)
                {
                    if (@interface.GetTypeInfo().IsGenericType)
                    {
                        // Reverse the search and find implementations without generic parameters and a matching name
                        Dictionary<Type, HashSet<Type>> implementations = new Dictionary<Type, HashSet<Type>>();
                        foreach (Type type in concretes)
                        {
                            foreach (Type typeInterface in type.GetInterfaces())
                            {
                                var typeInfo = typeInterface.GetTypeInfo();

                                if (typeInfo.IsGenericType
                                    && @interface == typeInfo.GetGenericTypeDefinition()
                                    && type.GetGenericParameterArguments().Length == typeInterface.GetGenericParameterArguments().Length
                                    && !type.IsAbstract)
                                {
                                    implementations.AddImplementationFor(typeInterface, type);
                                }
                            }
                        }

                        foreach (KeyValuePair<Type, HashSet<Type>> implementation in implementations)
                        {
                            foreach (Type type in implementation.Value)
                            {
                                Map(implementation.Key, type);
                            }
                        }
                    }
                    else
                    {
                        Type[] candidates = concretes.Where(t => t.GetInterfaces().Contains(@interface)).ToArray();
                        foreach (var candidate in candidates)
                        {
                            if (!candidate.IsAbstract)
                            {
                                Map(@interface, candidate);
                            }
                        }
                    }
                }
            }
        }

        private static List<Assembly> GetAssembliesToScanFrom(IEnumerable<Assembly> assemblies)
        {
            List<Assembly> assembliesToScan;
            lock (Assemblies)
            {
                assembliesToScan = assemblies.Except(Assemblies).ToList();
                var referencedAssemblyNames = assembliesToScan.SelectMany(
                    x => x.GetReferencedAssemblies().Where(assemblyName => !AssemblyBlackList.Any(bl => assemblyName.Name.StartsWith(bl)))
                );
                assembliesToScan.AddRange(referencedAssemblyNames.Select(Assembly.Load).Except(Assemblies));

                Assemblies.AddRange(assembliesToScan);
            }

            return assembliesToScan;
        }

        /// <summary>
        /// Creates an internal mapping from the given interface to the given implementation. Throws an exception if the implementation does not implement the interface. <i>Mappings will be overwritten by a subsequent call to Scan</i>
        /// </summary>
        /// <typeparam name="TInterface">The interface</typeparam>
        /// <param name="implementation">The implementation of the interface</param>
        public static void Map<TInterface>(TInterface implementation)
        {
			MapConstructedInstance(typeof(TInterface), implementation);
        }

		private static void MapConstructedInstance(Type root, object implementation)
		{
			if (ConstructedInstances.ContainsKey(root))
			{
				ConstructedInstances[root].Add(implementation);
			}
			else
			{
				ConstructedInstances.Add(root, new List<object>() { implementation });
			}
		}

        /// <summary>
        /// Creates an internal mapping from the given interface to the given implementation. Throws an exception if the implementation does not implement the interface. <i>Mappings will be overwritten by a subsequent call to Scan</i>
        /// </summary>
        /// <typeparam name="TInterface">The interface</typeparam>
        /// <typeparam name="TImplementation">The implementation of the interface</typeparam>
        public static void Map<TInterface, TImplementation>()
        {
            Map(typeof(TInterface), typeof(TImplementation));
        }

        /// <summary>
        /// Creates an internal mapping from the given interface to the given implementation. Throws an exception if the implementation does not implement the interface. <i>Mappings will be overwritten by a subsequent call to Scan</i>
        /// </summary>
        /// <param name="interface">The interface</param>
        /// <param name="implementation">The implementation of the interface</param>
        public static void Map(Type @interface, Type implementation)
        {
            if (!@interface.GetTypeInfo().IsInterface)
            {
                throw new IoCMapException($"{@interface.FullName} was passed as the interface parameter of the Map method, but it doesn't appear to be an interface.");
            }

            if(implementation.IsAbstract)
            {
                throw new IoCMapException($"Cannot map {implementation.Name} to {@interface.Name} because it is abstract.");
            }

            if (!DoesImplementationImplementInterface(@interface, implementation))
            {
                throw new IoCMapException($"Implementation {implementation.FullName} does not implement interface {@interface.FullName}");
            }

            ResolvableInterfaces.Add(@interface, implementation);
        }

        private static bool DoesImplementationImplementInterface(Type @interface, Type implementation)
        {
            return implementation.GetInterfaces().Any(i =>
                i == @interface
                || i.IsGenericType && (i.GetGenericTypeDefinition() == @interface || @interface.IsGenericType && i.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition()));
        }

        /// <summary>
        /// Gets a known constructed implementation for the given root key. Repeated calls to this method will return the same instance.
        /// </summary>
        /// <typeparam name="TRoot">The root key to be constructed (this should be an interface)</typeparam>
        /// <returns>A constructed instance of the type indicated by the key</returns>
        public static TRoot GetInstance<TRoot>()
		{
			return (TRoot)GetInstance(typeof(TRoot));
		}

        /// <summary>
        /// Gets a known constructed implementationfor the given root key. Repeated calls to this method will return the same instance.
        /// </summary>
        /// <param name="root">The root key to be constructed (this should be an interface)</param>
        /// <returns>A constructed instance of the type indicated by the key</returns>
        public static object GetInstance(Type root)
        {
            lock (ConstructedInstances)
            {            
                // If we have an interface, resolve it and create a new instance
                if (root.GetTypeInfo().IsInterface || root.GetTypeInfo().IsArray && (root.GetTypeInfo().GetElementType()?.IsInterface ?? false))
                {
                    if (!ConstructedInstances.ContainsKey(root))
                    {
                        MapConstructedInstance(root, GetNewInstance(root));
                    }

                    return ConstructedInstances[root].SingleOrDefault() ?? throw IoCValidateException.NewMultipleImplementationsFoundException(root);
                }

                // If it isn't an interface but we have it anyway, fish out the instance
                if (ConstructedInstances.ContainsKey(root))
                {
                    return ConstructedInstances[root].SingleOrDefault() ?? throw IoCValidateException.NewMultipleImplementationsFoundException(root);
                }

                // If we don't have the type in the dictionary keys, look through the values and return the first one we find.
                foreach (object constructedInstance in ConstructedInstances.SelectMany(x => x.Value))
                    if (constructedInstance.GetType() == root) return constructedInstance;

                // We still haven't got an instance, create a new one
                object instance = GetNewInstance(root);

                // Add it, keyed by type (not interface)
                ConstructedInstances.Add(root, new List<object>() { instance });

                return instance;
            }
        }

        /// <summary>
        /// Gets a new instance for the given type T. If T is an interface, the mapped interfaces will be searched for an implementation. If T is a type, it will be constructed with all resolvable interfaces.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetNewInstance<T>()
        {
            return (T)GetNewInstance(typeof(T));
        }

        /// <summary>
        /// Gets a new instance for the given root key. If the key is an interface, the mapped interfaces will be searched for an implementation. If the key is a type, it will be constructed with all resolvable interfaces.
        /// </summary>
        /// <param name="root">The root key (interface or class) to be constructed</param>
        /// <returns>A constructed instance of the type indicated by the key</returns>
        public static object GetNewInstance(Type root)
		{
			Type implementation;

			if (root.IsInterface)
			{
				EnsureTypeIsResolvable(root);

				// Whilst we do support injecting an array of dependencies into an implementation, supplying a root which has multiple implementations to GetNewInstance is not valid, as we don't know which one to return.

			    var types = ResolvableInterfaces.GetTypes(root);

				if (types.Count > 1)
				{
					throw IoCValidateException.NewMultipleImplementationsFoundException(root);
				}

                // if we don't have any types relating directly to the interface, we might still about to construct from a generic class
			    implementation = types.Count == 0 
			        ? ResolvableInterfaces.GetTypes(root.GetGenericTypeDefinition()).Single() 
			        : types.Single();
			}
			else
			{
				implementation = root;
			}

		    return implementation.ContainsGenericParameters
		        ? ConstructGenericType(implementation, root.GenericTypeArguments)
		        : ConstructType(implementation);
		}

		/// <summary>
		/// Gets an array of known constructed implementations for T. T must be an interface.
		/// Repeated calls to this method for the same T will return the same array of instances.
		/// </summary>
		/// <returns>An array of constructed instances of T</returns>
		public static T[] GetAllInstances<T>()
		{
			return GetAllInstances(typeof(T)).Cast<T>().ToArray();
		}

		/// <summary>
		/// Gets an array of known constructed implementations for the given root key. The key must be an interface.
		/// Repeated calls to this method for a given root key will return the same array of instances.
		/// </summary>
		/// <param name="root">The root key (must be an interface) to be constructed</param>
		/// <returns>An array of constructed instances of the type indicated by the key</returns>
		public static IEnumerable GetAllInstances(Type root)
		{
			if (!root.IsInterface)
			{
				throw new IoCValidateException("Cannot get all instances of a type that is not an interface.");
			}

			// If there aren't any constructed instances of the interface, resolve and construct any implementations.
			if (!ConstructedInstances.ContainsKey(root))
			{
			    var instances = GetAllNewInstances(root);
			    if (instances.Length > 0)
			    {
			        foreach (var instance in GetAllNewInstances(root))
			            MapConstructedInstance(root, instance);
			    }
			    else
			    {
			        var listType = typeof(List<>);
			        return (IEnumerable)Activator.CreateInstance(listType.MakeGenericType(root));
                }
			}

		    return ConstructedInstances[root];
		}

		/// <summary>
		/// Gets an array of newly constructed implementations for the given T. If T is an interface, the mapped interfaces will be searched for implementations. This method does not accept a concrete type T.
		/// </summary>
		/// <returns>An array of newly constructed implementations of T</returns>
		public static T[] GetAllNewInstances<T>()
		{
			return GetAllNewInstances(typeof(T)).Select(i => (T)i).ToArray();
		}

		/// <summary>
		/// Gets an array of newly constructed implementations for the given root key. If the key is an interface, the mapped interfaces will be searched for implementations. This method does not accept a concrete type as a root key.
		/// </summary>
		/// <param name="root">The root key (must be an interface) to be constructed</param>
		/// <returns>An array of newly constructed instances of the type indicated by the key</returns>
		public static object[] GetAllNewInstances(Type root)
		{
			if (!root.IsInterface)
			{
				throw new IoCValidateException("Cannot get all instances of a type that is not an interface.");
			}

			EnsureTypeIsResolvable(root);

		    return ResolvableInterfaces.GetTypes(root)
		        .Where(i => !i.ContainsGenericParameters)
		        .Select(ConstructType).ToArray();
		}

		private static void EnsureTypeIsResolvable(Type root)
		{
			if (!ResolvableInterfaces.ContainsType(root))
			{
				Scan(root.GetTypeInfo().Assembly);
			}
            
			if (!ResolvableInterfaces.ContainsType(root))
			{
				throw new IoCValidateException($"Could not find a constructable implementation of {root.Name} - i.e. a class having only one constructor with 0 or more interfaces, but no other parameters.");
			}
		}

        private static object ConstructGenericType(Type implementation, Type[] genericTypeArguments)
        {
            var type = implementation.MakeGenericType(genericTypeArguments);
            return ConstructType(type);
        }

		private static object ConstructType(Type implementation)
		{
			ConstructorInfo[] constructorInfos = implementation.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(c =>
					c.GetParameters().Length > 0
					&& c.GetParameters().All(p => p.ParameterType.GetTypeInfo().IsInterface || (p.ParameterType.GetTypeInfo().IsArray && (p.ParameterType.GetElementType()?.IsInterface ?? false))))
			  .ToArray();

			if (constructorInfos.Length == 0)
			{
			    ConstructorInfo constructor = implementation.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(c => c.GetParameters().Length == 0);
			    if (constructor != null)
			        return constructor.Invoke(new object[0]);
            }

		    var availableConstructors = constructorInfos.OrderByDescending(x => x.GetParameters().Length);
            var exceptions = new List<Exception>();
		    foreach (var ctor in availableConstructors)
		    {
		        var result = TryInvokeConstructor(ctor);

                if (result.Success)
		        {
		            return result.ConstructedObject;
		        }
                else
                {
                    exceptions.Add(result.Exception);
                }
		    }

		    throw new AggregateException($"Could not create {implementation.FullName} from any of it's constructors", exceptions);
		}

        private static ConstructorInvokerResult TryInvokeConstructor(ConstructorInfo constructorInfo)
        {
            try
            {
                var parameters = constructorInfo.GetParameters().Select(GetParameterInternal).ToArray();
                return new ConstructorInvokerResult
                {
                    ConstructedObject = constructorInfo.Invoke(parameters),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ConstructorInvokerResult
                {
                    ConstructedObject = null,
                    Exception = ex,
                    Success = false
                };
            }
        }

        private class ConstructorInvokerResult
        {
            public object ConstructedObject { get; set; }
            public bool Success { get; set; }
            public Exception Exception { get; set; }
        }

		private static object GetParameterInternal(ParameterInfo parameterType)
		{
			Type type = parameterType.ParameterType;

			if (type.IsArray && (type.GetElementType()?.IsInterface ?? false))
			{
				// We invoke the generic GetAllInstances overload to ensure we get T[] not object[].
			    return InvokeGetAllNewInstances(type.GetElementType());
			}

		    if (typeof(IEnumerable).IsAssignableFrom(type))
			{
			    var childType = type.GetGenericArguments()[0];
			    return InvokeGetAllNewInstances(childType);
            }

		    if (type.IsInterface)
		    {
		        return GetInstance(type);
		    }

		    throw new IoCValidateException($"Unable to resolve parameter of type {parameterType.ParameterType.Name}");
		}

        private static object InvokeGetAllNewInstances(Type type)
        {
            // We invoke the generic GetAllInstances overload to ensure we get T[] not object[].
            var methodInfo = typeof(Container).GetMethod(nameof(GetAllNewInstances), new Type[] { });
            if (methodInfo != null)
            {
                var getAllInstancesMethod = methodInfo.MakeGenericMethod(type);
                return getAllInstancesMethod.Invoke(null, null);
            }

            throw new IoCValidateException($"Unable to resolve parameter of type {type.Name}");
        }

		public static Dictionary<Type, TAttribute[]> FindAttributedTypes<TAttribute>() where TAttribute : Attribute
        {
            Dictionary<Type, TAttribute[]> result = new Dictionary<Type, TAttribute[]>();
            foreach (Type type in Types)
            {
                TAttribute[] attributes = GetTypeAttributes<TAttribute>(type);
                if (attributes.Any())
                    result.Add(type, attributes);
            }
            return result;
        }

        private static readonly Dictionary<Type, IEnumerable<Attribute>> CustomAttributeCache = new Dictionary<Type, IEnumerable<Attribute>>();

        private static TAttribute[] GetTypeAttributes<TAttribute>(Type type) where TAttribute : Attribute
        {
            lock (CustomAttributeCacheLock)
            {
                if (!CustomAttributeCache.ContainsKey(type))
                    CustomAttributeCache.Add(type, type.GetTypeInfo().GetCustomAttributes());
                return CustomAttributeCache[type].Where(a => a.GetType() == (typeof(TAttribute)) || a.GetType().IsSubclassOf(typeof(TAttribute))).Cast<TAttribute>().ToArray();
            }
        }

        public static IEnumerable<Type> GetImplementationsOf(Type @interface)
        {
            return Types.Where(t => !t.IsAbstract && t.GetInterfaces().Contains(@interface));
        }

        public static IEnumerable<Type> GetImplementations<TInterface>()
        {
            return GetImplementationsOf(typeof(TInterface));
        }

        public static Type GetTypeByGuid(Guid typeGuid)
        {
            return Types.FirstOrDefault(t => t.GUID == typeGuid);
        }
    }
}
