using SpaceInvadersCore.Game.Player;
using System.Drawing;

namespace SpaceInvadersCore.Game.AISupport
{
    /// <summary>
    /// Represents a RADAR.
    /// </summary>
    public static class Radar
    {
        /// <summary>
        /// How many radar points will be used for the long distance radar (invaders).
        /// </summary>
        public const int c_mainRadarSamplePoints = 45; // 51

        /// <summary>
        /// How many radar points will be used for the short distance radar (shields).
        /// </summary>
        public const int c_shieldRadarSamplePoints = 15; // 21

        /// <summary>
        /// How many points will be returned by the radar.
        /// </summary>
        private const int c_radarOutputSize = c_mainRadarSamplePoints + c_shieldRadarSamplePoints + 1;

        /// <summary>
        /// We support two radars, one for the invaders and one for the shields.
        /// </summary>
        private static readonly RadarDefinition[] c_radarDefinitions = new RadarDefinition[]
        {
            // bullet + invader radar
            new RadarDefinition(
                radarType: RadarType.MainRadar,
                samplePoints: c_mainRadarSamplePoints,
                startSweepAngleInDegrees: -85f, // -89
                searchDistanceInPixels: 210, // enough to reach to the top of the saucer
                startDistance: 7, // not too close to the player ship, but close enough to sense bullets and invaders
                resolution: 1), // I don't like "1", it is a lot of pixels. But bullets are 1px wide, and it's easy to miss them.

            // shield radar
            new RadarDefinition(radarType: RadarType.ShieldRadar,
                samplePoints: c_shieldRadarSamplePoints,
                startSweepAngleInDegrees: -65f,
                searchDistanceInPixels: 50, // 60 far enough to detect a shield in all directions that we sense
                startDistance: 9, // 9
                resolution: 1)
        };

        /// <summary>
        /// This returns {c_radarOutputSize} data points via two radars plus the invader speed/direction indicator.
        /// 
        /// Radar 1: Sweep 51 different angles from -89 to +89, each value in the array corresponding to the distance to the nearest 
        ///          invader/saucer in that direction. This radar penetrates shields and ignores them. Important to note, that bullets
        ///          appear on the radar (player and invader).
        /// Radar 2: Sweep 21 different angles from -65 +65, each value in the array corresponding to the distance to the nearest 
        ///          shield in that direction. This is a short radar that only sees shields.
        ///          Also note, it doesn't tell the AI of all the other shields, this is a defensive, am I protected or not sensor.
        /// 
        /// Humans know the shields are useful to hide between. With a simple radar that doesn't distinguish, how is it meant to know
        /// whether to shoot or hide behind? What you force the AI to do is destroy the shields just in case it's an invader. 
        /// 
        /// The thing about is a shield is that knowing it was there, is not an indicator of it having not been blown to smithereens since.
        /// 
        /// In fact the AI has no concept of past, current, future it works in here and now. This radar thus informs of when there is shield
        /// not destroyed that covers the size of the player ship (i.e. bullet cannot hit).
        /// </summary>
        /// <returns>AI data output from the radar.</returns>
        /// 
        internal static double[] Output(PlayerShip playerShip, int rackDirection, List<Point> radarPoints, int currentFrame, VideoDisplay videoScreen)
        {
            RemovePreviousRadarPoints(radarPoints, videoScreen);

            double[] RADAROutput = new double[c_radarOutputSize];

            // nothing to radar on frame 0.
            if (currentFrame == 0) return RADAROutput;

            int locationInOutput = 0; // radar output is a single array, but we have two radars, so we need to keep track of where we are in the array.

            // process all the radars, putting results in RADAROutput
            foreach (RadarDefinition radarDefinition in c_radarDefinitions)
            {
                locationInOutput += ProcessRadar(
                    playerShip: playerShip,
                    videoScreen: videoScreen,
                    radarPoints: radarPoints,
                    ref RADAROutput,
                    radarDefinition,
                    locationInOutput);
            }

            // RADAR tells you where things are, but from a firing perspective, it's helpful to know that they are moving, and in which direction
            // given for any left->right or vice versa, speed is constant we provide it here. 
            RADAROutput[c_radarOutputSize - 1] = (double)rackDirection / 3f;

            return RADAROutput;
        }

