namespace Takai
{
    public interface IObjectClass<TInstance> : Data.ISerializeExternally
    {
        string Name { get; set; }

        TInstance Instantiate();
    }

    public interface IObjectInstance<TClass>
    {
        TClass Class { get; set; }
    }
}
