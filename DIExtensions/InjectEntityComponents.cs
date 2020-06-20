using Stride.Engine;
using System;
using System.Reflection;

namespace DIExtensions
{
    public static class InjectEntityComponentsExtension
    {
        public static void InjectEntityComponents(this EntityComponent component)
        {
            var fields = component.GetType().GetFields(BindingFlags.NonPublic |
                         BindingFlags.Public | BindingFlags.Instance);
            var properties = component.GetType().GetProperties(BindingFlags.NonPublic |
                         BindingFlags.Public | BindingFlags.Instance);

            var getComponentMethod = component.Entity.GetType().GetMethod("Get", new Type[] { });
            foreach (var field in fields)
            {
                if(field.GetCustomAttribute<EntityComponentAttribute>() != null)
                {
                    var componentType = field.FieldType;
                    var injectedComponent = getComponentMethod.MakeGenericMethod(componentType)
                        .Invoke(component.Entity, new object[] { });

                    if (injectedComponent == null)
                        throw new NullEntityComponentException(componentType.Name);

                    field.SetValue(component, injectedComponent);
                }
            }
            foreach (var prop in properties)
            {
                if (prop.GetCustomAttribute<EntityComponentAttribute>() != null)
                {
                    var componentType = prop.PropertyType;
                    var injectedComponent = getComponentMethod.MakeGenericMethod(componentType)
                        .Invoke(component.Entity, new object[] { });

                    if (injectedComponent == null)
                        throw new NullEntityComponentException(componentType.Name);

                    prop.SetValue(component, injectedComponent);
                }
            }
        }
    }
}
