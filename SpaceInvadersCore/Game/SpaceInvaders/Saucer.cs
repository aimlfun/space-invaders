using SpaceInvadersCore.Game;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders;

/// <summary>
/// Represents the flying saucer that goes across the top of the screen.
/// </summary>
internal class Saucer
{
    //   ███      █     █   █    ███    █████   ████
    //  █   █    █ █    █   █   █   █   █       █   █
    //  █       █   █   █   █   █       █       █   █
    //   ███    █   █   █   █   █       ████    ████
    //      █   █████   █   █   █       █       █ █
    //  █   █   █   █   █   █   █   █   █       █  █
    //   ███    █   █    ███     ███    █████   █   █

    //        ██████         
    //      ██████████       
    //     ████████████      
    //    ██ ██ ██ ██ ██     
    //   ████████████████    
    //     ███  ██  ███      
    //      █        █       

    /// <summary>
    /// Debugging.
    /// </summary>
    private const bool c_showHitBox = false;

    /// <summary>
    /// Location of the flying saucer (horizontal).
    /// </summary>
    internal int X;

    /// <summary>
    /// Location of the flying saucer (vertical).
    /// </summary>
    internal int Y;

    /// <summary>
    /// Direction + speed of the flying saucer.
    /// </summary>
    internal int XDirection = 2;

    /// <summary>
    /// Dimensions of the flying saucer.
    /// Technically it's smaller, but this we're using the image of the saucer from the 1978 game - that has
    /// blank pixels either side to avoid having to erase it as it moves.
    /// </summary>
    internal static Size Dimensions = new()
    {
        Width = 24,
        Height = 8
    };

    /// <summary>
    /// This is the video display that the saucer is drawn on.
    /// </summary>
    private readonly VideoDisplay videoScreen;

    /// <summary>
    /// Saucer appears every 600 frames (every minute if I've done the maths right?)
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="playerShots"></param>
    internal Saucer(VideoDisplay screen, int playerShots)
    {
        videoScreen = screen;

        // The flying saucer's direction is linked to the player's shot count.The lowest bit of the count determines which side of the screen the saucer comes from.
        // If the saucer appears after an even number of player shots then it comes from the right.After an odd number it comes from the left.
        if (playerShots % 1 == 1)
        {
            XDirection = 2;
            X = 0; // start mostly offscreen.
        }
        else
        {
            XDirection = -2;
            X = OriginalDataFrom1978.c_screenWidthPX - 2 - Dimensions.Width; // start offscreen.
        }

        Y = OriginalDataFrom1978.c_topOfSaucerLinePX;
    }

    /// <summary>
    /// Moves the saucer.
    /// </summary>
    /// <returns></returns>
    internal void Move()
    {
        EraseSprite();

        X += XDirection;
    }

    /// <summary>
    /// Detects whether the saucer has gone off the screen.
    /// </summary>
    internal bool IsOffScreen
    {
        get
        {
            int dir = Math.Sign(XDirection);

            // gone right hand side
            if (dir == 1 && X > OriginalDataFrom1978.c_screenWidthPX - 2 - Dimensions.Width) return true;

            // reached left hand side
            if (dir == -1 && X < 2) return true;

            // still on screen
            return false;
        }
    }

    /// <summary>
    /// Saucers are ellipses, but to save on maths, we'll approximate to a rectangle they occupy.
    /// </summary>
    /// <returns></returns>
    private Rectangle HitBox()
    {
        return new Rectangle(X + 4, Y + 1, Dimensions.Width - 8, Dimensions.Height - 1);
    }

    /// <summary>
    /// Draws the flying saucer.
    /// </summary>
    /// <param name="g"></param>
    internal void DrawSprite()
    {
        /*
            SpriteSaucer width: 24 X height: 8
                    
                     ██████         
                   ██████████       
                  ████████████      
                 ██ ██ ██ ██ ██     
                ████████████████    
                  ███  ██  ███      
                   █        █       
         
         */

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox());
#pragma warning restore CS0162 // Unreachable code detected

        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["SpriteSaucer"], X, Y);
    }

    /// <summary>
    /// Erases the flying saucer.
    /// </summary>
    /// <param name="g"></param>
    internal void EraseSprite()
    {
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["SpriteSaucer"], X, Y);

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Black, HitBox());
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Detects if player bullet hit saucer.
    /// </summary>
    /// <param name="bulletLocation"></param>
    /// <returns></returns>
    internal bool BulletHit(Point bulletLocation)
    {
        return HitBox().Contains(bulletLocation);
    }
}