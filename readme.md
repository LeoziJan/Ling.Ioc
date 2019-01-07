# Ling.Ioc
[img](https://img.shields.io/badge/netcore-2.1-brightgreen.svg)
## How to use ?
### Demo
``` csharp
// init
IIocContainer root=new IocContainer();

// register
root.AddTransient<Interface,Impelement>()  // return IIocContainer 
    // so you can
    .AddScope<Interface,Impelement>()
    .AddSinleton<Interface,Impelement>();

// or you can 
root.RegisterAssmbly(your assmbly);

// you service class must be ImpImpelement (ITransientDependency / IScopeDependency / ISinletonDependency)
public interface IInvoiceService {}
public class InvoiceService:IInvoiceService,ITransientDependency
{   
}
// get service
var invoicService=root.Resolve<IInvoiceService>();
```


