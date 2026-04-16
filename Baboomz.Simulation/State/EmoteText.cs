namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure data mapping from <see cref="EmoteType"/> to the speech-bubble text
    /// rendered above the emoting player. Lives in the simulation layer so both
    /// the Godot renderer and tests can share the single source of truth.
    /// </summary>
    public static class EmoteText
    {
        /// <summary>
        /// Returns the bubble text for an emote, or null for <see cref="EmoteType.None"/>.
        /// </summary>
        public static string Get(EmoteType type) => type switch
        {
            EmoteType.Taunt => "Ha!",
            EmoteType.Laugh => "Haha!",
            EmoteType.Cry => "Nooo!",
            EmoteType.ThumbsUp => "GG!",
            EmoteType.Clap => "Bravo!",
            EmoteType.Salute => "Sir!",
            EmoteType.Dance => "Woo!",
            EmoteType.Flex => "Flex!",
            _ => null
        };
    }
}
