using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.IoC;

namespace Rook.Framework.Core.Tests.Unit.Ioc
{
	[TestClass]
	public class IoCContainerMultipleDependencyTests
	{
		[TestMethod]
		public void GetNewInstanceOfInterface_WhenImplementationTakesArrayOfGenericDependencies_ReturnsInstanceContainingAllMappedDependencies()
		{
			IGenericTestService genericTestService = Container.GetNewInstance<IGenericTestService>();
			Assert.IsTrue(genericTestService is MyGenericService);

			var resolvedDependencies = genericTestService.GetDependencies();
			Assert.AreEqual(2, resolvedDependencies.Length);

			Assert.IsNotNull(resolvedDependencies.SingleOrDefault(x => x.GetType() == typeof(GenericDependencyOne)), $"Expected one dependency of type {nameof(GenericDependencyOne)}");
			Assert.IsNotNull(resolvedDependencies.SingleOrDefault(x => x.GetType() == typeof(GenericDependencyTwo)), $"Expected one dependency of type {nameof(GenericDependencyTwo)}");
		}

		[TestMethod]
		public void GetNewInstanceOfInterface_WhenImplementationTakesArrayOfDependencies_ReturnsInstanceContainingAllMappedDependencies()
		{
			ITestService testService = Container.GetNewInstance<ITestService>();
			Assert.IsTrue(testService is MyService);

			var resolvedDependencies = testService.GetDependencies();
			Assert.AreEqual(2, resolvedDependencies.Length);

			Assert.IsNotNull(resolvedDependencies.SingleOrDefault(x => x.GetType() == typeof(DependencyOne)), $"Expected one dependency of type {nameof(DependencyOne)}");
			Assert.IsNotNull(resolvedDependencies.SingleOrDefault(x => x.GetType() == typeof(DependencyTwo)), $"Expected one dependency of type {nameof(DependencyTwo)}");
		}

		[TestMethod]
		public void GetNewInstance_WhenImplementationTakesMixtureOfDependencies_ResturnsInstanceContainingAllMappedDependencies()
		{
			IComplexConstructorService complexInstance = Container.GetNewInstance<IComplexConstructorService>();

			var resolvedDependencies = complexInstance.GetDependencies();

			Assert.AreEqual(2, resolvedDependencies.nonGenericDependencyArray.Length);
			Assert.AreEqual(2, resolvedDependencies.nonGenericDependencyArray.Length);

			Assert.IsNotNull(resolvedDependencies.nonGenericDependencyArray.SingleOrDefault(x => x.GetType() == typeof(DependencyOne)), $"Expected one dependency of type {nameof(DependencyOne)}");
			Assert.IsNotNull(resolvedDependencies.nonGenericDependencyArray.SingleOrDefault(x => x.GetType() == typeof(DependencyTwo)), $"Expected one dependency of type {nameof(DependencyTwo)}");

			Assert.IsNotNull(resolvedDependencies.genericDependencyArray.SingleOrDefault(x => x.GetType() == typeof(GenericDependencyOne)), $"Expected one dependency of type {nameof(GenericDependencyOne)}");
			Assert.IsNotNull(resolvedDependencies.genericDependencyArray.SingleOrDefault(x => x.GetType() == typeof(GenericDependencyTwo)), $"Expected one dependency of type {nameof(GenericDependencyTwo)}");

			Assert.IsTrue(resolvedDependencies.standaloneDependency is StandaloneDependency, $"Expected one dependency of type {nameof(StandaloneDependency)}");
		}

		private interface IGenericDependency<T>
		{
			T Run();
		}

		private class GenericDependencyOne : IGenericDependency<int>
		{
			public int Run()
			{
				return 100;
			}
		}

		private class GenericDependencyTwo : IGenericDependency<int>
		{
			public int Run()
			{
				return 100;
			}
		}

		private interface IGenericTestService
		{
			IGenericDependency<int>[] GetDependencies();
		}

		private class MyGenericService : IGenericTestService
		{
			IGenericDependency<int>[] _dependencies;

			public MyGenericService(IGenericDependency<int>[] dependencies)
			{
				_dependencies = dependencies;
			}

			public IGenericDependency<int>[] GetDependencies()
			{
				return _dependencies;
			}
		}

		private interface IDependency
		{
			void Run();
		}

		private class DependencyOne : IDependency
		{
			public void Run()
			{
				return;
			}
		}

		private class DependencyTwo : IDependency
		{
			public void Run()
			{
				return;
			}
		}

		private interface ITestService
		{
			IDependency[] GetDependencies();
		}

		private class MyService : ITestService
		{
			IDependency[] _dependencies;

			public MyService(IDependency[] dependencies)
			{
				_dependencies = dependencies;
			}

			public IDependency[] GetDependencies()
			{
				return _dependencies;
			}
		}

		private interface IComplexConstructorService
		{
			(IDependency[] nonGenericDependencyArray, IGenericDependency<int>[] genericDependencyArray, IStandaloneDependency standaloneDependency) GetDependencies();
		}

		private class ComplexConstructorService : IComplexConstructorService
		{
			private IDependency[] _dependencies;
			private IGenericDependency<int>[] _genericDependencies;
			private IStandaloneDependency _standaloneDependency;

			public ComplexConstructorService(
				IDependency[] dependencies,
				IGenericDependency<int>[] genericDependencies,
				IStandaloneDependency standaloneDependency)
			{
				_dependencies = dependencies;
				_genericDependencies = genericDependencies;
				_standaloneDependency = standaloneDependency;
			}

			public (IDependency[] nonGenericDependencyArray, IGenericDependency<int>[] genericDependencyArray, IStandaloneDependency standaloneDependency) GetDependencies()
			{
				return (_dependencies, _genericDependencies, _standaloneDependency);
			}
		}

		private interface IStandaloneDependency { }

		public class StandaloneDependency : IStandaloneDependency { }
	}
}
