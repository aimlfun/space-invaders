using SpaceInvadersCore;
using System.Drawing;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection;

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
            Draw();
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
            videoScreen.FillRectangle(Color.Black, OriginalDataFrom1978.s_scorePlayer1Rectangle);
            videoScreen.DrawString(ScoreAfter10KUsingSymbols(score), OriginalDataFrom1978.s_scorePlayer1Location);

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

            videoScreen.FillRectangle(Color.Black, OriginalDataFrom1978.s_highScoreRectangle);
            videoScreen.DrawString(ScoreAfter10KUsingSymbols(HighScore), OriginalDataFrom1978.s_highScoreLocation);
        }

        /// <summary>
        /// The Space Invaders original loops around to 0 after 9990, and only has space for 4 digits. 
        /// Because of that it also awards more extra lives (every time it hits 1500).
        /// The output happens, because it uses BCD and writes LSB and MSB as 2 digit hex.
        /// 
        /// Given we are not cycling it back to 0, we need to convert the score to a string that fits in 4 digits,
        /// so we use symbols to represent 10K, 100K, 1M.
        /// 0000-9999
        ///  10K-999K
        ///   1M-999M, after which the display will be a mess as it does not erase fully scores > 4 chars.
        /// Decimal points are not an option, with the limited screen space.
        /// </summary>
        /// <param name="scoreToConvert"></param>
        /// <returns></returns>
        private string ScoreAfter10KUsingSymbols(int scoreToConvert)
        {
            if (scoreToConvert < 10000)
            {
                return scoreToConvert.ToString().PadLeft(4, '0');
            }
            else
            {
                // measuring score in millions?
                if (score >= 1000000)
                {
                    scoreToConvert /= 1000000;
                    return scoreToConvert.ToString() + "M";
                }

                // measuring score in thousands?
                // 10000 = 10K
                scoreToConvert /= 1000;
                return scoreToConvert.ToString() + "K";
            }
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

            IncrementScoreBy(OriginalDataFrom1978.s_saucerScores[playerShots]);

            ++saucerKills;
        }

        /// <summary>
        /// Adds to the score whilst ensuring at 1500 the player gets an extra life.
        /// </summary>
        /// <param name="amount"></param>
        private void IncrementScoreBy(int amount)
        {
            int scoreBefore = score;

            score += amount;

            CheckForBonusLife(scoreBefore);
        }

        /// <summary>
        /// Called when player shoots invader.
        /// </summary>
        /// <param name="invaderRow"></param>
        internal void InvaderHit(int invaderRow)
        {
            if (invaderRow < 0 || invaderRow > 5) throw new ArgumentOutOfRangeException(nameof(invaderRow), "5 invader rows, 0..4");

            IncrementScoreBy(OriginalDataFrom1978.s_invaderPoints[invaderRow]);

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