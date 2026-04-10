namespace Baboomz.Simulation
{
    public enum TutorialActionType
    {
        MoveRight,
        Jump,
        AimUp,
        ChargeAndFire,
        SwitchWeapon,
        UseSkill,
        DestroyTerrain,
        KillEnemy
    }

    public class TutorialStepDef
    {
        public int StepId;
        public string Title;
        public string Description;
        public TutorialActionType ActionType;
        public float Threshold;       // min value to complete (e.g., distance moved, angle reached)
        public int TargetWeaponSlot;  // for SwitchWeapon steps (-1 = any)
        public int TargetSkillSlot;   // for UseSkill steps (-1 = any)
    }

    public class TutorialState
    {
        public TutorialStepDef[] Steps;
        public int CurrentStepIndex;
        public bool IsComplete;
        public bool IsSkipped;

        // Per-step tracking
        public float StepProgress;     // accumulated progress toward current step threshold
        public Vec2 StepStartPosition; // player position when step began
        public float StepStartAimAngle; // aim angle when step began
        public int StepStartTerrainPixels; // terrain pixels destroyed when step began

        // Event flags (set by TutorialSystem, cleared each step advance)
        public bool StepJustCompleted;  // true for one tick after a step completes
    }
}
