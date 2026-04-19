using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    public partial class AudioBridge
    {
        private void ProcessCallouts()
        {
            // Kill combo callouts from ComboEvents
            foreach (var evt in _state.ComboEvents)
            {
                switch (evt.Type)
                {
                    case ComboType.DoubleKill:
                        PlayClip(_doubleKillClip, 0.55f);
                        break;
                    case ComboType.MultiKill:
                        PlayClip(_megaKillClip, 0.65f);
                        break;
                    case ComboType.TripleHit:
                    case ComboType.QuadHit:
                    case ComboType.Unstoppable:
                        PlayClip(_tripleKillClip, 0.6f);
                        break;
                }
            }

            // Match phase transition callouts
            if (_state.Phase != _lastPhase)
            {
                if (_lastPhase == MatchPhase.Waiting && _state.Phase == MatchPhase.Playing)
                {
                    PlayClip(_matchStartFanfareClip, 0.5f);
                }
                else if (_lastPhase == MatchPhase.Playing && _state.Phase == MatchPhase.Ended)
                {
                    // Victory if player 0 (human) won, defeat otherwise
                    bool isVictory = _state.WinnerIndex == 0;
                    PlayClip(isVictory ? _matchEndVictoryClip : _matchEndDefeatClip, 0.55f);
                }
                _lastPhase = _state.Phase;
            }

            // Last-standing tension cue (2 players remaining)
            int aliveNow = CountAlivePlayers();
            if (aliveNow == 2 && _lastAliveCount > 2 && _state.Players.Length > 2)
            {
                PlayClip(_lastStandingClip, 0.5f);
            }
            _lastAliveCount = aliveNow;
        }

        private int CountAlivePlayers()
        {
            int count = 0;
            for (int i = 0; i < _state.Players.Length; i++)
            {
                if (!_state.Players[i].IsDead)
                    count++;
            }
            return count;
        }

        private void GenerateCalloutClips()
        {
            _doubleKillClip = GenerateKillAnnounce(2);
            _tripleKillClip = GenerateKillAnnounce(3);
            _megaKillClip = GenerateKillAnnounce(4);
            _matchStartFanfareClip = GenerateFanfare(true);
            _matchEndVictoryClip = GenerateFanfare(false);
            _matchEndDefeatClip = GenerateDefeatStinger();
            _lastStandingClip = GenerateTensionCue();
        }

        private static AudioStreamWav GenerateKillAnnounce(int killCount)
        {
            float duration = 0.15f + killCount * 0.08f;
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            float baseFreq = 400f + (killCount - 2) * 200f;
            int noteCount = killCount;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                int noteIndex = (int)(t * noteCount);
                if (noteIndex >= noteCount) noteIndex = noteCount - 1;
                float noteT = (t * noteCount) - noteIndex;

                float freq = baseFreq + noteIndex * 120f;
                float envelope = (1f - noteT * 0.6f) * Mathf.Clamp(noteT * 15f, 0f, 1f);
                float fundamental = Mathf.Sin(2f * Mathf.Pi * freq * i / SampleRate);
                float harmonic = Mathf.Sin(
                    2f * Mathf.Pi * freq * 1.5f * i / SampleRate) * 0.25f;
                float sample = (fundamental + harmonic) * envelope * 0.45f;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateFanfare(bool ascending)
        {
            float duration = 0.5f;
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            float[] freqs = ascending
                ? new[] { 523.3f, 659.3f, 784.0f }
                : new[] { 784.0f, 659.3f, 523.3f };
            float noteLen = duration / 3f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Min((int)(t / noteLen), 2);
                float noteT = (t - noteIndex * noteLen) / noteLen;

                float freq = freqs[noteIndex];
                float envelope = (1f - noteT * 0.4f) * Mathf.Clamp(noteT * 20f, 0f, 1f);
                float fundamental = Mathf.Sin(2f * Mathf.Pi * freq * t);
                float harmonic = Mathf.Sin(
                    2f * Mathf.Pi * freq * 2f * t) * 0.2f;
                float sample = (fundamental + harmonic) * envelope * 0.4f;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateDefeatStinger()
        {
            float duration = 0.6f;
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            float[] freqs = { 622.3f, 523.3f, 415.3f };
            float noteLen = duration / 3f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Min((int)(t / noteLen), 2);
                float noteT = (t - noteIndex * noteLen) / noteLen;

                float freq = freqs[noteIndex];
                float envelope = (1f - noteT * 0.5f) * Mathf.Clamp(noteT * 15f, 0f, 1f);
                float sample = Mathf.Sin(2f * Mathf.Pi * freq * t) * envelope * 0.4f;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }

        private static AudioStreamWav GenerateTensionCue()
        {
            float duration = 0.8f;
            int samples = (int)(duration * SampleRate);
            var data = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env1 = Envelope(t, 0.15f, 0.1f);
                float env2 = Envelope(t, 0.5f, 0.12f) * 0.8f;
                float envelope = Mathf.Max(env1, env2);

                float freq = 80f + 10f * Mathf.Sin(2f * Mathf.Pi * 3f * t);
                float sample = Mathf.Sin(2f * Mathf.Pi * freq * t) * envelope * 0.45f;

                short pcm = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                data[i * 2] = (byte)(pcm & 0xFF);
                data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return CreateWav(data, samples);
        }
    }
}
