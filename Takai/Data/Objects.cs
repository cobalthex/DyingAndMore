namespace Takai.Data
{
    /// <summary>
    /// Allows a class to be universally referenced by name.
    /// Rules on uniqueness/case-sensitivity/etc are not defined here
    /// </summary>
    public interface IReferenceable
    {
        string Name { get; set; }
    }

    /// <summary>
    /// An object that is both referencable by name and can be serialized to a file
    /// </summary>
    public interface INamedObject : IReferenceable, ISerializeExternally { }

    /// <summary>
    /// Non-generic version of IClass for generic constraints
    /// </summary>
    public interface IClassBase { }

    /// <summary>
    /// Defines a class of objects that can be instantiated from it
    /// </summary>
    /// <typeparam name="TInstance">The instance type that this class can create</typeparam>
    public interface IClass<TInstance> : IClassBase
    {
        TInstance Instantiate();
    }

    /// <summary>
    /// A <see cref="IClass{TInstance}"/> that has a name and can be serialized to a file
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    public interface INamedClass<TInstance> : IClass<TInstance>, INamedObject { }

    /// <summary>
    /// Defines the instance of a particular <see cref="TClass"/>
    /// </summary>
    /// <typeparam name="TClass">The class type this belongs to</typeparam>
    public interface IInstance<TClass>
        where TClass : IClassBase
    {
        TClass Class { get; set; }
    }
}
