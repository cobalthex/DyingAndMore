namespace Takai
{
    public interface INamedObject : Data.ISerializeExternally
    {
        string Name { get; set; }
    }

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
