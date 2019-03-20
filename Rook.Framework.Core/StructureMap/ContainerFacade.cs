using System;
using System.Collections.Generic;
using System.Linq;
using Rook.Framework.Core.Common;
using StructureMap;
using Container = Rook.Framework.Core.IoC.Container;
using LegacyContainer = Rook.Framework.Core.IoC.Container;

namespace Rook.Framework.Core.StructureMap
{
    public interface IContainerFacade
    {
        Dictionary<Type, TAttr[]> GetAttributedTypes<TAttr>(Type baseType) where TAttr : Attribute;
        object GetInstance(Type type);
        /// <summary>
        /// Gets instance of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unique">Unique only applies to legacy container, object lifecycle is managed within registries for StructureMap</param>
        /// <returns></returns>
        T GetInstance<T>(bool unique = false);
        /// <summary>
        /// Gets a named instance of type T. Only supported if using StructureMap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="namedInstance"></param>
        /// <returns></returns>
        T GetInstance<T>(string namedInstance);
        IEnumerable<T> GetAllInstances<T>();
        Type GetByTypeGuid(Guid guid);
        /// <summary>
        /// This only applies to the Legacy container. Structuremap dependencies should be wired up via the registry files
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        void Map<T>(T instance);
    }

    public class ContainerFacade : IContainerFacade
    {
        private readonly IContainer _container;
        private readonly IConfigurationManager _configurationManager;

        public ContainerFacade(IContainer container, IConfigurationManager configurationManager)
        {
            _container = container;
            _configurationManager = configurationManager;
        }

        public Dictionary<Type, TAttr[]> GetAttributedTypes<TAttr>(Type baseType) where TAttr : Attribute
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.GetAttributedTypes<TAttr>(baseType);
            }

            return IoC.Container.FindAttributedTypes<TAttr>();
        }

        public object GetInstance(Type type)
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.GetInstance(type);
            }

            return IoC.Container.GetInstance(type);
        }

        public T GetInstance<T>(bool unique = false)
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.GetInstance<T>();
            }

            return unique
                ? IoC.Container.GetNewInstance<T>()
                : IoC.Container.GetInstance<T>();
        }

        public T GetInstance<T>(string namedInstance)
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.GetInstance<T>(namedInstance);
            }

            throw new ArgumentException("Named instances not supported by LegacyContainer");
        }

        public IEnumerable<T> GetAllInstances<T>()
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.GetAllInstances<T>();
            }

            return IoC.Container.GetAllInstances<T>();
        }

        public Type GetByTypeGuid(Guid guid)
        {
            if (_configurationManager.Get("UseStructureMap", false))
            {
                return _container.Model.AllInstances.SingleOrDefault(x => x.ReturnedType.GUID == guid)?.ReturnedType;
            }

            return IoC.Container.GetTypeByGuid(guid);
        }
        
        public void Map<T>(T instance)
        {
            if (!_configurationManager.Get("UseStructureMap", false))
            {
                IoC.Container.Map(instance);
            }
        }
    }
}