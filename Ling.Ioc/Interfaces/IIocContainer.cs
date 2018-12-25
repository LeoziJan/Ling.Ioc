using Ling.Ioc.enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ling.Ioc.Interfaces
{
    public interface IIocContainer : IServiceProvider, IDisposable
    {
        IIocContainer RootIocContainer { get; set; }
        IDictionary<Type, ServiceDescriptor> Registries { get; set; }
        IDictionary<ServiceDescriptor, object> Services { get; set; }
        IInstanceFactory InstanceFactory { get; set; }
        ConcurrentBag<IDisposable> Disposables { get; set; }

        IIocContainer Register(ServiceDescriptor serviceDescriptor);

        IIocContainer Register(Type TInterface, Type TImplement, ServiceLifeTime serviceLifeTime);
        IIocContainer Register<TImplement>(ServiceLifeTime serviceLifeTime)
            where TImplement : class;
        IIocContainer Register<TInterface, TImplement>(ServiceLifeTime serviceLifeTime)
            where TImplement : class, TInterface
            where TInterface : class;

        IIocContainer RegisterAssembly(Assembly assembly);

        IIocContainer AddTransient<TInterface, TImplement>()
            where TImplement : class, TInterface
            where TInterface : class;

        IIocContainer AddScoped<TInterface, TImplement>()
            where TImplement : class, TInterface
            where TInterface : class;

        IIocContainer AddSingleton<TInterface, TImplement>()
           where TImplement : class, TInterface
           where TInterface : class;

        IIocContainer AddTransient<TImplement>()
          where TImplement : class;
        IIocContainer AddScoped<TImplement>()
            where TImplement : class;
        IIocContainer AddSingleton<TImplement>()
           where TImplement : class;
        IIocContainer AddTransient(Type TInterface, Type TImplement);
        IIocContainer AddScoped(Type TInterface, Type TImplement);
        IIocContainer AddSingleton(Type TInterface, Type TImplement);

        object Resolve(Type serviceType);
        T Resolve<T>();
        bool HasRegister(Type serviceType);

        IIocContainer CreateContainer(IIocContainer container);
    }
}