        /// <summary>
        /// Process the radar (sweep the angle) and return the distance to the nearest object in that direction.
        /// </summary>
        /// <param name="playerShip">Provides us the location (of the ship), which is the center of the radar.</param>
        /// <param name="videoScreen">Contains the video screen with invaders / shields / bullets</param>
        /// <param name="radarPoints">(REF) Returns a list of radar points that it sensed and plotted, so we can remove them.</param>
        /// <param name="radarOutput">(REF) Contains the output of the radar (proximity distances).</param>
        /// <param name="radarDefinition">Contains the definition for the radar (sweep/distance etc).</param>
        /// <param name="indexToRadarOutput">The position the first radar sensor writes in the radarOutput.</param>
        private static int ProcessRadar(
            PlayerShip playerShip,
            VideoDisplay videoScreen,
            List<Point> radarPoints,
            ref double[] radarOutput,
            RadarDefinition radarDefinition,    
            int indexToRadarOutput)
        {
            bool shieldDebugOn = DebugSettings.c_debugDrawMainRadar || DebugSettings.c_debugDrawShieldRadar;

            //     -45  0  45
            //  -90 _ \ | / _ 90   <-- relative to direction of player. 0 = right, 90 = up, so we adjust for
            float radarAngleToCheckInDegrees = radarDefinition.StartSweepAngleInDegrees + 90;

            // sweep the angle -x to +x, populating the radarOutput with the distance to the nearest object in that direction.
            for (int radarAngleIndex = indexToRadarOutput; radarAngleIndex < indexToRadarOutput + radarDefinition.SamplePoints; radarAngleIndex++)
            {
                // get the sin/cos for the angle we are checking
                GetSinCosForAngle(radarAngleToCheckInDegrees, out double cos, out double sin);

                float distanceToObject = 0;

                for (int currentRadarScanningDistanceRadius = radarDefinition.StartDistanceInPixels; 
                         currentRadarScanningDistanceRadius < radarDefinition.SearchDistanceInPixels;
                         currentRadarScanningDistanceRadius += radarDefinition.Resolution)
                {
                    // y has to be negated because the screen is upside down. Cartesian (0,0) is bottom left, our back-buffer is Bitmap aligned (0,0) is top left.
                    // sweep is intentionally left to right.
                    Point pointOnRadarToCheck = new(
                        playerShip.Position.X - (int)Math.Round(cos * currentRadarScanningDistanceRadius),
                        playerShip.Position.Y - (int)Math.Round(sin * currentRadarScanningDistanceRadius));

                    // a neat short-cut when the aliens are high is to not scan the lower part. WE CANNOT. Bullets rain down from the sky and will be below the refAlienY.
                    // DO NOT ADD >> "if (radarDefinition.RadarType == RadarType.MainRadar && pointOnRadarToCheck.Y + 8 < refAlienY) continue;"

                    // if outside the bounds of the radar, then we cannot check pixels, and we cannot go around the loop again (we'll be even more out of bounds).
                    if (!IsRadarPointWithinBounds(radarDefinition.RadarType, pointOnRadarToCheck))
                        break;

                    Color pixel = videoScreen.GetPixel(pointOnRadarToCheck);

                    if (ShouldAssignDistanceToObject(radarDefinition.RadarType, pixel))
                    {
                        distanceToObject = currentRadarScanningDistanceRadius; // we've found the nearest object, so we can stop scanning.
                        break;
                    }

                    // draw the radar point if we are debugging.
                    if (shieldDebugOn && ShouldDrawDebugRadarPoint(radarDefinition.RadarType, pixel)) RadarPlotPoint(radarPoints, videoScreen, pointOnRadarToCheck);
                }

                // store the output of the radar (distance to nearest object in that direction). It's inverted so "1"="VERY CLOSE" and "0"="VERY FAR AWAY".
                radarOutput[radarAngleIndex] = distanceToObject > 0 ? (1 - (distanceToObject / (double) radarDefinition.SearchDistanceInPixels)) : 0;

                // move to the next angle to check.
                radarAngleToCheckInDegrees += radarDefinition.RadarVisionAngleInDegrees;
            }

            // return where the next radar should start writing in the radarOutput.
            return indexToRadarOutput + radarDefinition.SamplePoints;
        }

        /// <summary>
        /// Returns true if we've encountered a pixel that we are interested in (alien/shield).
        /// </summary>
        /// <param name="radarType"></param>
        /// <param name="pixel"></param>
        /// <returns></returns>
        private static bool ShouldAssignDistanceToObject(RadarType radarType, Color pixel)
        {
            return radarType == RadarType.MainRadar && pixel.A == 255 && (pixel.G != 0 || pixel.R != 0) || // true of invader (white) or saucer (magenta) shields (green, alpha 252).
                   radarType == RadarType.ShieldRadar && pixel.A <= 252; // do we see shield at the pixel? (shield=252 alpha)
        }

