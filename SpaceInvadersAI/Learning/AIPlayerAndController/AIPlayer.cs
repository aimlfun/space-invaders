using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.Learning.Utilities;
using System.Diagnostics;
using SpaceInvadersAI.AI;
using static SpaceInvadersAI.Learning.Configuration.PersistentConfig;

namespace SpaceInvadersAI.Learning.AIPlayerAndController;

/// <summary>
/// Represents a player controlled by an AI.
/// </summary>
internal class AIPlayer : IDisposable
{
    //    █      ███            ████    █         █     █   █   █████   ████
    //   █ █      █             █   █   █        █ █    █   █   █       █   █
    //  █   █     █             █   █   █       █   █    █ █    █       █   █
    //  █   █     █             ████    █       █   █     █     ████    ████
    //  █████     █             █       █       █████     █     █       █ █
    //  █   █     █             █       █       █   █     █     █       █  █
    //  █   █    ███            █       █████   █   █     █     █████   █   █

    #region DEBUG
    /// <summary>
    /// If true, it will overlay what the AI sees on the screen.
    /// </summary>
    private const bool c_debugOverlayWhatAISees = false;

    /// <summary>
    /// If true, it will show the transparent colour film of the game (that makes the white pixels coloured).
    /// </summary>
    private const bool c_debugShowColourFilm = false;
    #endregion

    /// <summary>
    /// The game controller for this player.
    /// </summary>
    internal readonly GameController gameController;

    /// <summary>
    /// Used to uniquely reference a player. 
    /// It's just an ever increasing number assigned to each player.
    /// </summary>
    internal int uniquePlayerIdentifier;

    /// <summary>
    /// Used to detect when to paint "game over" on the screen or not.
    /// If WasDead = false, GameOver = true => paint "game over" on the screen THEN update the PictureBox, and set WasDead = true.
    /// If WasDead = true, GameOver = true => do nothing.
    /// </summary>
    internal bool WasDead = false;

    /// <summary>
    /// Tracks whether the player is dead. true = dead (doesn't move etc), false = moving.
    /// </summary>
    internal bool IsDead
    {
        get { return gameController.IsGameOver; }
    }

    /// <summary>
    /// Contains a pointer to the "brain" that is guiding the player.
    /// Each has their own brain.
    /// </summary>
    internal Brain brainControllingPlayerShip;

    /// <summary>
    /// The last image drawn for this player.
    /// </summary>
    private Bitmap? lastImage = null;

    /// <summary>
    /// The video display for this player.
    /// </summary>
    private readonly VideoDisplay videoDisplay;

    /// <summary>
    /// Used to annotate screens with the brain's name / provenance.
    /// </summary>
    private readonly Font fontAnnotation = new("Courier New", 7, FontStyle.Bold);

