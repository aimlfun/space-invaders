Templates are intentionally saved with the "input" prefix plus the "feedback" and then the level, score and number of invaders / saucers killed:

	{input} {feedback} lvl={level} s={score} ik={invaders-killed} sk={saucers-killed}


input:
	"internalData" - internal data is coming from the game itself, in the form of the reference-alien, a 1/0 for whether the alien 1-55 is alive, the position of the bullets.
	"videoScreen" - data is a low resolution image of the screen, with 1/0 for whether the pixel is on or off
	"radar" - data is comprised of 2 radars, one for the aliens/bullets and one for the shields.

output:
	"action" - the AI returns a number 1-5 that determines move left, right, or shoot (or combinations thereof)
	"position" - the AI returns two numbers; the x-position to place the ship, and whether to fire


Templates are copied into the bin/templates folder, and must be declared using the AI configuration where you state what level(s) the template is for, and what the template is called.