# RW-SlugBrain
WIP BepInEx plugin for Rain World that aims to automate slugcat. TAS tools (Tool-Assisted Slowrun).

The AI is roughly built around how the vanilla game handles AI, with various modules that handle different elements of survival, like food and prey tracking. <br>
There is no machine learning involved, so progress between runs is dependent on development of the code and little bits of RNG here and there. <br>
The pathfinding within rooms is done using a DIY A* algorithm. <br>
The AI has some advantage over the typical new player because it can see food and threats in adjacent rooms, and has the Overseers' shelter knowledge, but this is probably less of an advantage than a player with 2+ completed runs (or just loads of time in the area) would have. 


## Current state

- custom pathfinding implemented
- basic jump pathing implemented
- for some reason it still favours jumping over walking on some flat terrain
- there are some quirks with the inputs it tries to use based on where it wants to go that still need to be ironed out
- need to add more jump types (pouncing, etc.)
- need to add threat tracking 
- I eventually want to add options to enable some TAS-only moves like wiggle flight :)


## FAQ
**Are you working with \<x modder>?** <br>
Currently, no. 

**Isn't this similar to \<x mod>?** <br>
Maybe. No mods have been publically released in the Rain World Discord or published to RainDB that do the same thing as SlugBrain. Yes, I know the slugpup mod exists. 

**Why don't you just make NPC slugcats?** <br>
I don't want to lol. The game playing itself seems weird? Okay. 
