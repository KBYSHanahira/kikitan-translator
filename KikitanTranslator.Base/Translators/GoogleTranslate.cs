using System.Net;
using KikitanTranslator.Utility;
using Newtonsoft.Json;
using Serilog;

namespace KikitanTranslator.Base.Translators;

internal class Sentence
{
    [JsonProperty("trans")] public string Translation = null!;
}

internal class Response : IDisposable
{
    [JsonProperty("sentences")] public Sentence[] Sentences = null!;


    public void Dispose()
    {
        
    }
}

public class GoogleTranslate : ITranslator
{
    private static readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

    private CurlImpersonate _curlImpersonate = new();

    public string? Translate(string text, string source, string target)
    {
        Log.Debug($"[GT]   Translation requested: chars={text.Length}, source={source}, target={target}");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={source}&tl={target}&dt=t&dt=bd&dj=1&q={Uri.EscapeDataString(text)}");

        try
        {
            using (HttpResponseMessage response = _httpClient.Send(request))
            {
                if (response.StatusCode != HttpStatusCode.OK) return TranslateWithCurlImpersonate(text, source, target);

                using (Stream stream = response.Content.ReadAsStream())
                using (StreamReader reader = new StreamReader(stream))
                using (Response? resp = JsonConvert.DeserializeObject<Response>(reader.ReadToEnd()))
                {
                    if (resp == null)
                    {
                        Log.Error($"[GT]   Response deserialization returned null");

                        return null;
                    }

                    var final = "";
                    foreach (var sentence in resp.Sentences)
                    {
                        final += $" {Uri.UnescapeDataString(sentence.Translation)}";
                    }

                    return final.Trim();
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"[GT]  Exception while translation via regular Google Translate API: {e}");
            
            return TranslateWithCurlImpersonate(text, source, target);
        }
    }
    
    
    private string? TranslateWithCurlImpersonate(string text, string source, string target)
    {
        Log.Debug($"[GT]   Trying curl fallback: chars={text.Length}, source={source}, target={target}");
        string? resp = _curlImpersonate.DoGet( $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={source}&tl={target}&dt=t&dt=bd&dj=1&q={Uri.EscapeDataString(text)}");
        if (resp == null)
        {
            Log.Error($"[GT]   Google Translate failed (via curl_impersonate)!");

            return null;
        }

        Response? r = JsonConvert.DeserializeObject<Response>(resp);
        if (r == null)
        {
            Log.Error($"[GT]   Response deserialization returned null");
                    
            return null;
        }

        var final = "";
        foreach (var sentence in r.Sentences)
        {
            final += $" {Uri.UnescapeDataString(sentence.Translation)}";
        }

        return final.Trim();
    }
    
    public void Dispose()
    {
        
    }
}
