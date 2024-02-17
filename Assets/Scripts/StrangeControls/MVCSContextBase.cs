using UnityEngine;
using strange.extensions.context.impl;
using System;
using System.Linq;

public class MVCSContextBase : MVCSContext
{
    public MVCSContextBase(MonoBehaviour view) : base(view)
    {
    }

    public void BindSingletons(Type[] assemblyTypes, Type singletonType, bool crossContext)
    {
        //bind up all the singleton signals
        //note that this will not inject any types that are bound so these should be plain data classes
        foreach (Type type in assemblyTypes)
        {
            if (type.GetCustomAttributes(singletonType, true).Length > 0)
            {
                var interfaceName = "I" + type.Name;
                var implementedInterface = type.GetInterfaces().Where(x => x.Name == interfaceName).FirstOrDefault();
                if (implementedInterface != null)
                {
                    var sing = injectionBinder.Bind(implementedInterface).To(Activator.CreateInstance(type)).ToSingleton();
                    if (crossContext)
                    {
                        sing.CrossContext();
                    }
                }
                else
                {
                    var sing = injectionBinder.Bind(type).To(Activator.CreateInstance(type)).ToSingleton();
                    if (crossContext)
                    {
                        sing.CrossContext();
                    }
                }
            }
        }
    }

    public void BindViews(Type[] assemblyTypes)
    {
        //bind views to mediators
        foreach (Type type in assemblyTypes.Where(x => x.Name.EndsWith("View")))
        {
            if(type.IsInterface) continue;
            if (type.GetCustomAttributes(typeof(ManualMapSignalAttribute), true).Length > 0)
            {
                continue;
            }

            var mediatorName = type.Name.Replace("View", "Mediator");
            var mediatorType = assemblyTypes.Where(x => x.Name == mediatorName).FirstOrDefault();
            if (mediatorType != null)
            {
                mediationBinder.Bind(type).To(mediatorType);
            }
        }
    }

    public void BindSignals(Type[] assemblyTypes)
    {
        //bind signals to commands
        foreach (Type type in assemblyTypes.Where(x => x.Name.EndsWith("Signal")))
        {
            if(type.IsInterface) continue;
            if (type.GetCustomAttributes(typeof(ManualMapSignalAttribute), true).Length > 0)
            {
                continue;
            }

            var commandName = type.Name.Replace("Signal", "Command");
            var commandType = assemblyTypes.Where(x => x.Name == commandName).FirstOrDefault();
            if (commandType != null)
            {
                commandBinder.Bind(type).To(commandType);
            }
        }
    }

    public void TransferBindings()
    {
    }
}
