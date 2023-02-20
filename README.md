# CabinStacker

This is a Stardew Valley mod allowing you to play with as many players as you want (set playerLimit to whatever you want in %appdata%/StardewValley/startup_preferences), without having cabins taking up too much space on your farm.

- Every player entering the main FarmHouse will enter their own instantiated home.

- If a new player wants to join, a new home instance will automatically be created for him.

- You can move your home on the Farm map by standing where you want a cabin to appear and entering `!move` in the chat. You can move it back away the same way.

- You can visit other players' homes by entering `!warp [player_name]` in the chat while already at home.

This mod should only run on the host. It should be compatible with most mods that don't change doors or mailboxes locations, however it does (ab)use harmony, so it will probably have issues with some mods.

Behind the scenes, every player has its own cabin hidden out of bounds, and the host edits parts of the data sent to the clients in order to move them correctly between their cabins and the farm.

I created this mod because I wanted to run a Stardew Valley server for me and my friends, so I use it in conjunction with [this other mod](https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod) which is fully compatible.