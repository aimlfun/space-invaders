using SpaceInvadersCore.Game.Player;
using SpaceInvadersCore;
using System.Drawing;

namespace SpaceInvadersCore.Game;

/// <summary>
/// Represents the game state.
/// </summary>
internal class GameState
{
    //   ████     █     █   █   █████            ███    █████     █     █████   █████
    //  █        █ █    ██ ██   █               █   █     █      █ █      █     █
    //  █       █   █   █ █ █   █               █         █     █   █     █     █
    //  █       █   █   █ █ █   ████             ███      █     █   █     █     ████
    //  █  ██   █████   █   █   █                   █     █     █████     █     █
    //  █   █   █   █   █   █   █               █   █     █     █   █     █     █
    //   ████   █   █   █   █   █████            ███      █     █   █     █     █████

    /// <summary>
    /// # lives remaining
    /// </summary>
    private int lives = 0;

    /// <summary>
    /// Which level we're on.
    /// </summary>
    internal int Level;

    /// <summary>
    /// The number of frames played.
    /// </summary>
    internal int FramesPlayed = 0;

    /// <summary>
    /// Indicates whether the game is over.
    /// </summary>
    private bool gameOver = false;

    /// <summary>
    /// Getter/Setter to indicates whether the game is over.
    /// We require a setter so that we can draw the "game over" message.
    /// </summary>
    internal bool GameOver
    {
        get
        {
            return gameOver;
        }
        set
        {
            if (gameOver == value) return; // no change
            
            gameOver = value;

            if (gameOver) // we've just gone game over, so draw the game over message.
            {
                videoScreen.DrawString("GAME OVER", new Point(76, 50));
            }
        }
    }

    /// <summary>
    /// Indicates whether the Space Invaders reached the bottom of the screen.
    /// </summary>
    private bool invadersReachedBottom = false;

    /// <summary>
    /// If one wants to adjust score where AI failed to stop invaders reaching bottom, this is the value to use.
    /// </summary>
    internal bool InvadersReachedBottom
    {
        get
        {
            return invadersReachedBottom;
        }
        set
        {
            if (value == true) GameOver = true;

            invadersReachedBottom = value;
        }
    }
    
    /// <summary>
    /// How many times the player shot their own shields.
    /// </summary>
    internal int ShieldsHit = 0;

    /// <summary>
    /// The video screen we draw on.
    /// </summary>
    internal VideoDisplay videoScreen;

    /// <summary>
    /// Setter / getter for number of lives remaining?
    /// The setter draws/removes the lives at the bottom left of the screen.
    /// </summary>
    internal int Lives
    {
        get
        {
            return lives;
        }

        set
        {
            if (lives == value) return;

            // remove or add player "ships" accordingly
            if (lives < value) // lives have increased, draw extra
            {
                for (int life = lives; life < value; life++)
                {
                    DrawLife(life);
                }
            }
            else // lives have decreased, erase some (one at a time)
            {
                for (int life = value; life <= lives; life++)
                {
                    EraseLife(life);
                }

            }

            lives = value;

            WriteLivesInBottomLeft();
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="startLevel"></param>
    internal GameState(VideoDisplay screen, int startLevel)
    {
        videoScreen = screen;

        Level = startLevel;
        Reset();
    }

    /// <summary>
    /// Resets the game state.
    /// </summary>
    internal void Reset()
    {
        lives = 0;
        Lives = 3; // standard number of lives
     
        GameOver = false; // player is yet to be killed.
    }

    /// <summary>
    /// Draws additional life at the bottom left.
    /// </summary>
    /// <param name="life"></param>
    private void DrawLife(int life)
    {
        if (life == 0) return; // we draw additional lives, not ALL lives. The life the player is using, isn't counted.

        PlayerShip.DrawPlayerSpriteAt(videoScreen, LifeSpritePosition(life));
    }

    /// <summary>
    /// Removes additional life at the bottom left.
    /// </summary>
    /// <param name="life"></param>
    private void EraseLife(int life)
    {
        if (life == 0) return; // it only shows additional (to the one you're on)

        PlayerShip.ErasePlayerSpriteAt(videoScreen, LifeSpritePosition(life));
    }

    /// <summary>
    /// Return the location where the life sprite should be rendered.
    /// </summary>
    /// <param name="life"></param>
    /// <returns></returns>
    private static Point LifeSpritePosition(int life)
    {
        return new Point(16 + life * 16, OriginalDataFrom1978.c_greenLineIndicatingFloorPX + 8);
    }

    /// <summary>
    /// The bottom left of the screen shows the lives. I don't understand the thinking in the UI. For a while I assumed it was the level
    /// as there are ships indicating lives. But having watched it enough on YouTube, I'm pretty sure it's the lives...
    /// </summary>
    private void WriteLivesInBottomLeft()
    {
        // erase any existing live.
        videoScreen.FillRectangle(Color.Black, new Rectangle(8, OriginalDataFrom1978.c_greenLineIndicatingFloorPX + 1, 16, 8));

        // write the lives in the SI font
        videoScreen.DrawString(Lives.ToString(), new Point(8, OriginalDataFrom1978.c_greenLineIndicatingFloorPX + 1));
    }
}