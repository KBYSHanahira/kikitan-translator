namespace KikitanTranslator.Base.Rvc;

public static class Resampler
{
    // Sinc zero crossings on each side of the filter centre; 16 keeps aliasing well below audible levels.
    private const int ZeroCrossings = 16;

    /// <summary>
    /// Resamples mono audio with a Blackman-windowed sinc filter. Output sample n sits exactly at input time
    /// n * fromRate / toRate, so the result has no delay against the input.
    /// </summary>
    public static float[] Resample(float[] input, int fromRate, int toRate)
    {
        if (fromRate == toRate) return (float[])input.Clone();

        var ratio = (double)fromRate / toRate;
        // Low-pass at 95% of the lower Nyquist frequency, in cycles per input sample.
        var cutoff = 0.5 * Math.Min(1.0, 1.0 / ratio) * 0.95;
        var halfWidth = ZeroCrossings / (2 * cutoff);

        var output = new float[(int)((long)input.Length * toRate / fromRate)];
        for (var n = 0; n < output.Length; n++)
        {
            var center = n * ratio;
            var first = (int)Math.Ceiling(center - halfWidth);
            var last = (int)Math.Floor(center + halfWidth);

            double sum = 0;
            for (var k = Math.Max(first, 0); k <= Math.Min(last, input.Length - 1); k++)
            {
                var x = k - center;
                var window = 0.42 + 0.5 * Math.Cos(Math.PI * x / halfWidth) + 0.08 * Math.Cos(2 * Math.PI * x / halfWidth);
                sum += input[k] * 2 * cutoff * Sinc(2 * cutoff * x) * window;
            }

            output[n] = (float)sum;
        }

        return output;
    }

    private static double Sinc(double x) => x == 0 ? 1 : Math.Sin(Math.PI * x) / (Math.PI * x);
}
