using Ling.Ioc.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ling.Ioc
{
    internal class DefaultFactory : IInstanceFactory
    {
        public object Create(IIocContainer iocContainer, Type type, Type[] argumentsType)
        {
            if (argumentsType.Length > 0)
            {
                type = type.MakeGenericType(argumentsType);
            }
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new Exception($"{type} has not public ctor ");
            }
            var ctor = constructors.First();

            var parameters = ctor.GetParameters();

            if (parameters.Length == 0)
            {
                return Activator.CreateInstance(type);
            }
            var arguments = new object[parameters.Length];
            
            for (int i = 0; i < arguments.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;
                if (iocContainer.HasRegister(parameterType))
                {
                    arguments[i] = iocContainer.Resolve(parameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else
                {
                    throw new Exception($"can not create this Instance of {type},{type} is not Registered");
                }
            }

            return Activator.CreateInstance(type, arguments);
        }
    }
}
