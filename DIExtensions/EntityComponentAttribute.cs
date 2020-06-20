using System;

namespace DIExtensions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class EntityComponentAttribute : Attribute
    {
    }


    [Serializable]
    public class NullEntityComponentException : Exception
    {
        public NullEntityComponentException() { }
        public NullEntityComponentException(string componentType)
            : base($"This component requires the entity to also have {componentType}") { }
    }
}