    /// <summary>
    /// Warn the developer when the debug is enabled.
    /// </summary>
    static AIPlayer()
    {
        if (c_debugOverlayWhatAISees)
        {
#pragma warning disable CS0162 // Unreachable code detected  <<<-- this code runs when the debug is enabled.
            Debug.WriteLine("c_debugOverlayWhatAISees = true in AIPlayer.cs -> showing AI 56x64px overlay");
#pragma warning restore CS0162 // Unreachable code detected
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="id"></param>
    internal AIPlayer(Brain brain, int id, bool showFrameCounter = false)
    {
        videoDisplay = new VideoDisplay();

        gameController = new(videoDisplay, brain.HighScore, Settings.AIPlaysWithShields, Settings.AIOneLevelOnly, Settings.AIStartLevel, Settings.EndGameIfLifeLost, showFrameCounter);

        uniquePlayerIdentifier = id;

        brainControllingPlayerShip = brain;
        brainControllingPlayerShip.AIPlayer = this;

        // if the AI is reading the screen, adding silly pixels that are unique to this brain will confuse it. Better not to have the annotations.
        if (PersistentConfig.Settings.InputToAI != AIInputMode.videoScreen)
        {
            // we write these to the screen once for performance reasons. Just to show off, we do so using our video display code not onto a Bitmap.
            videoDisplay.DrawString(brain.Provenance.ToUpper().Replace("/", "-"), new Point(216 - 8 - brain.Provenance.Length * 8, OriginalDataFrom1978.s_scorePlayer2Rectangle.Y)); // alphanumeric sprites are 8px wide

            // remove CREDIT 00 using fill, and replace with brain's name.
            videoDisplay.FillRectangle(Color.Black, new Rectangle(136, OriginalDataFrom1978.s_credit_00_position.Y, 87, 11));
            videoDisplay.DrawString(brain.Name, new Point(216 - brain.Name.Length * 8, OriginalDataFrom1978.s_credit_00_position.Y)); // alphanumeric sprites are 8px wide
        }
    }

    /// <summary>
    /// Moves the player using AI.
    /// </summary>
    internal void Move()
    {
        // no movement if dead...
        if (gameController.IsGameOver) return;

        // don't need to call neural network until the game is ready
        if (gameController.PlayerIsReady)
        {
            // The neural network data, either then internal structure (ref. alien + alive status etc) or the pixels on the screen.
            double[] neuralNetworkInput = PersistentConfig.Settings.InputToAI switch
            {
                AIInputMode.videoScreen => gameController.AIGetShrunkScreen(),// (shrunk to 56x64)
                AIInputMode.internalData => gameController.AIGetObjectArray(),
                AIInputMode.radar => gameController.AIGetRadarArray(),
                _ => throw new NotImplementedException(),
            };

            brainControllingPlayerShip.SetInputValues(neuralNetworkInput);

            // ask the neural to use the input and decide what to do with the car
            Dictionary<string, double> outputFromNeuralNetwork = brainControllingPlayerShip.FeedForward(); // process inputs

            if (brainControllingPlayerShip.IsDead)
            {
                gameController.AbortGame();
                return;
            }

            // two approaches to moving the player...
            if (!PersistentConfig.Settings.UseActionFireApproach)
            {
                AIChoosesXPositionOfPlayerAndWhetherToFire(outputFromNeuralNetwork);
            }
            else
            {
                AIChoosesActionToControlPlayer(outputFromNeuralNetwork);
            }
        }

        gameController.Play();
    }

    /// <summary>
    /// The neural network is asked to decide where to move the player to, and whether to fire
    /// </summary>
    /// <param name="outputFromNeuralNetwork"></param>
    private void AIChoosesXPositionOfPlayerAndWhetherToFire(Dictionary<string, double> outputFromNeuralNetwork)
    {
        // output is a value between 0 and 1, so multiply by the screen width to get the desired position
        int desiredPosition = (int)Math.Round(outputFromNeuralNetwork["desired-position"] * 208 /* OriginalDataFrom1978.c_screenWidthPX - 16*/ ); // where it wants to go

        // clamp the value to the screen width, just to be safe
        desiredPosition = desiredPosition.Clamp(0, 208 /* OriginalDataFrom1978.c_screenWidthPX - 16 */ );

        gameController.MovePlayerTo(desiredPosition);

        // fire if the output is greater than 0.5
        double fire = outputFromNeuralNetwork["fire"];

        // the only way to know intent of fire or not, is to decide what indicates "fire". In this case, it's 0.65f.
        if (fire >= 0.65f) gameController.RequestPlayerShipFiresBullet();
    }

    /// <summary>
    /// In this approach, the neural network is asked to decide what action to take.
    /// It has 6 possible actions, ranging from moving left, to moving right, to firing.
    /// </summary>
    /// <param name="outputFromNeuralNetwork"></param>
    private void AIChoosesActionToControlPlayer(Dictionary<string, double> outputFromNeuralNetwork)
    {
        int action = (int)Math.Round(outputFromNeuralNetwork["action"] * 5);

        // outside range, does not move or fire
        if (action < 0 || action > 5) action = 2;

        switch (action)
        {
            case 0:
                gameController.SetPlayerMoveDirectionToLeft();
                break;

            case 1:
                gameController.SetPlayerMoveDirectionToLeft();
                gameController.RequestPlayerShipFiresBullet();
                break;

            case 2:
                gameController.CancelMove(); // no move
                break;

            case 3:
                gameController.CancelMove();
                gameController.RequestPlayerShipFiresBullet();
                break;

            case 4:
                gameController.SetPlayerMoveDirectionToRight();
                gameController.RequestPlayerShipFiresBullet();
                break;

            case 5:
                gameController.SetPlayerMoveDirectionToRight();
                break;
        }
    }

    /// <summary>
    /// Draws the game to a bitmap (we rendered it to a back buffer).
    /// </summary>
    internal Bitmap Draw()
    {
        // if the game is over, just return the last image drawn. <performance improvement>
        if (!WasDead || lastImage is null)
        {
            if (gameController.IsGameOver) WasDead = true;

            Bitmap latestVideoDisplayContent = videoDisplay.GetVideoDisplayContent();

            // if we are debugging, and we want to see what the AI sees, then overlay the AI's input onto the screen.
            if (PersistentConfig.Settings.InputToAI == AIInputMode.videoScreen)
            {
                LabelVideoScreenWithNameAndProvenancePlusPixelOverlay(latestVideoDisplayContent);
            }

            // if you wish to see the regions we emulate the coloured films, then enable this.
            if (c_debugShowColourFilm)
            {
#pragma warning disable CS0162 // Unreachable code detected
                DrawFilmThatChangesWhitePixelsToGreenOrMagenta(latestVideoDisplayContent);
#pragma warning restore CS0162 // Unreachable code detected
            }

            // when it's game over, we darken the screen a little, so you can distinguish between whether game is over or not.
            if (gameController.IsGameOver)
            {
                DarkenTheDisplayAsItIsGameOver(latestVideoDisplayContent);
            }

            lastImage?.Dispose();
            lastImage = latestVideoDisplayContent;
        }

        return lastImage;
    }

    /// <summary>
    /// Darkening the display makes it obvious during training which ones are finished with during a training session.
    /// </summary>
    /// <param name="latestVideoDisplayContent"></param>
    private static void DarkenTheDisplayAsItIsGameOver(Bitmap latestVideoDisplayContent)
    {
        using Graphics graphics = Graphics.FromImage(latestVideoDisplayContent);
        using SolidBrush darken = new(Color.FromArgb(150, 20, 20, 20)); // transparent dark grey

        graphics.FillRectangle(darken, 0, 0, 224, 256);
        graphics.Flush();
    }

    /// <summary>
    /// The film is low tech (something like acetate) but effective. The side effect is bullets travelling under the film change from white.
    /// Later versions of Space Invaders dispense with this and use colour processing hardware.
    /// </summary>
    /// <param name="latestVideoDisplayContent"></param>
    private static void DrawFilmThatChangesWhitePixelsToGreenOrMagenta(Bitmap latestVideoDisplayContent)
    {
        using Graphics graphics = Graphics.FromImage(latestVideoDisplayContent);

        Bitmap overlay = VideoDisplay.BitMapTransparentRedGreen(); // get the coloured overlay
        graphics.DrawImageUnscaled(overlay, 0, 0);

        graphics.Flush();
    }

    /// <summary>
    /// If debug is enabled, we overlay the AI's input onto the screen - it is 56x64 pixels that we enlarge to 224x256 to overlay.
    /// Using that you can see the 1/0's that the AI is seeing.
    /// We also want to display the name of the AI, and the provenance of the AI. But if we do that in the video display, then the AI will see it.
    /// That will confuse it, as it differs each time an AI runs. So we add labels post processing using a .DrawString().
    /// </summary>
    /// <param name="latestVideoDisplayContent"></param>
    private void LabelVideoScreenWithNameAndProvenancePlusPixelOverlay(Bitmap latestVideoDisplayContent)
    {
        using Graphics graphics = Graphics.FromImage(latestVideoDisplayContent);

        // show the the AI overlay
        if (c_debugOverlayWhatAISees)
        {
#pragma warning disable CS0162 // Unreachable code detected - code is used if you enable the debug option
            using Bitmap overlay = VideoDisplay.VideoShrunkForOverlay(videoDisplay.VideoShrunkForAI() /* what the AI will see*/); // neuralNetworkInput is what the AI saw, then moved
            graphics.DrawImageUnscaled(overlay, 0, 0);
#pragma warning restore CS0162 // Unreachable code detected
        }

        // if the AI is reading the screen, we didn't write this to the screen otherwise the AI will get confused.
        // we decorate it here, so it is still present but not in the AI's input.

        // we write these to the screen once for performance reasons. Just to show off, we do so using our video display code not onto a Bitmap.

        DrawStringRightAlignedTo(brainControllingPlayerShip.Provenance, fontAnnotation, Brushes.White, 224 - 16, 23, graphics);

        // remove the CREDIT 00 or the previous label
        graphics.FillRectangle(Brushes.Black, 130, 240, 93, 11);
        DrawStringRightAlignedTo(brainControllingPlayerShip.lineage, fontAnnotation, Brushes.White, 224 - 8, 240, graphics);
    }

    /// <summary>
    /// Draws a string right aligned to the specified point.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="font"></param>
    /// <param name="brush"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="graphics"></param>
    private static void DrawStringRightAlignedTo(string label, Font font, Brush brush, int x, int y, Graphics graphics)
    {
        label = label.ToUpper();
        // measure the label and draw it right aligned
        SizeF size = graphics.MeasureString(label, font);

        graphics.DrawString(label, font, brush, new Point(x - (int)size.Width, y));
    }

    /// <summary>
    /// Enables GC of the image when not in a PictureBox.
    /// </summary>
    public void Dispose()
    {
        lastImage = null;
    }
}