namespace Takai
{
    public interface IObjectClass<TInstance> : Data.ISerializeExternally
    {
        string Name { get; set; }

        TInstance Create();
    }

    public interface IObjectInstance<TClass>
    {
        TClass Class { get; set; }
    }
}
