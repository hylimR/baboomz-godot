using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public enum AchievementCategory
    {
        Combat,
        Skill,
        Campaign,
        Misc
    }

    public struct AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        public AchievementCategory Category;
        public bool IsHidden;
    }

    public struct AchievementEvent
    {
        public string AchievementId;
        public int PlayerIndex;
    }

    /// <summary>
    /// Static registry of all 30 achievement definitions.
    /// Pure data — no logic, no Unity dependency.
    /// </summary>
    public static class AchievementDefs
    {
        public static readonly AchievementDef[] All = new[]
        {
            // Combat Mastery (10)
            new AchievementDef { Id = "cm_1",  Name = "First Blood",       Description = "Deal damage for the first time",                    Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_2",  Name = "Sharpshooter",      Description = "Hit 3 direct cannon shots in one match",             Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_3",  Name = "Chain Reaction",    Description = "Trigger an oil barrel chain explosion",              Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_4",  Name = "Zap Master",        Description = "Hit 2 targets with one Lightning Rod chain",         Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_5",  Name = "Demolition Expert", Description = "Destroy 500 terrain pixels in one shot",             Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_6",  Name = "Untouchable",       Description = "Win a match without taking damage",                  Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_7",  Name = "Overkill",          Description = "Deal 100+ damage in a single hit",                   Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_8",  Name = "Pyromaniac",        Description = "Deal 200+ total fire zone damage in one match",      Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_9",  Name = "Gravity Well",      Description = "Kill an enemy with fall damage from knockback",      Category = AchievementCategory.Combat },
            new AchievementDef { Id = "cm_10", Name = "Freezer Burn",      Description = "Freeze an enemy then hit them with napalm",          Category = AchievementCategory.Combat },

            // Skill Mastery (8)
            new AchievementDef { Id = "sm_1",  Name = "Escape Artist",     Description = "Teleport to dodge a projectile",                    Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_2",  Name = "Tarzan",            Description = "Travel 30+ units on a single grapple swing",         Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_3",  Name = "Earthquake!",       Description = "Hit 2+ enemies with one Earthquake",                Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_4",  Name = "Shield Wall",       Description = "Block 100+ damage with a single Shield activation", Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_5",  Name = "Bridge Builder",    Description = "Place a girder that an enemy walks on",              Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_6",  Name = "Smoke and Mirrors", Description = "Fire through your own smoke screen and hit",         Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_7",  Name = "War Machine",       Description = "Get 3 kills while War Cry is active",               Category = AchievementCategory.Skill },
            new AchievementDef { Id = "sm_8",  Name = "Jet Fighter",       Description = "Hit an enemy while airborne from Jetpack",           Category = AchievementCategory.Skill },

            // Campaign (7)
            new AchievementDef { Id = "ca_1",  Name = "Enlisted",          Description = "Complete World 1",                                  Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_2",  Name = "Desert Storm",      Description = "Complete World 2",                                  Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_3",  Name = "Frostbreaker",      Description = "Complete World 3",                                  Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_4",  Name = "Forgemaster",       Description = "Complete World 4",                                  Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_5",  Name = "Liberator",         Description = "Complete World 5",                                  Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_6",  Name = "Speedrunner",       Description = "Complete any world in under 10 minutes",            Category = AchievementCategory.Campaign },
            new AchievementDef { Id = "ca_7",  Name = "Perfectionist",     Description = "3-star all levels in one world",                    Category = AchievementCategory.Campaign },

            // Misc/Hidden (5)
            new AchievementDef { Id = "mi_1",  Name = "Self-Destruct",     Description = "Kill yourself with your own weapon",                Category = AchievementCategory.Misc, IsHidden = true },
            new AchievementDef { Id = "mi_2",  Name = "Pacifist Round",    Description = "Win a round without firing a weapon",               Category = AchievementCategory.Misc, IsHidden = true },
            new AchievementDef { Id = "mi_3",  Name = "Against All Odds",  Description = "Win at 1 HP",                                      Category = AchievementCategory.Misc, IsHidden = true },
            new AchievementDef { Id = "mi_4",  Name = "Sheep Thrills",     Description = "Kill an enemy with the Sheep weapon",               Category = AchievementCategory.Misc, IsHidden = true },
            new AchievementDef { Id = "mi_5",  Name = "Holy Moly",         Description = "Destroy indestructible terrain with HHG",           Category = AchievementCategory.Misc, IsHidden = true }
        };

        static Dictionary<string, AchievementDef> _byId;

        public static AchievementDef? GetById(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, AchievementDef>();
                foreach (var def in All)
                    _byId[def.Id] = def;
            }
            return _byId.ContainsKey(id) ? _byId[id] : (AchievementDef?)null;
        }
    }
}
