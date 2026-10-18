namespace KikitanTranslator.Photino.Handlers;

public class Quit : IHandler
{
    public Task<string?> OnDataReceived(string data)
    {
        Environment.Exit(0);

        return Task.FromResult<string?>(null);
    }
}