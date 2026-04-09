using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Plays procedurally generated sound effects based on GameState events.
    /// No external audio files needed — all sounds are synthesized at Init time.
    /// Port of Unity AudioBridge to Godot using AudioStreamWav buffers.
    /// </summary>
    public partial class AudioBridge : Node
    {
        private GameState _state;
        private AudioStreamPlayer _sfxPlayer;
        private AudioStreamPlayer _musicPlayer;

        // Cached procedural clips
        private AudioStreamWav _fireClip;
        private AudioStreamWav _explosionClip;
        private AudioStreamWav _hitClip;
        private AudioStreamWav _jumpClip;
        private AudioStreamWav _switchClip;
        private AudioStreamWav _hitTickClip;

        private int _lastWeaponSlot = -1;
        private bool _lastJumpState;

        private const int SampleRate = 44100;

        public void Init(GameState state)
        {
            _state = state;

            _sfxPlayer = new AudioStreamPlayer();
            _sfxPlayer.Name = "SfxPlayer";
            _sfxPlayer.Bus = "Master";
            AddChild(_sfxPlayer);

            GenerateClips();
            StartBackgroundMusic();

            ProcessPriority = 80; // After renderers
        }

        private void GenerateClips()
        {
            _fireClip = GenerateTone(0.15f, 220f, 440f, 0.4f);
            _explosionClip = GenerateNoise(0.4f, 0.6f);
            _hitClip = GenerateTone(0.1f, 800f, 400f, 0.3f);
            _jumpClip = GenerateTone(0.08f, 300f, 600f, 0.2f);
            _switchClip = GenerateTone(0.05f, 500f, 500f, 0.15f);
            _hitTickClip = GenerateTone(0.05f, 1200f, 900f, 0.35f);
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            if (_state.Players == null || _state.Players.Length == 0) return;

            // Explosion sounds
            foreach (var evt in _state.ExplosionEvents)
            {
                float vol = Mathf.Clamp(evt.Radius * 0.2f, 0.3f, 1f);
                PlayClip(_explosionClip, vol);
            }

            // Damage hit sounds
            foreach (var evt in _state.DamageEvents)
            {
                float vol = Mathf.Clamp(evt.Amount * 0.02f, 0.2f, 0.5f);
                PlayClip(_hitClip, vol);
            }

            // Player 0 weapon switch
            ref PlayerState p = ref _state.Players[0];
            if (p.ActiveWeaponSlot != _lastWeaponSlot && _lastWeaponSlot >= 0)
            {
                PlayClip(_switchClip, 0.3f);
            }
            _lastWeaponSlot = p.ActiveWeaponSlot;

            // Jump sound
            if (!p.IsGrounded && _lastJumpState && p.Velocity.y > 0)
            {
                PlayClip(_jumpClip, 0.2f);
            }
            _lastJumpState = p.IsGrounded;
        }

        /// <summary>
        /// Plays a short metallic tick for hit confirmation markers.
        /// Pitch increases with damage for stronger feedback.
        /// </summary>
        public void PlayHitTick(float damage)
        {
            float pitch = damage >= 60f ? 1.2f : damage >= 30f ? 1.1f : 1.0f;
            PlayClipPitched(_hitTickClip, 0.4f, pitch);
        }

        /// <summary>
        /// Called by GameRunner when a projectile is spawned.
        /// </summary>
        public void OnProjectileFired()
        {
            PlayClip(_fireClip, 0.4f);
        }

        private void PlayClip(AudioStreamWav clip, float volume)
        {
            if (clip == null) return;
            var player = new AudioStreamPlayer();
            player.Stream = clip;
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.Bus = "Master";
            AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }

        private void PlayClipPitched(AudioStreamWav clip, float volume, float pitch)
        {
            if (clip == null) return;
            var player = new AudioStreamPlayer();
            player.Stream = clip;
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.PitchScale = pitch;
            player.Bus = "Master";
            AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }

        // --- Procedural audio generation ---

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

        // --- Background music ---

        private void StartBackgroundMusic()
        {
            _musicPlayer = new AudioStreamPlayer();
            _musicPlayer.Name = "MusicPlayer";
            _musicPlayer.Bus = "Master";
            _musicPlayer.VolumeDb = Mathf.LinearToDb(0.15f); // quiet ambient
            AddChild(_musicPlayer);

            var clip = GenerateBackgroundTrack();
            _musicPlayer.Stream = clip;
            _musicPlayer.Play();
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
