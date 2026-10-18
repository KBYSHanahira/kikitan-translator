using Newtonsoft.Json;

namespace KikitanTranslator.Types.Bing
{
    public class ContextServiceTag
    {
        [JsonProperty("serviceTag")] public String ServiceTag = null!;
    } 

    public class TurnStart
    {
        [JsonProperty("context")] public ContextServiceTag Context = null!;
    }

    public class SpeechData : IDisposable
    {
        [JsonProperty("Text")] public string? Text;
        [JsonProperty("DisplayText")] public string? DisplayText;
        
        public void Dispose()
        {
            
        }
    }
}