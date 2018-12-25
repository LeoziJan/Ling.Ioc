using Ling.Ioc.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Ling.Ioc.enums;
using System.Linq;

namespace Ling.Ioc
{
    public class IocContainer : IIocContainer
    {
        #region [ property ]
        public IIocContainer RootIocContainer { get; set; }
        public IDictionary<Type, ServiceDescriptor> Registries { get; set; }
        public IDictionary<ServiceDescriptor, object> Services { get; set; }
        public IInstanceFactory InstanceFactory { get; set; }
        public ConcurrentBag<IDisposable> Disposables { get; set; }

        private volatile bool _disposed;
        #endregion

        #region [ ctor ]
        public IocContainer()
        {
            IocContainerInit();
        }

        internal IocContainer(IIocContainer parent)
        {
            IocContainerInit(parent);
        }

        private void IocContainerInit(IIocContainer parent = null)
        {
            if (parent == null)
            {
                RootIocContainer = this;
                Registries = new ConcurrentDictionary<Type, ServiceDescriptor>();
            }
            else
            {
                RootIocContainer = parent.RootIocContainer; // parent
                Registries = RootIocContainer.Registries;  // inheritance parent
            }
            InstanceFactory = new DefaultFactory();
            Services = new ConcurrentDictionary<ServiceDescriptor, object>();
            Disposables = new ConcurrentBag<IDisposable>();
        }
        #endregion

        #region[ register ]
        public IIocContainer Register(ServiceDescriptor serviceDescriptor)
        {
            IsDisposed();
            var serviceType = serviceDescriptor.ServiceType;
            Registries[serviceType] = serviceDescriptor;
            return this;
        }

        public IIocContainer Register<TImplement>(ServiceLifeTime serviceLifeTime)
            where TImplement : class
        {
            Func<IIocContainer, Type[], object> factory =
                (iocContainer, arguments) => InstanceFactory.Create(this, typeof(TImplement), arguments);

            var serviceDescriptor = new ServiceDescriptor(typeof(TImplement), serviceLifeTime, factory);

            Register(serviceDescriptor);

            return this;
        }

        public IIocContainer Register(Type TInterface, Type TImplement, ServiceLifeTime serviceLifeTime)
        {
            if (!TInterface.IsAbstract || !TInterface.IsInterface)
                     throw new ArgumentException(nameof(TInterface));
            if (!TInterface.IsAssignableFrom(TImplement))
                     throw new ArgumentException(nameof(TImplement));


            Func<IIocContainer, Type[], object> factory =
                (iocContainer, arguments) => InstanceFactory.Create(this, TImplement, arguments);

            var serviceDescriptor = new ServiceDescriptor(TInterface, serviceLifeTime, factory);

            Register(serviceDescriptor);
            return this;
        }

        public IIocContainer Register<TInterface, TImplement>(ServiceLifeTime serviceLifeTime)
          where TImplement : class, TInterface
          where TInterface : class
                => Register(typeof(TInterface), typeof(TImplement), serviceLifeTime);

        public IIocContainer AddTransient<TInterface, TImplement>()
            where TImplement : class, TInterface
            where TInterface : class
             => Register<TInterface, TImplement>(ServiceLifeTime.Transient);
        public IIocContainer AddScoped<TInterface, TImplement>()
           where TImplement : class, TInterface
           where TInterface : class
            => Register<TInterface, TImplement>(ServiceLifeTime.Scoped);

        public IIocContainer AddSingleton<TInterface, TImplement>()
           where TImplement : class, TInterface
           where TInterface : class
           => Register<TInterface, TImplement>(ServiceLifeTime.Singleton);

        public IIocContainer AddTransient<TImplement>()
            where TImplement : class
             => Register<TImplement>(ServiceLifeTime.Transient);

        public IIocContainer AddScoped<TImplement>()
            where TImplement : class
             => Register<TImplement>(ServiceLifeTime.Scoped);

        public IIocContainer AddSingleton<TImplement>()
            where TImplement : class
             => Register<TImplement>(ServiceLifeTime.Singleton);

        public IIocContainer AddTransient(Type TInterface, Type TImplement)
               => Register(TInterface, TImplement, ServiceLifeTime.Transient);
        public IIocContainer AddScoped(Type TInterface, Type TImplement)
             => Register(TInterface, TImplement, ServiceLifeTime.Scoped);
        public IIocContainer AddSingleton(Type TInterface, Type TImplement)
             => Register(TInterface, TImplement, ServiceLifeTime.Singleton);

