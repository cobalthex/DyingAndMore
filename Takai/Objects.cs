namespace Takai
{
    public interface INamedObject : Data.ISerializeExternally
    {
        string Name { get; set; }
    }

    public interface IObjectClass<TInstance> : INamedObject
    {
        TInstance Instantiate();
    }

    public interface IObjectInstance<TClass>
    {
        TClass Class { get; set; }
    }
}
