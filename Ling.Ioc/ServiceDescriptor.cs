using System;
using System.Collections.Generic;
using System.Text;
using Ling.Ioc.enums;
using Ling.Ioc.Interfaces;

namespace Ling.Ioc
{
    public class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public ServiceLifeTime ServiceLifeTime { get; }
        public Func<IIocContainer, Type[], object> ImplementationFactory { get; }

        public ServiceDescriptor(
            Type serviceType,
            ServiceLifeTime serviceLifeTime,
            Func<IIocContainer,Type[],object> implementationFactory
            )
        {
            ServiceType = serviceType;
            ServiceLifeTime = serviceLifeTime;
            ImplementationFactory = implementationFactory;
        }
    }
}
