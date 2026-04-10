using System.Collections.Generic;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Faction and history encyclopedia entries.
    /// Partial of EncyclopediaContent — split to stay under 300-line SOLID limit.
    /// </summary>
    public static partial class EncyclopediaContent
    {
        public static EncyclopediaEntry[] GetFactionEntries()
        {
            return new[]
            {
                new EncyclopediaEntry
                {
                    Id = "aethermoor_council", Name = "The Aethermoor Council",
                    Description = "The governing body of Aethermoor for three centuries. Maintained the Aether Core under strict neutrality until Cogsworth's coup.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Status", "Defunct" },
                        { "Members", "7 elected engineers" },
                        { "Legacy", "Founded after the Gear Wars" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "verdant_militia", Name = "The Verdant Militia",
                    Description = "A ragtag resistance force recruited from farmhands, mechanics, and academy students. The last organized opposition to Cogsworth.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Status", "Active" },
                        { "Leader", "Pip (youngest member)" },
                        { "Motto", "Roots hold" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "desert_scavengers", Name = "The Desert Scavengers",
                    Description = "Nomadic salvagers who strip Gear Wars wreckage for parts. They respect anyone who can survive the Sunken Expanse.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Status", "Active" },
                        { "Loyalty", "None — trade only" },
                        { "Gift", "Sticky Bomb design" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "frostspire_order", Name = "The Frostspire Order",
                    Description = "An engineering-monastic order that maintained the Frostspire Peaks' defenses. Their Glacial Cannon was seized and reversed by Cogsworth.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Status", "In hiding" },
                        { "Philosophy", "Build to protect, never to attack" },
                        { "Gift", "Freeze Grenade technique" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "automata_corps", Name = "Cogsworth's Automata Corps",
                    Description = "Not a faction of people — a factory line. Cogsworth's entirely mechanical army, mass-produced in the Magma Crucible.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Status", "Active" },
                        { "Units", "Bomber, Shielder, Flyer, Healer" },
                        { "General", "Forge Colossus" }
                    }
                }
            };
        }

        public static EncyclopediaEntry[] GetHistoryEntries()
        {
            return new[]
            {
                new EncyclopediaEntry
                {
                    Id = "gear_wars", Name = "The Gear Wars",
                    Description = "An eleven-year war two centuries ago between those who wanted to power machines with the Aether Core and those who wanted to study it. Left the Green Valley a wasteland now called the Sunken Expanse.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Era", "200 years before present" },
                        { "Duration", "11 years" },
                        { "Outcome", "Council formed, Core shared equally" }
                    }
                },
                new EncyclopediaEntry
                {
                    Id = "cogsworth_coup", Name = "Cogsworth's Coup",
                    Description = "Baron Cogsworth staged a coup with his automata army after the Council voted 6-1 against granting him unrestricted Aether Core access. Six council members presumed dead.",
                    Stats = new Dictionary<string, string>
                    {
                        { "Era", "Recent" },
                        { "Trigger", "Council vote 6-1 against" },
                        { "Outcome", "Cogsworth seizes the Aether Core" }
                    }
                }
            };
        }
    }
}
