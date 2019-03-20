using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.IoC;

namespace Rook.Framework.Core.Tests.Unit.Ioc
{
    [TestClass]
    public class IoCContainerTests
    {
        [TestMethod]
        public void GetInstanceOfGenericInterface()
        {
            IGenericInterface<string> instance = Container.GetInstance<IGenericInterface<string>>();
            Assert.IsTrue(instance is GenericTestStringImplementation);
        }

        [TestMethod]
        public void GetNewInstanceOfGenericInterface()
        {
            IGenericInterface<string> instance = Container.GetNewInstance<IGenericInterface<string>>();
            Assert.IsTrue(instance is GenericTestStringImplementation);
        }

		[TestMethod]
		public void GetNewInstanceOfNonGenericInterface()
		{
			INonGenericInterface instance = Container.GetNewInstance<INonGenericInterface>();
			Assert.IsTrue(instance is NonGenericImplementation);
		}

		[TestMethod]
		public void GetInstanceOfNonGenericInterface()
		{
			INonGenericInterface instance = Container.GetInstance<INonGenericInterface>();
			Assert.IsTrue(instance is NonGenericImplementation);
		}

		[TestMethod]
		public void GetInstance_WhenInterfaceHasNoImplementation_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetInstance<IHaveNoImplementations>());
		}

		[TestMethod]
		public void GetAllInstances_WhenInterfaceHasNoImplementation_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetAllInstances<IHaveNoImplementations>());
		}

		[TestMethod]
		public void GetAllInstances_GivenATypeThatIsNotAnInterface_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetAllInstances(typeof(NonGenericImplementation)));
		}

		[TestMethod]
		public void GetAllNewInstances_GivenATypeThatIsNotAnInterface_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetAllNewInstances(typeof(NonGenericImplementation)));
		}

		[TestMethod]
		public void GetNewInstance_GivenATypeThatHasMultipleImplementations_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetNewInstance(typeof(IHaveMultipleImplementations)));
		}

		[TestMethod]
		public void GetInstance_GivenATypeThatHasMultipleImplementations_ThrowsIoCValidateException()
		{
			Assert.ThrowsException<IoCValidateException>(() => Container.GetInstance(typeof(IHaveMultipleImplementations)));
		}

        [TestMethod]
        public void GetInstance_UsesGreediestConstructorItCanCreate_WhenCreatingObjectWithMultipleConstructors()
        {
            Container.Map(typeof(IMultipleConstructors), typeof(MultipleConstructors));

            var result = (MultipleConstructors)Container.GetInstance<IMultipleConstructors>();

            Assert.IsNotNull(result.Impl1);
            Assert.IsNotNull(result.Impl2);
            Assert.IsNull(result.NoImpl);
        }

        [TestMethod]
        public void GetInstance_AllowsGenericTypes()
        {
            var result = Container.GetInstance<IGenericInterfaceWithOnlyGenericImplementation<short>>();

            Assert.AreEqual(typeof(GenericClass<short>), result.GetType());
        }

        [TestMethod]
        public void Map_DoesNotBreakWhenRegisteringATypeAlias()
        {
            Container.Scan(typeof(IGenericInterfaceWithMoreParams<,>).Assembly);
        }

        [TestMethod]
        public void GetInstance_CanConstructGenericTypesWithMultipleGenericParameters()
        {
            var result = Container.GetInstance<IGenericInterfaceWithMoreParams<int, int>>();
            Assert.AreEqual(typeof(TypeNotAliasedTest<int, int>), result.GetType());
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void GetInstance_DoesNotSwallowExceptionsThrownInConstructor()
        {
            Container.GetInstance<UsingFailingClass>();
        }

        [TestMethod]
        public void GetInstance_WhenImplementationImplementsAbstractBaseClass_ShouldNotThrowException()
        {
            Container.GetInstance<IAbstractBaseClass>();
        }

        [TestMethod, ExpectedException(typeof(IoCMapException))]
        public void Map_GivenAbstractImplementation_ThrowsIoCMapException()
        {
            Container.Map(typeof(IAbstractBaseClass), typeof(AbstractBaseClass));
        }
	}

    public class UsingFailingClass
    {
        private readonly FailingClass _failingClass;

        public UsingFailingClass(FailingClass failingClass)
        {
            _failingClass = failingClass;
        }
    }
    
    public class FailingClass
    {
        public FailingClass()
        {
            throw new ApplicationException("Something went drastically wrong!");
        }
    }
    
    public interface IGenericInterfaceWithMoreParams<T1, T2> { }
    public interface IGenericInterfaceWithMoreParams<T1> : IGenericInterfaceWithMoreParams<T1, object> { }

    public class TypeNotAliasedTest<T1, T2> : IGenericInterfaceWithMoreParams<T1, T2>
    {
    };
    public class TypeAliasedTest<T1> : IGenericInterfaceWithMoreParams<T1> { }




    public interface IGenericInterface<T>
    {
        T Run();
    }

    public interface IGenericInterfaceWithOnlyGenericImplementation<T> { }

    public class GenericClass<T> : IGenericInterfaceWithOnlyGenericImplementation<T> { }

    public class GenericTestStringImplementation : IGenericInterface<string>
    {

        public GenericTestStringImplementation() { }
        public string Run()
        {
            return "blah";
        }
    }

    public class GenericTestLongImplementation : IGenericInterface<long>
    {

        public GenericTestLongImplementation() { }
        public long Run()
        {
            return 1;
        }
    }

    public interface INonGenericInterface
    {
        void Run();
    }

    public class NonGenericImplementation : INonGenericInterface
    {
        public void Run()
        {
            return;
        }
    }

    public interface IHaveNoImplementations { }
    public interface IHaveMultipleImplementations { }
    public class ImplementationOne : IHaveMultipleImplementations { }
    public class ImplementationTwo : IHaveMultipleImplementations { }

    public interface IMultipleConstructors { }
    public class MultipleConstructors : IMultipleConstructors
    {
        public readonly IGenericInterface<string> Impl1;
        public readonly IGenericInterface<long> Impl2;
        public readonly IGenericInterface<int> NoImpl;

        public MultipleConstructors(IGenericInterface<string> impl1, IGenericInterface<long> impl2, IGenericInterface<int> noimpl)
        {
            Impl1 = impl1;
            Impl2 = impl2;
            NoImpl = noimpl;
        }

        public MultipleConstructors(IGenericInterface<string> impl1, IGenericInterface<long> impl2)
        {
            Impl1 = impl1;
            Impl2 = impl2;
        }

        public MultipleConstructors(IGenericInterface<string> impl1)
        {
            Impl1 = impl1;
        }
    }

    public interface IAbstractBaseClass { }

    public abstract class AbstractBaseClass : IAbstractBaseClass { }

    public class MyBaseClass : AbstractBaseClass { }
}
