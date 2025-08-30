# Positional Audio

A framework mod for Stardew Valley that lets mod authors define background
sounds and music that play when the player is in proximity to them. This is
intended to work like the base game's waterfall sounds, hammering mine rocks
guy, pirate's cove music, and so on: the closer you get to the source, the
louder it becomes, and the game's normal background music becomes quieter so
you can hear the area sound more easily.


## How to Use

As a user, you will need SMAPI 4.1.10+ and Stardew Valley 1.6.15+. Install this
mod like any other (unzip it into your Mods folder), and your other mods that
depend on it will do their work.

As a mod author, this mod provides a data asset which your mod should edit in
order to add entries for your audio. At this time, you are expected to use
[Content
Patcher](https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher)
to do this; I will probably add C# and SMAPI content API support in the future,
even though I expect Content Patcher will suffice for almost everyone.

A more detailed explanation of how to use this mod's features is in the [author
guide](author-guide.md).


## Special Thanks

To Dolphin Is Not a Fish, for their incredible patience.
