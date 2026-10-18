namespace KikitanTranslator.Photino.Handlers;

public class ManualTranslate(Manager manager) : IHandler
{
    public Task<string?> OnDataReceived(string data)
    {
        manager.ManualTranslate(data);
        
        return Task.FromResult<string?>("");
    }
}