using SpaceInvadersCore;
using System.Drawing;

namespace SpaceInvadersCore.Game
{
    /// <summary>
    /// Represents a Space Invaders score board. That is the score, high score.
    /// It handles awarding points for killing invaders and saucers plus bonus lives.
    /// </summary>
    internal class ScoreBoard
    {
        //   ███     ███     ███    ████    █████   ████     ███      █     ████    ████
        //  █   █   █   █   █   █   █   █   █       █   █   █   █    █ █    █   █   █   █
        //  █       █       █   █   █   █   █       █   █   █   █   █   █   █   █   █   █
        //   ███    █       █   █   ████    ████    ████    █   █   █   █   ████    █   █
        //      █   █       █   █   █ █     █       █   █   █   █   █████   █ █     █   █
        //  █   █   █   █   █   █   █  █    █       █   █   █   █   █   █   █  █    █   █
        //   ███     ███     ███    █   █   █████   ████     ███    █   █   █   █   ████

        /// <summary>
        /// Scoring points for an invader.
        /// The original is 30,20,10.
        /// </summary>
        const int c_invaderPointsRow0 = 30;
        const int c_invaderPointsRow1and2 = 20;
        const int c_invaderPointsRow3and4 = 10;

        /// <summary>
        /// This is the rectangle that will be used to draw the score.
        /// </summary>
        private static Rectangle s_scoreRectangle = new(24, 16, 32, 8);

        /// <summary>
        /// This is the rectangle that will be used to draw the high score.
        /// </summary>
        private static Rectangle s_highScoreRectangle = new(88, 16, 32, 8);

        /// <summary>
        /// This is the location where the score will be drawn.
        /// </summary>
        private static Point s_scoreLocation = new(24, 16);

        /// <summary>
        /// This is the location where the high score will be drawn.
        /// </summary>
        private static Point s_highScoreLocation = new(88, 16);

        /// <summary>
        /// The score the player currently has.
        /// </summary>
        private int score;

        /// <summary>
        /// Getter for players' score.
        /// </summary>
        internal int Score
        {
            get { return score; }
        }

        /// <summary>
        /// Sets the score to 0.
        /// </summary>
        internal void ResetScore()
        {
            score = 0;
        }

        /// <summary>
        /// For training AI as individual levels, it's necessary for it to start with the score the previous finished with.
        /// </summary>
        /// <param name="fakedScore"></param>
        internal void AISetScore(int fakedScore)
        {
            score = fakedScore;
        }

        /// <summary>
        /// Number of bullets fired by the player that hit an invader/saucer.
        /// </summary>
        private int invaderKills = 0;

        /// <summary>
        /// We separate out the saucer kills from the invader kills, because 
        /// score is not a perfect indicator of how well the player is doing...
        /// We might discard a player who destroys less aliens, but more saucers. 
        /// That won't enable us ensure it improves thru levels.
        /// </summary>
        private int saucerKills = 0;

        /// <summary>
        /// Getter for kills (bullets that hit an invader).
        /// </summary>
        internal int InvaderKills
        {
            get { return invaderKills; }
        }

        /// <summary>
        /// Getter for the number of saucers killed.
        /// </summary>
        internal int SaucerKills
        {
            get { return saucerKills; }
        }

        /// <summary>
        /// The high score so far.
        /// </summary>
        internal int HighScore = 0;

        /// <summary>
        /// Bonus life is awarded at 1500.
        /// </summary>
        internal bool AdditionalLifeDue = false;

        /// <summary>
        /// This is the screen that the score / high score will be drawn on.
        /// </summary>
        private readonly VideoDisplay videoScreen;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="screen"></param>
        internal ScoreBoard(VideoDisplay screen)
        {
            videoScreen = screen;
        }

        /// <summary>
        /// Draws the "score" to the image. If the score is higher than the high score, the high score is updated.
        /// </summary>
        /// <param name="graphics"></param>
        internal void Draw()
        {
            //  SCORE<1> HI-SCORE SCORE<2>
            //    0000    0000                  <<<----

            // We don't "erase" the score, we just draw over it with black. Erasing would be slower, and we don't have to avoid pixels like bullets.
            videoScreen.FillRectangle(Color.Black, s_scoreRectangle);
            videoScreen.DrawString(score.ToString().PadLeft(4, '0'), s_scoreLocation);

            // If the score is higher than the high score, update the high score (not every time, just when it changes).
            if (Score > HighScore)
            {
                HighScore = Score;
                DrawHighScore();
            }
        }

        /// <summary>
        /// Draws the high score.
        /// </summary>
        internal void DrawHighScore()
        {
            // This hurts my autistic brain. On the original the high score is not centred!
            // see https://www.youtube.com/watch?v=MU4psw3ccUI.
            // I really want to add 8px to "88" so it's centred, but I'm not going to.

            videoScreen.FillRectangle(Color.Black, s_highScoreRectangle);
            videoScreen.DrawString(HighScore.ToString().PadLeft(4, '0'), s_highScoreLocation);
        }

        /// <summary>
        /// Called when player shoots saucer.
        /// </summary>
        /// <param name="playerShots"></param>
        internal void SaucerHit(int playerShots)
        {
            // The score for shooting the saucer ranges from 50 to 300, and the exact value depends on the number of player shots fired.
            // The table at 0x1D54 contains 16 score values, but a bug in the code at 0x044E treats the table as having 15 values. The saucer
            // data starts out pointing to the first entry. Every time the player's shot blows up the pointer is incremented and wraps back around.
            playerShots %= 15; // 0..14

            int scoreBefore = score;

            score += OriginalDataFrom1978.s_saucerScores[playerShots];

            CheckForBonusLife(scoreBefore);

            ++saucerKills;
        }

        /// <summary>
        /// Called when player shoots invader.
        /// </summary>
        /// <param name="invaderRow"></param>
        internal void InvaderHit(int invaderRow)
        {
            if (invaderRow < 0 || invaderRow > 5) throw new ArgumentOutOfRangeException(nameof(invaderRow), "5 invader rows, 0..4");

            int scoreBefore = score;

            switch (invaderRow)
            {
                case 4: // top level
                    score += c_invaderPointsRow0;
                    break;

                case 3:
                case 2:
                    score += c_invaderPointsRow1and2;
                    break;

                case 1:
                case 0: // bottom level
                    score += c_invaderPointsRow3and4;
                    break;
            }

            CheckForBonusLife(scoreBefore);

            ++invaderKills;
        }

        /// <summary>
        /// Bonus life is awarded at 1500 not every 1500 as afaik. When the score crosses the boundary we set a flag.
        /// </summary>
        /// <param name="scoreBefore"></param>
        private void CheckForBonusLife(int scoreBefore)
        {
            if (scoreBefore < OriginalDataFrom1978.c_scoreAtWhichExtraLifeIsAwarded && score >= OriginalDataFrom1978.c_scoreAtWhichExtraLifeIsAwarded)
            {
                AdditionalLifeDue = true;
            }
        }
    }
}