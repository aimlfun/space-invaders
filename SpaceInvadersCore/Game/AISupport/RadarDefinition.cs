namespace SpaceInvadersCore.Game.AISupport
{
    /// <summary>
    /// Contains the RADAR definition structure, it enables us to scan the radar in different ways using a single method and a definition structure input.
    /// </summary>
    struct RadarDefinition
    {
        /// <summary>
        /// The type of radar we're using. (See RadarType enum)
        /// </summary>
        internal RadarType RadarType { get; set; }

        /// <summary>
        /// How many radar beams between +/- StartSweepAngleInDegrees.
        /// </summary>
        internal int SamplePoints { get; set; }

        /// <summary>
        /// The angle in degrees to start the radar sweep from.
        /// </summary>
        internal float StartSweepAngleInDegrees { get; set; }

        /// <summary>
        /// How long the radar beam is in pixels.
        /// </summary>
        internal int SearchDistanceInPixels { get; set; }

        /// <summary>
        /// How many degrees between each radar beam.
        /// </summary>
        internal float RadarVisionAngleInDegrees { get; set; }

        /// <summary>
        /// How many pixels from the centre of the radar to start the radar sweep from.
        /// </summary>
        internal int StartDistanceInPixels { get; set; }

        /// <summary>
        /// How many pixels to move the radar sensor along the beam.
        /// With "1", there is no gap. With "2", there is a gap of 1 pixel between each sensor reading.
        /// </summary>
        internal int Resolution { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="radarType"></param>
        /// <param name="samplePoints"></param>
        /// <param name="startSweepAngleInDegrees"></param>
        /// <param name="searchDistanceInPixels"></param>
        /// <param name="startDistance"></param>
        /// <param name="resolution"></param>
        public RadarDefinition(RadarType radarType, int samplePoints, float startSweepAngleInDegrees, int searchDistanceInPixels, int startDistance, int resolution)
        {
            RadarType = radarType;
            SamplePoints = samplePoints;
            StartSweepAngleInDegrees = startSweepAngleInDegrees;
            SearchDistanceInPixels = searchDistanceInPixels;
            RadarVisionAngleInDegrees = 2f * (-startSweepAngleInDegrees) / (samplePoints - 1f);
            StartDistanceInPixels = startDistance;
            Resolution = resolution;
        }
    }
}