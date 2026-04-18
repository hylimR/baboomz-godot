using Godot;

namespace Baboomz
{
    /// <summary>
    /// Procedural audio synthesis helpers for AudioBridge.
    /// Generates all sound clips (SFX + BGM) from code — no external audio files.
    /// </summary>
    public partial class AudioBridge
    {
        private void GenerateClips()
        {
            _fireClip = GenerateTone(0.15f, 220f, 440f, 0.4f);
            _explosionClip = GenerateNoise(0.4f, 0.6f);
            _hitClip = GenerateTone(0.1f, 800f, 400f, 0.3f);
            _jumpClip = GenerateTone(0.08f, 300f, 600f, 0.2f);
            _switchClip = GenerateTone(0.05f, 500f, 500f, 0.15f);
            _hitTickClip = GenerateTone(0.05f, 1200f, 900f, 0.35f);
            _heartbeatClip = GenerateHeartbeat(0.6f, 0.5f);

            // Per-weapon fire sounds (#175)
            _fireCannonClip = GenerateTone(0.18f, 110f, 220f, 0.5f);   // deep bass thump
            _fireRocketClip = GenerateWhoosh(0.25f, 180f, 400f, 0.45f); // mid whoosh + sweep
            _fireSniperClip = GenerateTone(0.08f, 1400f, 700f, 0.5f);  // sharp crack
            _fireTossClip = GenerateTone(0.12f, 350f, 250f, 0.3f);     // soft toss arc
            _fireSpecialClip = GenerateTone(0.15f, 600f, 900f, 0.4f);  // unique rising chirp

            // Per-skill activation sounds (#175)
            _skillTeleportClip = GenerateTone(0.1f, 1800f, 600f, 0.4f);  // high-pitched pop
            _skillDashClip = GenerateWhoosh(0.12f, 400f, 800f, 0.35f);   // quick whoosh
            _skillShieldClip = GenerateTone(0.15f, 800f, 1200f, 0.35f);  // metallic pong
            _skillQuakeClip = GenerateNoise(0.3f, 0.4f);                 // low rumble
            _skillHealClip = GenerateChime(0.2f, 880f, 0.3f);            // soft chime
            _skillPowerClip = GenerateTone(0.25f, 200f, 600f, 0.35f);    // power-up hum
        }

        /// <summary>
        /// Generates a sine tone that sweeps from startFreq to endFreq with linear decay.
        /// </summary>
        private static AudioStreamWav GenerateTone(
            float duration, float startFreq, float endFreq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2]; // 16-bit mono

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = 1f - t; // linear fade out
                float sample = Mathf.Sin(
                    2f * Mathf.Pi * freq * i / SampleRate) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        /// <summary>
        /// Generates a noise burst with exponential decay envelope.
        /// Uses a simple low-pass (averaging with previous sample) for a warmer boom.
        /// </summary>
        private static AudioStreamWav GenerateNoise(float duration, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var pcmData = new float[samples];

            var rng = new System.Random(42);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1f - t) * (1f - t); // quadratic decay
                float noise = (float)rng.NextDouble() * 2f - 1f;
                // Simple low-pass: average with previous sample
                if (i > 0) noise = (noise + pcmData[i - 1]) * 0.5f;
                pcmData[i] = noise * envelope * volume;
            }

