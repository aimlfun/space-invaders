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
    internal readonly static Size Dimensions = new()
    {
        Width = 16,
        Height = 8
    };

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
        return new Rectangle(X + xAdjustment - 1, Y, Dimensions.Width + widthAdjustment + 1, Dimensions.Height);
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

        if (DebugSettings.c_debugSpaceInvaderDrawHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox(invaderNumber, referenceAlienPosition));

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
                videoScreen.DrawSprite(OriginalSpritesFrom1978.Get(OriginalDataFrom1978.s_spaceInvaderImageFramesIndexedByRow[row][frame]), X, Y);
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
                videoScreen.DrawSprite(OriginalSpritesFrom1978.Get("AlienExplode"), X, Y);
                break;

            case InvaderState.dead:
                throw new ApplicationException("why paint dead invaders?");
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
        /*
           This takes the alien index and converts it to a screen position. Every 11, it adds 16 pixels (11 invaders per row). the 0..11 is multiplied by 16 for the X position.
           Remember on the original, it uses Cartesian (0,0) is bottom left, so adding 16 goes upwards.
           
                 GetAlienCoords:
                ; Convert alien index in L to screen bit position in C,L.
                ; Return alien row index (converts to type) in D.
                
                017A: 16 00           LD      D,$00               ; Row 0
                017C: 7D              LD      A,L                 ; Hold onto alien index 
                017D: 21 09 20        LD      HL,$2009            ; Get alien X ...
                0180: 46              LD      B,(HL)              ; ... to B
                0181: 23              INC     HL                  ; Get alien y ...
                0182: 4E              LD      C,(HL)              ; ... to C
                0183: FE 0B           CP      $0B                 ; Can we take a full row off of index?   << 11
                0185: FA 94 01        JP      M,$0194             ; No ... we have the row
                0188: DE 0B           SBC     A,$0B               ; Subtract off 11 (one whole row)
                018A: 5F              LD      E,A                 ; Hold the new index
                018B: 78              LD      A,B                 ; Add ...
                018C: C6 10           ADD     A,$10               ; ... 16 to bit ...                      << 16 per row
                018E: 47              LD      B,A                 ; ... position Y (1 row in rack)
                018F: 7B              LD      A,E                 ; Restore tallied index
                0190: 14              INC     D                   ; Next row
                0191: C3 83 01        JP      $0183               ; Keep skipping whole rows
                ;
                0194: 68              LD      L,B                 ; We have the LSB (the row)
                0195: A7              AND     A                   ; Are we in the right column?
                0196: C8              RET     Z                   ; Yes ... X and Y are right
                0197: 5F              LD      E,A                 ; Hold index
                0198: 79              LD      A,C                 ; Add ...
                0199: C6 10           ADD     A,$10               ; ... 16 to bit ...                     << 16 per column
                019B: 4F              LD      C,A                 ; ... position X (1 column in rack)
                019C: 7B              LD      A,E                 ; Restore index
                019D: 3D              DEC     A                   ; We adjusted for 1 column
                019E: C3 95 01        JP      $0195               ; Keep moving over column
         */

        row = invaderNumber / 11;
        col = invaderNumber % 11; // 0-10

        // I am not sure I agree / understand the disassembly explanation. The top row according to matching to YouTube recordings of the real game, have the top alien at 64px.
        // 5 rows, mean 4 rows @16px => 64px, the bottom row top would be at 64px+64px => 128.

        // Set the x and y coordinates of the space invader, positioning them spaced out in rows and cols.

        X = referenceAlien.X + col * (Dimensions.Width); // multiply by 16, width of sprite
        Y = referenceAlien.Y - row * (Dimensions.Height + 8); // 8px height of sprite + 8px gap
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

        if (DebugSettings.c_debugSpaceInvaderDrawHitBox) videoScreen.DrawRectangle(Color.Black, HitBox(invaderNumber, referenceAlienPosition));

        switch (state)
        {
            case InvaderState.alive:
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Get(OriginalDataFrom1978.s_spaceInvaderImageFramesIndexedByRow[row][1 - frame]), X, Y);
                break;

            case InvaderState.exploding:
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("AlienExplode"), X, Y);
                break;

            case InvaderState.dead:
                throw new ApplicationException("why paint dead invaders?");
        }
    }
}