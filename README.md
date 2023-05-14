# Space Invaders

       ███    ████      █      ███    █████            ███    █   █   █   █     █     ████    █████   ████     ███ 
      █   █   █   █    █ █    █   █   █                 █     █   █   █   █    █ █    █   █   █       █   █   █   █
      █       █   █   █   █   █       █                 █     ██  █   █   █   █   █   █   █   █       █   █   █
       ███    ████    █   █   █       ████              █     █ █ █   █   █   █   █   █   █   ████    ████     ███
          █   █       █████   █       █                 █     █  ██   █   █   █████   █   █   █       █ █         █
      █   █   █       █   █   █   █   █                 █     █   █    █ █    █   █   █   █   █       █  █    █   █
       ███    █       █   █    ███    █████            ███    █   █     █     █   █   ████    █████   █   █    ███      


From the blog post (coming soon) - https://aimlfun.com/space-invaders/, that has no affiliation with Taito Corp. / Square Enix and respects their trademark & copyright.

This application requires .net7 and Visual Studio 2022 (Community works).

1. Download https://visualstudio.microsoft.com/vs/community/
2. Download the source-code as a ZIP. 
3. Save it where you like - I tend to use c:\repos\ai\{folder-name}, feel free to choose a better folder.
4. Open the solution, and enjoy!

Any problems? Post a comment on my blog, and I will happily try to assist.

       █     █    
        █   █     
       ███████    
      ██ ███ ██   
     ███████████  
     █ ███████ █  
     █ █     █ █  
        ██ ██     

## There are several projects in the solution:
- SpaceInvaders, the user controlled version : Arrow keys move, space bar to fire, P to pause. See note below, the primary purpose of this is to enable a developer to debug game play "issues".
- SpaceInvadersAI, the AI learning version. Check out my blog to learn more. 
- SpaceInvadersCore, the game itself used by both AI & UI versions.
- SpaceInvaderCommentDecoration, used to create the large comments in Space Invader font!

          █       
         ███      
         ███      
     ███████████  
    █████████████ 
    █████████████ 
    █████████████ 
    █████████████ 
    
## PLEASE NOTE:

Space Invaders^TM^, the arcade game was created by Japanese engineer and game designer **Tomohiro Nishikado** (https://en.wikipedia.org/wiki/Tomohiro_Nishikado) in 1978; and produced by Japanese electronic game manufacturer **Taito Corporation**. 

The current owner of the copyright and trademark is with "Square Enix" (see https://en.wikipedia.org/wiki/Square_Enix) who acquired Taito Corp, their website is https://spaceinvaders.square-enix-games.com/

**Please be respectful of their ownership and rights.**

I have intentionally solely built the core parts for training AI (and testing as a user), but have not copied the game in its entirety - it doesn't have any attract screens etc. 
If you want to play the original (running their Zilog 8080 code), please look for it on MAME. It was made available. Where one stands legally doing so, one will need to check with Square Enix.