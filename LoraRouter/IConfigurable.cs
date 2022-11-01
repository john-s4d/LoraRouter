namespace LoraRouter
{
    public interface IConfigurable<T> 
        where T : ConfigBase
    {
        T Config { get; set; }
    }
}