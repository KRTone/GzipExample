namespace MultithreadedGZip.BLL.Interfaces.Configurators
{
    public interface IMultithreadedConfigurator
    {
        int Processors { get; }
        int BlockSize { get; }
    }
}
