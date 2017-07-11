namespace Takai.Game
{
    public interface IObjectClass<TInstance>
    {
        TInstance Create();
    }

    public interface IObjectInstance<TClass>
    {
        TClass Class { get; set; }
    }
}
