using System;
using System.Collections.Generic;
using System.Text;

namespace Ling.Ioc.Interfaces
{
    public interface IInstanceFactory
    {
        object Create(IIocContainer iocContainer, Type type, Type[] argumentsType);
    }
}