        public IIocContainer RegisterAssembly(Assembly assembly)
        {
            IsDisposed();
            var types = assembly.GetTypes();

            #region [ Transient ]
            var transientInterfaceTypes = types
                 .Where(t => t.GetInterfaces().Contains(typeof(ITransientDependency)))
                 .SelectMany(t => t.GetInterfaces().Where(f => !f.FullName.Contains(".ITransientDependency")))
                 .ToList();


            foreach (var interfaceType in transientInterfaceTypes)
            {
                var ImpType = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(interfaceType)).FirstOrDefault();
                if (ImpType != null)
                    AddTransient(interfaceType, ImpType);
            }
            #endregion

            #region [ Scoped ]
            var scopedInterfaceTypes = types
                  .Where(t => t.GetInterfaces().Contains(typeof(IScopeDependency)))
                  .SelectMany(t => t.GetInterfaces().Where(f => !f.FullName.Contains(".IScopeDependency")))
                  .ToList();


            foreach (var interfaceType in scopedInterfaceTypes)
            {
                var ImpType = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(interfaceType)).FirstOrDefault();
                if (ImpType != null)
                    AddScoped(interfaceType, ImpType);
            }
            #endregion

            #region [ Singleton ]
            var singletonInterfaceTypes = types
                   .Where(t => t.GetInterfaces().Contains(typeof(ISingletonDependency)))
                   .SelectMany(t => t.GetInterfaces().Where(f => !f.FullName.Contains(".ISingletonDependency")))
                   .ToList();


            foreach (var interfaceType in singletonInterfaceTypes)
            {
                var ImpType = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(interfaceType)).FirstOrDefault();
                if (ImpType != null)
                    AddSingleton(interfaceType, ImpType);
            }
            #endregion

            return RootIocContainer;
        }

        public bool HasRegister(Type serviceType)
        {
            return Registries.ContainsKey(serviceType);
        }
        #endregion

        #region[ resolve ]
        public object GetService(Type serviceType) => Resolve(serviceType);

        public T Resolve<T>() => (T)Resolve(typeof(T));

        public object Resolve(Type serviceType)
        {
            IsDisposed();
            if (serviceType == typeof(IIocContainer))
            {
                return this;
            }

            return Registries.TryGetValue(serviceType, out var serviceDescriptor)
                ? ResolveCore(serviceDescriptor, new Type[0]) : null;
        }

        private object ResolveCore(ServiceDescriptor serviceDescriptor, Type[] argumentsType)
        {
            IsDisposed();
            switch (serviceDescriptor.ServiceLifeTime)
            {
                case enums.ServiceLifeTime.Scoped:
                    return GetInstance(Services, Disposables);
                case enums.ServiceLifeTime.Singleton:
                    return GetInstance(RootIocContainer.Services, RootIocContainer.Disposables);
                case enums.ServiceLifeTime.Transient:
                    return CreateInstance(null, Disposables);
            }

            return serviceDescriptor.ImplementationFactory(this, argumentsType);

            object GetInstance(IDictionary<ServiceDescriptor, object> services, ConcurrentBag<IDisposable> disposables)
            {
                // has instance
                if (services.TryGetValue(serviceDescriptor, out object instance)) return instance;

                // not find 
                return CreateInstance(services, disposables);
            }


            object CreateInstance(IDictionary<ServiceDescriptor, object> services, ConcurrentBag<IDisposable> disposables)
            {
                var instance = serviceDescriptor.ImplementationFactory(this, argumentsType);

                if (services != null) services[serviceDescriptor] = instance;

                if (instance is IDisposable disposable) disposables.Add(disposable);

                return instance;
            }
        }
        #endregion

        #region [ other ]
        public IIocContainer CreateContainer(IIocContainer iocContainer)
            => new IocContainer((IocContainer)iocContainer);

        public void Dispose()
        {
            _disposed = true;
            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }
            while (!Disposables.IsEmpty)
            {
                Disposables.TryTake(out _);
            }
            Services.Clear();
        }

        private void IsDisposed()
        {
            if (_disposed)
                throw new Exception("IocContainer was Disposed");
        }
        #endregion
    }

}