            var data = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                short pcm = (short)(Mathf.Clamp(pcmData[i], -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        /// <summary>
        /// Generates a two-beat heartbeat thump for low-HP warning (#36).
        /// Each beat is a very low sine (~60 Hz) with a sharp envelope.
        /// </summary>
        private static AudioStreamWav GenerateHeartbeat(float duration, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            // Two beats spaced ~0.25s apart.
            float beatCenter1 = 0.08f;
            float beatCenter2 = 0.28f;
            float beatWidth = 0.07f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Envelope: two Gaussian-ish bumps centered on beatCenter1/beatCenter2.
                float env1 = Envelope(t, beatCenter1, beatWidth);
                float env2 = Envelope(t, beatCenter2, beatWidth) * 0.75f;
                float envelope = Mathf.Max(env1, env2);

                // ~60 Hz thump with slight downward sweep for a "pulse" feel.
                float freq = 60f + 20f * envelope;
                float sample = Mathf.Sin(2f * Mathf.Pi * freq * t) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        /// <summary>
        /// Generates a whoosh sound: filtered noise layered with a frequency sweep.
        /// Used for rocket trails and dash skills (#175).
        /// </summary>
        private static AudioStreamWav GenerateWhoosh(
            float duration, float startFreq, float endFreq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];
            var rng = new System.Random(123);
            float prev = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = (1f - t) * Mathf.Clamp(t * 8f, 0f, 1f); // attack + decay
                float tone = Mathf.Sin(2f * Mathf.Pi * freq * i / SampleRate) * 0.5f;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                noise = (noise + prev) * 0.5f; // low-pass
                prev = noise;
                float sample = (tone + noise * 0.5f) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        /// <summary>
        /// Generates a bright chime: a sine with harmonics and quick attack/decay.
        /// Used for heal/mend skill activation (#175).
        /// </summary>
        private static AudioStreamWav GenerateChime(float duration, float freq, float volume)
        {
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1f - t) * Mathf.Clamp(t * 20f, 0f, 1f);
                float fundamental = Mathf.Sin(2f * Mathf.Pi * freq * i / SampleRate);
                float harmonic = Mathf.Sin(2f * Mathf.Pi * freq * 2f * i / SampleRate) * 0.3f;
                float sample = (fundamental + harmonic) * envelope * volume;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static float Envelope(float t, float center, float width)
        {
            float d = (t - center) / width;
            return Mathf.Exp(-d * d * 4f);
        }

        private static AudioStreamWav CreateWav(byte[] pcmData, int sampleCount)
        {
            var wav = new AudioStreamWav();
            wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
            wav.MixRate = SampleRate;
            wav.Stereo = false;
            wav.Data = pcmData;
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
            return wav;
        }

        /// <summary>
        /// Generates a ~16-second looping ambient track.
        /// Combines a low drone with pentatonic melody notes.
        /// </summary>
        private static AudioStreamWav GenerateBackgroundTrack()
        {
            float duration = 16f;
            int samples = (int)(duration * SampleRate);
            var pcmData = new float[samples];

            // Pentatonic scale (C minor pentatonic): C3, Eb3, F3, G3, Bb3
            float[] melodyFreqs = { 130.8f, 155.6f, 174.6f, 196.0f, 233.1f };
            var rng = new System.Random(777);

            float noteLength = SampleRate * 0.8f; // ~0.8 seconds per note
            int currentNote = 0;
            float nextNoteAt = 0;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Low drone (C2 = 65.4 Hz) with gentle vibrato
                float drone = Mathf.Sin(2f * Mathf.Pi * 65.4f * t
                    + 0.5f * Mathf.Sin(2f * Mathf.Pi * 0.3f * t)) * 0.3f;

                // Melody note (changes periodically)
                if (i >= nextNoteAt)
                {
                    currentNote = rng.Next(0, melodyFreqs.Length);
                    nextNoteAt = i + noteLength
                        + (float)(rng.NextDouble() * SampleRate * 0.4f);
                }
                float noteT = (i - (nextNoteAt - noteLength)) / noteLength;
                // Attack + decay envelope
                float noteEnv = Mathf.Clamp(1f - noteT, 0f, 1f)
                    * Mathf.Clamp(noteT * 10f, 0f, 1f);
                float melody = Mathf.Sin(
                    2f * Mathf.Pi * melodyFreqs[currentNote] * t) * noteEnv * 0.15f;

                // Soft pad (fifth above drone)
                float pad = Mathf.Sin(2f * Mathf.Pi * 98f * t) * 0.08f;

                pcmData[i] = Mathf.Clamp(drone + melody + pad, -1f, 1f);
            }

            var data = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                short pcm = (short)(Mathf.Clamp(pcmData[i], -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            var wav = new AudioStreamWav();
            wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
            wav.MixRate = SampleRate;
            wav.Stereo = false;
            wav.Data = data;
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wav.LoopBegin = 0;
            wav.LoopEnd = samples;
            return wav;
        }

    }
}
