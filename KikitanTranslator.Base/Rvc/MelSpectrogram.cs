namespace KikitanTranslator.Base.Rvc;

/// <summary>
/// Log-mel spectrogram matching RVC's RMVPE front end (rmvpe.py MelSpectrogram(128, 16000, 1024, 160, None, 30, 8000)):
/// torch.stft with center=True (reflect padding) and a periodic Hann window, magnitude, a librosa mel filterbank
/// (htk=True, Slaney normalisation), then log(clamp(x, 1e-5)). RMVPE's pitch output is only as good as this match.
/// </summary>
public static class MelSpectrogram
{
    public const int SampleRate = 16000;
    public const int MelBins = 128;
    public const int FftSize = 1024;
    public const int HopLength = 160;
    private const double MinFrequency = 30;
    private const double MaxFrequency = 8000;
    private const float Clamp = 1e-5f;

    private static readonly float[] Window = CreateWindow();
    private static readonly float[][] Filters = CreateFilterbank();

    /// <summary>Returns [MelBins][frames] for 16 kHz mono audio.</summary>
    public static float[][] Compute(float[] audio)
    {
        var pad = FftSize / 2;
        var padded = ReflectPad(audio, pad);
        var frames = 1 + audio.Length / HopLength;
        var bins = FftSize / 2 + 1;

        var mel = new float[MelBins][];
        for (var m = 0; m < MelBins; m++) mel[m] = new float[frames];

        var re = new double[FftSize];
        var im = new double[FftSize];
        var magnitude = new double[bins];

        for (var t = 0; t < frames; t++)
        {
            var start = t * HopLength;
            for (var i = 0; i < FftSize; i++)
            {
                re[i] = padded[start + i] * Window[i];
                im[i] = 0;
            }

            Fft(re, im);

            for (var k = 0; k < bins; k++) magnitude[k] = Math.Sqrt(re[k] * re[k] + im[k] * im[k]);

            for (var m = 0; m < MelBins; m++)
            {
                var filter = Filters[m];
                double sum = 0;
                for (var k = 0; k < bins; k++) sum += filter[k] * magnitude[k];

                mel[m][t] = MathF.Log(Math.Max((float)sum, Clamp));
            }
        }

        return mel;
    }

    // numpy/torch "reflect": the edge sample itself is not repeated. Short input is reflected back and forth.
    private static float[] ReflectPad(float[] audio, int pad)
    {
        var result = new float[audio.Length + 2 * pad];
        var n = audio.Length;

        for (var i = 0; i < result.Length; i++)
        {
            var j = i - pad;
            if (n == 1) j = 0;
            else
            {
                var period = 2 * (n - 1);
                j = ((j % period) + period) % period;
                if (j >= n) j = period - j;
            }

            result[i] = audio[j];
        }

        return result;
    }

    // torch.hann_window default (periodic=True)
    private static float[] CreateWindow()
    {
        var window = new float[FftSize];
        for (var i = 0; i < FftSize; i++) window[i] = (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / FftSize));

        return window;
    }

    // librosa.filters.mel(sr=16000, n_fft=1024, n_mels=128, fmin=30, fmax=8000, htk=True, norm="slaney")
    private static float[][] CreateFilterbank()
    {
        var bins = FftSize / 2 + 1;
        var fftFrequencies = new double[bins];
        for (var k = 0; k < bins; k++) fftFrequencies[k] = (double)k * SampleRate / FftSize;

        var minMel = HzToMel(MinFrequency);
        var maxMel = HzToMel(MaxFrequency);
        var melFrequencies = new double[MelBins + 2];
        for (var i = 0; i < melFrequencies.Length; i++)
            melFrequencies[i] = MelToHz(minMel + (maxMel - minMel) * i / (MelBins + 1));

        var filters = new float[MelBins][];
        for (var m = 0; m < MelBins; m++)
        {
            filters[m] = new float[bins];
            var lowerWidth = melFrequencies[m + 1] - melFrequencies[m];
            var upperWidth = melFrequencies[m + 2] - melFrequencies[m + 1];
            var norm = 2.0 / (melFrequencies[m + 2] - melFrequencies[m]);

            for (var k = 0; k < bins; k++)
            {
                var lower = (fftFrequencies[k] - melFrequencies[m]) / lowerWidth;
                var upper = (melFrequencies[m + 2] - fftFrequencies[k]) / upperWidth;
                filters[m][k] = (float)(Math.Max(0, Math.Min(lower, upper)) * norm);
            }
        }

        return filters;
    }

    private static double HzToMel(double hz) => 2595 * Math.Log10(1 + hz / 700);
    private static double MelToHz(double mel) => 700 * (Math.Pow(10, mel / 2595) - 1);

    // In-place iterative radix-2 FFT; FftSize is a power of two.
    private static void Fft(double[] re, double[] im)
    {
        var n = re.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;

            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var wRe = Math.Cos(angle);
            var wIm = Math.Sin(angle);

            for (var i = 0; i < n; i += length)
            {
                double curRe = 1, curIm = 0;
                for (var k = 0; k < length / 2; k++)
                {
                    var a = i + k;
                    var b = a + length / 2;
                    var tRe = re[b] * curRe - im[b] * curIm;
                    var tIm = re[b] * curIm + im[b] * curRe;

                    re[b] = re[a] - tRe;
                    im[b] = im[a] - tIm;
                    re[a] += tRe;
                    im[a] += tIm;

                    var nextRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = nextRe;
                }
            }
        }
    }
}