        /// <summary>
        /// Returns true if the point is within the bounds of the radar.
        /// </summary>
        /// <param name="radarType"></param>
        /// <param name="pointOnRadarToCheck"></param>
        /// <returns></returns>
        private static bool IsRadarPointWithinBounds(RadarType radarType, Point pointOnRadarToCheck)
        {
            if (pointOnRadarToCheck.X < 0 || pointOnRadarToCheck.X > 223) return false; // off screen, no need to check the radar further

            if (pointOnRadarToCheck.Y < (radarType == RadarType.MainRadar ? OriginalDataFrom1978.c_topOfSaucerLinePX : OriginalDataFrom1978.c_topOfShieldsPX - 2)) return false;

            return true;
        }

        /// <summary>
        /// Returns true if the point should be drawn on the radar.
        /// </summary>
        /// <param name="radarType"></param>
        /// <param name="pixel"></param>
        /// <returns></returns>
        private static bool ShouldDrawDebugRadarPoint(RadarType radarType, Color pixel)
        {
            return (radarType == RadarType.MainRadar && DebugSettings.c_debugDrawMainRadar && pixel.A > 252) || // do we see invader / saucer on that pixel? true of invader (white) or saucer (magenta) shields (green, alpha 252).
                   (radarType == RadarType.ShieldRadar && DebugSettings.c_debugDrawShieldRadar);  // do we see shield at the pixel?
        }

        /// <summary>
        /// Add 90 degrees to the angle to check, and convert to radians. Then return sine/cosine for that angle.
        /// </summary>
        /// <param name="RADARAngleToCheckInDegrees"></param>
        /// <param name="cos"></param>
        /// <param name="sin"></param>
        private static void GetSinCosForAngle(float RADARAngleToCheckInDegrees, out double cos, out double sin)
        {
            double alienRadarAngleToCheckInRadians = DegreesInRadians(RADARAngleToCheckInDegrees);

            cos = Math.Cos(alienRadarAngleToCheckInRadians);
            sin = Math.Sin(alienRadarAngleToCheckInRadians);
        }

        /// <summary>
        /// Draws a blue pixel on the radar at the given point.
        /// </summary>
        /// <param name="radarPoints"></param>
        /// <param name="videoScreen"></param>
        /// <param name="pointOnRadarToCheck"></param>
        private static void RadarPlotPoint(List<Point> radarPoints, VideoDisplay videoScreen, Point pointOnRadarToCheck)
        {
            videoScreen.SetPixel(Color.Blue, pointOnRadarToCheck);
            radarPoints.Add(pointOnRadarToCheck);
        }

        /// <summary>
        /// Removes blue pixels from the radar that were previously plotted.
        /// </summary>
        /// <param name="radarPoints"></param>
        /// <param name="videoScreen"></param>
        private static void RemovePreviousRadarPoints(List<Point> radarPoints, VideoDisplay videoScreen)
        {
            // we only undo, if one of the radars were on
            if (radarPoints.Count == 0) return;

            // remove the previous plotted radar points only if they are still radar coloured (no invaders/bullets etc drawn on top).
            // we are explicitly removing pixels we plotted previously, rather than re-computing to avoid using sin/cos.
            foreach (Point pointInRadarBeamPlottedPreviously in radarPoints)
            {
                if (!ColorEquals(videoScreen.GetPixel(pointInRadarBeamPlottedPreviously), Color.Blue)) continue;

                videoScreen.SetPixel(Color.FromArgb(255, 0, 0, 0), pointInRadarBeamPlottedPreviously);
            }

            radarPoints.Clear();
        }

        /// <summary>
        /// Logic requires radians but we track angles in degrees, this converts.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double DegreesInRadians(double angle)
        {
            return Math.PI * angle / 180;
        }

        /// <summary>
        /// Compare two Color objects for equality, because Color.Equals() is not implemented in a logical way.
        /// This matches ARGB. It does not consider "Name".
        /// </summary>
        /// <param name="colour1"></param>
        /// <param name="colour2"></param>
        /// <returns></returns>
        public static bool ColorEquals(Color colour1, Color colour2)
        {
            return colour1.A == colour2.A && colour1.R == colour2.R && colour1.G == colour2.G && colour1.B == colour2.B;
        }
    }
}