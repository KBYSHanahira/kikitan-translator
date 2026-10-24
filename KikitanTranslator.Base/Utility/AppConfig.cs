using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Serilog;

namespace KikitanTranslator.Utility;

public class ConfigObject : INotifyPropertyChanged
{
    [JsonProperty("quickstart_viewed")] private bool _quickstartViewed;

    [JsonIgnore]
    public bool QuickstartViewed
    {
        get => _quickstartViewed;
        set
        {
            if (_quickstartViewed != value)
            {
                _quickstartViewed = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonProperty("language")] private string _language = "en";

    [JsonIgnore]
    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged();
            }
        }
    }
    [JsonProperty("last_version")] private string _lastVersion = "en";

    [JsonIgnore]
    public string LastVersion
    {
        get => _lastVersion;
        set
        {
            if (_lastVersion != value)
            {
                _lastVersion = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonProperty("source_language")] private string _sourceLanguage = "en";

    [JsonIgnore]
    public string SourceLanguage
    {
        get => _sourceLanguage;
        set
        {
            if (_sourceLanguage != value)
            {
                _sourceLanguage = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("target_language")] private string _targetLanguage = "ja";

    [JsonIgnore]
    public string TargetLanguage
    {
        get => _targetLanguage;
        set
        {
            if (_targetLanguage != value)
            {
                _targetLanguage = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("light_mode")] private bool _lightMode = false;

    [JsonIgnore]
    public bool LightMode
    {
        get => _lightMode;
        set
        {
            if (_lightMode != value)
            {
                _lightMode = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("speech_to_text_only")] private bool _speechToTextOnly = false;

    [JsonIgnore]
    public bool SpeechToTextOnly
    {
        get => _speechToTextOnly;
        set
        {
            if (_speechToTextOnly != value)
            {
                _speechToTextOnly = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("microphone")] private string _microphone = string.Empty;

    [JsonIgnore]
    public string Microphone
    {
        get => _microphone;
        set
        {
            if (_microphone != value)
            {
                _microphone = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("translation_only")] private bool _translationOnly = false;

    [JsonIgnore]
    public bool TranslationOnly
    {
        get => _translationOnly;
        set
        {
            if (_translationOnly != value)
            {
                _translationOnly = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("disable_when_muted")] private bool _disableWhenMuted = false;

    [JsonIgnore]
    public bool DisableWhenMuted
    {
        get => _disableWhenMuted;
        set
        {
            if (_disableWhenMuted != value)
            {
                _disableWhenMuted = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("chatbox_wait_per_char_ms")] private int _chatboxWaitPerCharMs = 30;

    [JsonIgnore]
    public int ChatboxWaitPerCharMs
    {
        get => _chatboxWaitPerCharMs;
        set
        {
            if (_chatboxWaitPerCharMs != value)
            {
                _chatboxWaitPerCharMs = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("osc_port")] private int _oscPort = 9000;

    [JsonIgnore]
    public int OscPort
    {
        get => _oscPort;
        set
        {
            if (_oscPort != value)
            {
                _oscPort = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("send_to_chatbox")] private bool _sendToChatbox = true;

    [JsonIgnore]
    public bool SendToChatbox
    {
        get => _sendToChatbox;
        set
        {
            if (_sendToChatbox != value)
            {
                _sendToChatbox = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("send_user_data")] private bool _sendUserData = false;

    [JsonIgnore]
    public bool SendUserData
    {
        get => _sendUserData;
        set
        {
            if (_sendUserData != value)
            {
                _sendUserData = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("recognizer")] private int _recognizer = 0;

    [JsonIgnore]
    public int Recognizer
    {
        get => _recognizer;
        set
        {
            if (_recognizer != value)
            {
                _recognizer = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("translator")] private int _translator = 0;

    [JsonIgnore]
    public int Translator
    {
        get => _translator;
        set
        {
            if (_translator != value)
            {
                _translator = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("desktop_translation")] private bool _desktopTranslation = false;

    [JsonIgnore]
    public bool DesktopTranslation
    {
        get => _desktopTranslation;
        set
        {
            if (_desktopTranslation != value)
            {
                _desktopTranslation = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonProperty("send_without_waiting_for_finish")] private bool _sendWithoutWaitingForFinish = false;

    [JsonIgnore]
    public bool SendWithoutWaitingForFinish
    {
        get => _sendWithoutWaitingForFinish;
        set
        {
            if (_sendWithoutWaitingForFinish != value)
            {
                _sendWithoutWaitingForFinish = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonProperty("groq_api_key")] private string _groqApiKey = "";

    [JsonIgnore]
    public string GroqApiKey
    {
        get => _groqApiKey;
        set
        {
            if (_groqApiKey != value)
            {
                _groqApiKey = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonProperty("gemini_api_key")] private string _geminiApiKey = "";

    [JsonIgnore]
    public string GeminiApiKey
    {
        get => _geminiApiKey;
        set
        {
            if (_geminiApiKey != value)
            {
                _geminiApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("tts_enabled")] private bool _ttsEnabled = false;

    [JsonIgnore]
    public bool TtsEnabled
    {
        get => _ttsEnabled;
        set
        {
            if (_ttsEnabled != value)
            {
                _ttsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    // Playback device the speech is played on; empty means the system default
    [JsonProperty("tts_device")] private string _ttsDevice = string.Empty;

    [JsonIgnore]
    public string TtsDevice
    {
        get => _ttsDevice;
        set
        {
            if (_ttsDevice != value)
            {
                _ttsDevice = value;
                OnPropertyChanged();
            }
        }
    }

    // SAPI token ID of the preferred Windows voice, or "google". Empty, or a voice for another language,
    // means the first Windows voice for the spoken language, then Google.
    [JsonProperty("tts_voice")] private string _ttsVoice = string.Empty;

    [JsonIgnore]
    public string TtsVoice
    {
        get => _ttsVoice;
        set
        {
            if (_ttsVoice != value)
            {
                _ttsVoice = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonProperty("rvc_enabled")] private bool _rvcEnabled = false;

    [JsonIgnore]
    public bool RvcEnabled
    {
        get => _rvcEnabled;
        set
        {
            if (_rvcEnabled != value)
            {
                _rvcEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    // RVC voice model exported to ONNX with RVC WebUI
    [JsonProperty("rvc_model_path")] private string _rvcModelPath = string.Empty;

    [JsonIgnore]
    public string RvcModelPath
    {
        get => _rvcModelPath;
        set
        {
            if (_rvcModelPath != value)
            {
                _rvcModelPath = value;
                OnPropertyChanged();
            }
        }
    }

    // Run RVC on the GPU through DirectML (faster) or on the CPU (leaves the GPU alone)
    [JsonProperty("rvc_use_gpu")] private bool _rvcUseGpu = true;

    [JsonIgnore]
    public bool RvcUseGpu
    {
        get => _rvcUseGpu;
        set
        {
            if (_rvcUseGpu != value)
            {
                _rvcUseGpu = value;
                OnPropertyChanged();
            }
        }
    }

    // Pitch shift in semitones applied before conversion, e.g. +12 for a male TTS voice into a female model
    [JsonProperty("rvc_transpose")] private int _rvcTranspose = 0;

    [JsonIgnore]
    public int RvcTranspose
    {
        get => _rvcTranspose;
        set
        {
            if (_rvcTranspose != value)
            {
                _rvcTranspose = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public delegate void OnConfigUpdate();

public static class AppConfig
{
    public static ConfigObject ConfigObject = null!;
    public static event OnConfigUpdate? OnUpdate;
    private static string _currentConfigPath = null!;

    public static string GetAppFolder()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(basePath, "Kikitan Translator");
        Directory.CreateDirectory(appFolder);

        return appFolder;
    }

    public static void Load() => Load(Path.Join(GetAppFolder(), "config.json"));

    public static void Load(string configPath)
    {
        if (!Path.Exists(configPath))
        {
            Log.Warning("[CFG]  Specified path is nonexistent (perhaps first launch?). Using the default configuration");

            ConfigObject = new ConfigObject();
            ConfigObject.PropertyChanged += OnConfigPropertyChanged;
            _currentConfigPath = configPath;
            
            SaveConfig();

            return;
        }

        try
        {
            ConfigObject? cfg = JsonConvert.DeserializeObject<ConfigObject>(File.ReadAllText(configPath));
            if (cfg == null)
            {
                Log.Error("[CFG]  Deserialization result returned null!");

                return;
            }

            ConfigObject = cfg;
            ConfigObject.PropertyChanged += OnConfigPropertyChanged;
            _currentConfigPath = configPath;
            
            Log.Information($"[CFG]  Loaded from {configPath}");
        }
        catch (Exception e)
        {
            Log.Error($"[CFG]  Error occured while trying to load the config file!: {e}");

            return;
        }
    }
    
    public static void SaveConfig() {
        File.WriteAllText(_currentConfigPath, JsonConvert.SerializeObject(ConfigObject, Formatting.Indented));
        
        Log.Verbose($"[CFG]  Saved to {_currentConfigPath}");
    }

    private static void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveConfig();
        
        OnUpdate?.Invoke();   
    }
}