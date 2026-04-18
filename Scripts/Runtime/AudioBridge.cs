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
        private AudioStreamWav _heartbeatClip;

        // Per-weapon fire sounds (#175)
        private AudioStreamWav _fireCannonClip;
        private AudioStreamWav _fireRocketClip;
        private AudioStreamWav _fireSniperClip;
        private AudioStreamWav _fireTossClip;
        private AudioStreamWav _fireSpecialClip;

        // Per-skill activation sounds (#175)
        private AudioStreamWav _skillTeleportClip;
        private AudioStreamWav _skillDashClip;
        private AudioStreamWav _skillShieldClip;
        private AudioStreamWav _skillQuakeClip;
        private AudioStreamWav _skillHealClip;
        private AudioStreamWav _skillPowerClip;

        // Event callout clips (#249)
        private AudioStreamWav _doubleKillClip;
        private AudioStreamWav _tripleKillClip;
        private AudioStreamWav _megaKillClip;
        private AudioStreamWav _matchStartFanfareClip;
        private AudioStreamWav _matchEndVictoryClip;
        private AudioStreamWav _matchEndDefeatClip;
        private AudioStreamWav _lastStandingClip;

        private int _lastWeaponSlot = -1;
        private bool _lastJumpState;
        private MatchPhase _lastPhase;
        private int _lastAliveCount = -1;

        private const int SampleRate = 44100;

        public void Init(GameState state)
        {
            _state = state;

            _sfxPlayer = new AudioStreamPlayer();
            _sfxPlayer.Name = "SfxPlayer";
            _sfxPlayer.Bus = "SFX";
            AddChild(_sfxPlayer);

            GenerateClips();
            GenerateCalloutClips();
            StartBackgroundMusic();

            _lastPhase = state.Phase;
            _lastAliveCount = CountAlivePlayers();

            ProcessPriority = 80; // After renderers
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

            // Skill activation sounds (#175)
            foreach (var evt in _state.SkillEvents)
            {
                OnSkillActivated(evt.Type);
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

            // Event callouts (#249)
            ProcessCallouts();
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
        /// Routes to per-weapon sound profile based on weaponId (#175).
        /// </summary>
        public void OnProjectileFired(string weaponId = null)
        {
            var clip = GetWeaponFireClip(weaponId);
            PlayClip(clip, 0.4f);
        }

        private AudioStreamWav GetWeaponFireClip(string weaponId)
        {
            return weaponId switch
            {
                "cannon" or "shotgun" or "flak_cannon" => _fireCannonClip,
                "rocket" or "homing" or "harpoon" => _fireRocketClip,
                "lightning_rod" or "blowtorch" => _fireSniperClip,
                "dynamite" or "sticky_bomb" or "banana_bomb"
                    or "holy_hand_grenade" or "freeze_grenade"
                    or "napalm" or "gravity_bomb" => _fireTossClip,
                "drill" or "boomerang" or "ricochet_disc"
                    or "sheep" or "magma_ball" or "gust_cannon" => _fireSpecialClip,
                "cluster" or "airstrike" => _fireRocketClip,
                _ => _fireClip // fallback for unknown weapons
            };
        }

        /// <summary>
        /// Called when a skill is activated. Plays a distinctive sound per skill type (#175).
        /// </summary>
        public void OnSkillActivated(Simulation.SkillType type)
        {
            var clip = type switch
            {
                Simulation.SkillType.Teleport or Simulation.SkillType.ShadowStep => _skillTeleportClip,
                Simulation.SkillType.Dash or Simulation.SkillType.Jetpack => _skillDashClip,
                Simulation.SkillType.Shield or Simulation.SkillType.Deflect => _skillShieldClip,
                Simulation.SkillType.Earthquake or Simulation.SkillType.MineLay => _skillQuakeClip,
                Simulation.SkillType.Heal or Simulation.SkillType.Mend => _skillHealClip,
                Simulation.SkillType.Overcharge or Simulation.SkillType.WarCry
                    or Simulation.SkillType.EnergyDrain => _skillPowerClip,
                _ => _skillTeleportClip // fallback
            };
            PlayClip(clip, 0.35f);
        }

        /// <summary>
        /// Plays a single low-HP heartbeat thump. Issue #36: triggered once when
        /// the local player first crosses the low-health threshold, not every frame.
        /// </summary>
        public void PlayLowHealthCue()
        {
            PlayClip(_heartbeatClip, 0.6f);
        }

        private void PlayClip(AudioStreamWav clip, float volume)
        {
            if (clip == null) return;
            var player = new AudioStreamPlayer();
            player.Stream = clip;
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.Bus = "SFX";
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
            player.Bus = "SFX";
            AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }

        // --- Background music (StartBackgroundMusic calls GenerateBackgroundTrack in Synth partial) ---

        private void StartBackgroundMusic()
        {
            _musicPlayer = new AudioStreamPlayer();
            _musicPlayer.Name = "MusicPlayer";
            _musicPlayer.Bus = "Music";
            _musicPlayer.VolumeDb = Mathf.LinearToDb(0.15f); // quiet ambient
            AddChild(_musicPlayer);

            var clip = GenerateBackgroundTrack();
            _musicPlayer.Stream = clip;
            _musicPlayer.Play();
        }
    }
}
