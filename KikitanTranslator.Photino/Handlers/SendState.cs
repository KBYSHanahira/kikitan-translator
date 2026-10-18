namespace KikitanTranslator.Photino.Handlers;

public class SendState(Manager manager) : IHandler
{
    public Task<string?> OnDataReceived(string data)
    {
        manager.SendUpdateToUI();

        return Task.FromResult<string?>("");
    }
}