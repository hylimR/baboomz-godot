namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure simulation logic for the interactive tutorial.
    /// Validates player actions against the current step's requirements.
    /// No Unity dependencies — fully testable in EditMode.
    /// </summary>
    public static class TutorialSystem
    {
        public static TutorialState CreateFromSteps(TutorialStepDef[] steps)
        {
            var state = new TutorialState
            {
                Steps = steps ?? new TutorialStepDef[0],
                CurrentStepIndex = 0,
                IsComplete = false,
                IsSkipped = false
            };
            return state;
        }

        public static void Update(GameState gameState, float dt)
        {
            var tut = gameState.Tutorial;
            if (tut == null || tut.IsComplete || tut.IsSkipped) return;
            if (tut.Steps == null || tut.Steps.Length == 0)
            {
                tut.IsComplete = true;
                return;
            }

            tut.StepJustCompleted = false;

            if (tut.CurrentStepIndex >= tut.Steps.Length)
            {
                tut.IsComplete = true;
                return;
            }

            var step = tut.Steps[tut.CurrentStepIndex];
            var player = gameState.Players[0];

            bool completed = CheckStepCompletion(step, ref player, tut, gameState);

            // Write back player state (struct copy)
            gameState.Players[0] = player;

            if (completed)
            {
                AdvanceStep(tut);
            }
        }

        public static void Skip(TutorialState tut)
        {
            if (tut == null) return;
            tut.IsSkipped = true;
        }

        public static TutorialStepDef GetCurrentStep(TutorialState tut)
        {
            if (tut == null || tut.IsComplete || tut.IsSkipped) return null;
            if (tut.Steps == null || tut.CurrentStepIndex >= tut.Steps.Length) return null;
            return tut.Steps[tut.CurrentStepIndex];
        }

        public static void InitStepTracking(TutorialState tut, GameState gameState)
        {
            if (tut == null || tut.Steps == null || tut.CurrentStepIndex >= tut.Steps.Length) return;
            if (gameState.Players.Length == 0) return;

            var player = gameState.Players[0];
            tut.StepStartPosition = player.Position;
            tut.StepStartAimAngle = player.AimAngle;
            tut.StepProgress = 0f;
            tut.StepStartTerrainPixels = player.TerrainPixelsDestroyed;
        }

        static bool CheckStepCompletion(TutorialStepDef step, ref PlayerState player,
            TutorialState tut, GameState gameState)
        {
            switch (step.ActionType)
            {
                case TutorialActionType.MoveRight:
                    float distMoved = player.Position.x - tut.StepStartPosition.x;
                    tut.StepProgress = distMoved;
                    return distMoved >= step.Threshold;

                case TutorialActionType.Jump:
                    // Player jumped if they're above their start Y by threshold
                    float heightGained = player.Position.y - tut.StepStartPosition.y;
                    if (heightGained > tut.StepProgress)
                        tut.StepProgress = heightGained;
                    return tut.StepProgress >= step.Threshold;

                case TutorialActionType.AimUp:
                    float angleDelta = player.AimAngle - tut.StepStartAimAngle;
                    tut.StepProgress = angleDelta;
                    return angleDelta >= step.Threshold;

                case TutorialActionType.ChargeAndFire:
                    // Complete when player has fired (ShotsFired incremented)
                    tut.StepProgress = player.ShotsFired;
                    return player.ShotsFired > 0 && gameState.PlayerInputs[0].FireReleased;

                case TutorialActionType.SwitchWeapon:
                    if (step.TargetWeaponSlot >= 0)
                    {
                        tut.StepProgress = player.ActiveWeaponSlot;
                        return player.ActiveWeaponSlot == step.TargetWeaponSlot;
                    }
                    // Any weapon switch from slot 0
                    tut.StepProgress = player.ActiveWeaponSlot;
                    return player.ActiveWeaponSlot != 0;

                case TutorialActionType.UseSkill:
                    // Check if any skill was activated this frame
                    if (gameState.SkillEvents.Count > 0)
                    {
                        for (int i = 0; i < gameState.SkillEvents.Count; i++)
                        {
                            if (gameState.SkillEvents[i].PlayerIndex == 0)
                            {
                                tut.StepProgress = 1f;
                                return true;
                            }
                        }
                    }
                    return false;

                case TutorialActionType.DestroyTerrain:
                    int pixelsDestroyed = player.TerrainPixelsDestroyed - tut.StepStartTerrainPixels;
                    tut.StepProgress = pixelsDestroyed;
                    return pixelsDestroyed >= step.Threshold;

                case TutorialActionType.KillEnemy:
                    // Check if any non-player entity was killed
                    for (int i = 1; i < gameState.Players.Length; i++)
                    {
                        if (gameState.Players[i].IsDead)
                        {
                            tut.StepProgress = 1f;
                            return true;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        static void AdvanceStep(TutorialState tut)
        {
            tut.StepJustCompleted = true;
            tut.CurrentStepIndex++;
            tut.StepProgress = 0f;

            if (tut.CurrentStepIndex >= tut.Steps.Length)
            {
                tut.IsComplete = true;
            }
        }

        public static TutorialActionType ParseActionType(string actionStr)
        {
            switch (actionStr)
            {
                case "move_right": return TutorialActionType.MoveRight;
                case "jump": return TutorialActionType.Jump;
                case "aim_up": return TutorialActionType.AimUp;
                case "charge_and_fire": return TutorialActionType.ChargeAndFire;
                case "switch_weapon": return TutorialActionType.SwitchWeapon;
                case "use_skill": return TutorialActionType.UseSkill;
                case "destroy_terrain": return TutorialActionType.DestroyTerrain;
                case "kill_enemy": return TutorialActionType.KillEnemy;
                default: return TutorialActionType.MoveRight;
            }
        }
    }
}
