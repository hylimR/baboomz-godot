using Godot;

namespace Baboomz
{
    public partial class AudioBridge
    {
        private static AudioStreamWav GenerateBuzz(float duration, float freq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];
            var rng = new System.Random(256);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1f - t) * Mathf.Clamp(t * 15f, 0f, 1f);
                float phase = 2f * Mathf.Pi * freq * i / SampleRate;
                float square = Mathf.Sin(phase) > 0f ? 1f : -1f;
                float harmonic = Mathf.Sin(phase * 3f) * 0.3f + Mathf.Sin(phase * 5f) * 0.15f;
                float crackle = ((float)rng.NextDouble() * 2f - 1f) * 0.2f * Mathf.Clamp(1f - t * 3f, 0f, 1f);
                float sample = (square * 0.4f + harmonic + crackle) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateBell(float duration, float freq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 3f) * Mathf.Clamp(t * 30f, 0f, 1f);
                float phase = 2f * Mathf.Pi * i / SampleRate;
                float fundamental = Mathf.Sin(phase * freq);
                float h2 = Mathf.Sin(phase * freq * 2.0f) * 0.5f;
                float h3 = Mathf.Sin(phase * freq * 3.0f) * 0.25f;
                float h5 = Mathf.Sin(phase * freq * 5.0f) * 0.1f;
                float sample = (fundamental + h2 + h3 + h5) * envelope * volume * 0.5f;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateDrill(float duration, float baseFreq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Clamp(t * 10f, 0f, 1f) * Mathf.Clamp(1f - (t - 0.7f) * 3.3f, 0f, 1f);
                float modulation = Mathf.Sin(2f * Mathf.Pi * 30f * i / SampleRate) * 0.5f + 0.5f;
                float freq = baseFreq + modulation * 200f;
                float saw = (2f * ((freq * i / SampleRate) % 1f) - 1f) * 0.6f;
                float whine = Mathf.Sin(2f * Mathf.Pi * (baseFreq * 4f) * i / SampleRate) * 0.2f;
                float sample = (saw + whine) * envelope * volume * modulation;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateBoing(float duration, float freq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1f - t) * Mathf.Clamp(t * 20f, 0f, 1f);
                float wobble = Mathf.Sin(2f * Mathf.Pi * 12f * t) * 80f * (1f - t);
                float currentFreq = freq + wobble;
                float sample = Mathf.Sin(2f * Mathf.Pi * currentFreq * i / SampleRate) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }
    }
}
