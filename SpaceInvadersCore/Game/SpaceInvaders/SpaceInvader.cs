using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Data;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders;

/// <summary>
/// Represents a single Space Invader.
/// </summary>
internal static class SpaceInvader
{
    //   ███    ████      █      ███    █████            ███    █   █   █   █     █     ████    █████   ████
    //  █   █   █   █    █ █    █   █   █                 █     █   █   █   █    █ █    █   █   █       █   █
    //  █       █   █   █   █   █       █                 █     ██  █   █   █   █   █   █   █   █       █   █
    //   ███    ████    █   █   █       ████              █     █ █ █   █   █   █   █   █   █   ████    ████
    //      █   █       █████   █       █                 █     █  ██   █   █   █████   █   █   █       █ █
    //  █   █   █       █   █   █   █   █                 █     █   █    █ █    █   █   █   █   █       █  █
    //   ███    █       █   █    ███    █████            ███    █   █     █     █   █   ████    █████   █   █

    //      ████          █     █         ██
    //   ██████████     █  █   █  █      ████  
    //  ████████████    █ ███████ █     ██████     
    //  ███  ██  ███    ███ ███ ███    ██ ██ ██ 
    //  ████████████    ███████████    ████████    
    //     ██  ██        █████████       █  █      
    //    ██ ██ ██        █     █       █ ██ █   
    //  ██        ██     █       █     █ █  █ █  

    /// <summary>
    /// The 3 states an invader can be in.
    /// </summary>
    internal enum InvaderState { alive, exploding, dead };

    /// <summary>
    /// Dimensions of our space invader.
    /// </summary>
    internal static Size Dimensions = new()
    {
        Width = 16,
        Height = 8
    };

    /// <summary>
    /// Debugging.
    /// </summary>
    private const bool c_showHitBox = false;

    /// <summary>
    /// We detect collision on the bullet, then apply this to determine which Space Invader was hit.
    /// </summary>
    /// <param name="invaderNumber"></param>
    /// <param name="referenceAlienPosition"></param>
    /// <returns></returns>
    private static Rectangle HitBox(int invaderNumber, Point referenceAlienPosition)
    {
        GetPosition(referenceAlienPosition, invaderNumber, out int row, out _, out int X, out int Y);

        int xAdjustment = ((row != 0) ? 1 : 0) + ((row == 4) ? 1 : 0);
        int widthAdjustment = ((row != 0) ? -1 : 0) + ((row == 4) ? -3 : 0);

        // invader on top row is 8x8, next 2 rows  is 11x8 ..next 2 rows 12x8, so we need to adjust the hitbox
        return new Rectangle(X + 2 + xAdjustment - 1 - 2, Y, Dimensions.Width - 4 + widthAdjustment + 1 + 4, Dimensions.Height);
    }

    /// <summary>
    /// Draws a Space Invader at the position specified.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="invaderNumber"></param>
    /// <param name="state"></param>
    /// <param name="referenceAlienPosition"></param>
    /// <param name="frame"></param>
    internal static void DrawSprite(VideoDisplay videoScreen, int invaderNumber, InvaderState state, Point referenceAlienPosition, int frame)
    {
        GetPosition(referenceAlienPosition, invaderNumber, out int row, out _, out int X, out int Y);

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox(invaderNumber, referenceAlienPosition));
#pragma warning restore CS0162 // Unreachable code detected

        switch (state)
        {
            case InvaderState.alive:
                /*

                    AlienSprA0/A1 width: 16 X height: 8
                          ████      
                       ██████████   
                      ████████████  
                      ███  ██  ███  
                      ████████████  
                         ██  ██     
                        ██ ██ ██    
                      ██        ██  

                          ████      
                       ██████████   
                      ████████████  
                      ███  ██  ███  
                      ████████████  
                        ███  ███    
                       ██  ██  ██   
                        ██    ██    


                    AlienSprB0/B1 width: 16 X height: 8
                         █     █    
                       █  █   █  █  
                       █ ███████ █  
                       ███ ███ ███  
                       ███████████  
                        █████████   
                         █     █    
                        █       █   

                         █     █    
                          █   █     
                         ███████    
                        ██ ███ ██   
                       ███████████  
                       █ ███████ █  
                       █ █     █ █  
                          ██ ██     

                    AlienSprC0/C1 width: 16 X height: 8
                           ██       
                          ████      
                         ██████     
                        ██ ██ ██    
                        ████████    
                          █  █      
                         █ ██ █     
                        █ █  █ █    

                           ██       
                          ████      
                         ██████     
                        ██ ██ ██    
                        ████████    
                         █ ██ █     
                        █      █    
                         █    █     
                
                 */
                videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites[OriginalDataFrom1978.s_spaceInvaderImageFramesIndexedByRow[row][frame]], X, Y);
                break;

            case InvaderState.exploding:
                /*
                         █   █      
                      █   █ █   █   
                       █       █    
                        █     █     
                     ██         ██  
                        █     █     
                       █  █ █  █    
                      █  █   █  █                        
                 */
                videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["AlienExplode"], X, Y);
                break;

            case InvaderState.dead:
                throw new Exception("why paint dead invaders?");
        }
    }

    /// <summary>
    /// Calculate this invader wrt to reference invader.
    /// </summary>
    /// <param name="referenceAlien"></param>
    /// <param name="invaderNumber"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    internal static void GetPosition(Point referenceAlien, int invaderNumber, out int row, out int col, out int X, out int Y)
    {
        row = invaderNumber / 11;
        col = invaderNumber % 11; // 0-10

        // I am not sure I agree / understand the disassembly explanation. The top row according to matching to YouTube recordings of the real game, have the top alien at 64px.
        // 5 rows, mean 4 rows @16px => 64px, the bottom row top would be at 64px+64px => 128.

        // Set the x and y coordinates of the space invader, positioning them spaced out in rows and cols.

        X = referenceAlien.X + col * (Dimensions.Width);
        Y = referenceAlien.Y - row * (Dimensions.Height + 9);
    }

    /// <summary>
    /// Detects whether a bullet hit or not.
    /// </summary>
    /// <param name="bulletLocation"></param>
    /// <param name="invaderNumber"></param>
    /// <param name="referenceAlienPosition"></param>
    /// <returns></returns>
    internal static bool BulletHit(Point bulletLocation, int invaderNumber, Point referenceAlienPosition)
    {
        Rectangle hitbox = HitBox(invaderNumber, referenceAlienPosition);

        return hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y )) ||
               hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 2)) ||
               hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 5));
    }

    /// <summary>
    /// Erases the Space Invader.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="invaderNumber"></param>
    /// <param name="state"></param>
    /// <param name="referenceAlienPosition"></param>
    /// <param name="frame"></param>
    internal static void EraseSprite(VideoDisplay videoScreen, int invaderNumber, InvaderState state, Point referenceAlienPosition, int frame)
    {
        GetPosition(referenceAlienPosition, invaderNumber, out int row, out _, out int X, out int Y);

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Black, HitBox(invaderNumber, referenceAlienPosition));
#pragma warning restore CS0162 // Unreachable code detected

        switch (state)
        {
            case InvaderState.alive:
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites[OriginalDataFrom1978.s_spaceInvaderImageFramesIndexedByRow[row][1-frame]], X, Y);
                break;

            case InvaderState.exploding:
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["AlienExplode"], X, Y);
                break;

            case InvaderState.dead:
                throw new Exception("why paint dead invaders?");
        }
    }
}