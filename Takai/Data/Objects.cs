namespace Takai.Data
{
    public interface IReferenceable
    {
        string Name { get; set; }
    }

    public interface INamedObject : IReferenceable, ISerializeExternally { }

    public interface IClass<TInstance>
    {
        TInstance Instantiate();
    }

    public interface INamedClass<TInstance> : IClass<TInstance>, INamedObject { }

    public interface IInstance<TClass>
    {
        TClass Class { get; set; }
    }
}
