namespace SpaceInvadersCore.Game.AISupport
{
    /// <summary>
    /// Two types of radar supported, one for the invaders and one for the shields.
    /// </summary>
    internal enum RadarType
    {
        /// <summary>
        /// Senses invaders and bullets, does not sense shields.
        /// </summary>
        MainRadar, 
        
        /// <summary>
        /// Senses the shields only.
        /// </summary>
        ShieldRadar
    }
}