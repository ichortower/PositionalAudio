# Positional Audio - Author Guide

This document explains how to use this mod to add your position-dependent
audio to Stardew Valley.

## Contents

* [Adding Audio Items](#adding-audio-items)
  * [Content Patcher example](#content-patcher-example)
* [Audio Behavior](#audio-behavior)
* [New Game State Queries](#new-game-state-queries)
  * [NPC Position](#npc-position)
  * [NPC Animations](#npc-animations)
  * [Getting Updates](#getting-updates)


## Adding Audio Items

There are two necessary steps to add a positional audio item with this mod.
First, the audio must be in the game's sound bank: if you are using vanilla
sounds or music, you don't need to do anything, but for your own audio, you
must add it via `Data/AudioChanges`. Second, you must edit this mod's data
asset:

`Mods/ichortower.PositionalAudio/Data`

The asset is a `string->object` dictionary. The `string` keys are item IDs, and
should be [unique string
IDs](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID),
like most 1.6-era data items. The model (object) has the following fields:

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Condition</code></td>
<td>string</td>
<td>

A [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries)
specifying the conditions under which this audio item will play. These
conditions are reevaluated with some frequency (but not constantly) in order to
allow the active items to change in (near) real time.

This mod also adds [some queries](#new-game-state-queries) to provide a few
NPC status checks which may be helpful. These, and the TIME query, are why the
reevaluation is as frequent as it is: see that section for more details.

This field is checked after `Location`: see that field for details.

*Default:* `""`

</td>
</tr>

<tr>
<td><code>CueName</code></td>
<td>string</td>
<td>

The id of the audio cue to play. If you added your own audio via
`Data/AudioChanges`, specify its id here.

Note that some of the properties from the sound bank cue itself (Category and
Looped) determine behavior within this mod. See the `RepeatDelay` field and the
[Audio Behavior](#audio-behavior) section for more details.

*Default:* `""`

</td>
</tr>

<tr>
<td><code>Location</code></td>
<td>string</td>
<td>

The id of a location (from `Data/Locations`) in which this item is active.

This works in concert with `Condition` to determine which items will be
available to play. Location is checked first; if it matches the player's
location, Condition is checked next, and only if that also matches will the
audio be available.

It is not currently possible to have a positional audio item be available in
multiple locations; if you need that at this time, define multiple items.

*Default:* `""`

</td>
</tr>

<tr>
<td><code>MaximumIntensity</code></td>
<td>float</td>
<td>

How loud the audio item will be, at maximum, when you have reached the source
area (see `Radius` for more information). This value must range from 0.0 to 1.0
(it will be clamped), and reflects the maximum volume as a percentage of the
cue's native volume.

*Default:* `1.0`

</td>
</tr>

<tr>
<td><code>MinimumBgmVolume</code></td>
<td>float</td>
<td>

How quiet the game's current background music will become when you reach the
closest fade-out distance (see `Radius` for more information). This value must
range from 0.0 to 1.0 (it will be clamped), and reflects the minimum volume as
a percentage of the normal volume.

*Default:* `0.0`

</td>
</tr>

<tr>
<td><code>Radius</code></td>
<td>object</td>
<td>

This object holds three fields of its own, which determine how the audio's
volume is calculated (and, likewise, how quiet the existing background music
will be). Each of them is a floating-point number and will be clamped to be
non-negative (negative values will be replaced with 0.0). They are measured in
tiles and represent a distance from the item's `TilePosition`.

* `Floor`: the radius within which this item will be at its `MaximumIntensity`.
* `Shelf`: the radius within which the normal background music will be at the
  `MinimumBgmVolume`.
* `Maximum`: the radius beyond which this item can no longer be heard (it will
  be at zero volume).

The distances are measured using Vector2.Distance, which is to say using the
hypotenuse of the right triangle connecting the audio and player positions on
the X and Y axes, so the areas are circular and not cleanly divided into whole
tiles.

When the player moves near the audio's tile position within these three
distances, the mod will calculate volumes on every frame that the player's
position changes. See [Audio Behavior](#audio-behavior) for more details.

*Default:* `{"Floor": 2.0, "Shelf": 4.0, "Maximum": 8.0}`

</td>
</tr>

<tr>
<td><code>RepeatDelay</code></td>
<td>array(integer)</td>
<td>

An array of integers from which to pick randomly when pausing between sounds.
**This field applies only if the target audio specified in CueName belongs to
the category "Sound"**.

When using a Sound cue, instead of looping or restarting immediately, the mod
waits some number of milliseconds before allowing the sound to play again. The
number is determined by random selection from this list. For example, with the
value `[1500, 3000]`, the wait would be either 1.5 seconds or 3 seconds. The
chance is equally distributed, so you can specify multiples of a value to give
it a higher chance of being chosen.

*Default:* `[800, 1000, 1200]`

</td>
</tr>

<tr>
<td><code>TilePosition</code></td>
<td>Point</td>
<td>

This object (with two fields, "X" and "Y") is the tile position within the
item's `Location` where the sound originates. This is the point used by the
`Radius` field to calculate distances with the player's position and thereby
calculate volume levels.

*Default:* `{"X": -1, "Y": -1}`

</td>
</tr>

</table>


## Configuration

There is only one config setting for this mod, but it is important: you can
manually set the delay between checking for NPC state updates. The option is
called `PollDelay` and its default value is `20`: every 20 ticks, the mod will
check every NPC to determine whether the overall state has changed: i.e. if any
NPC has started or stopped animating, or started or stopped moving to a new
schedule point. If the state has changed, then all items in the data asset are
rechecked and enabled/disabled as appropriate for the new state.

I have tried to make the mod avoid as much work as possible during this
process, but with unknowable mod loadouts out there, it is possible that every
20 frames is too frequent for some users, so you can increase this value if you
notice any performance issues. Note that the higher this number, the longer on
average you will expect to wait after a state change for audio to turn on or
off in response: the average delay is half this number of frames. So, for
example, if you use the value `60` (one second), you should expect on average
to wait 30 frames (half a second) for changes to be reflected.


## Audio Behavior


## New Game State Queries

This mod adds three game state queries which you can use to control when audio
should play. Two of them are variants of each other, so there are only two new
capabilities: checking NPC positions and checking active NPC animations.

### NPC Position

There are two queries to check for an NPC to be at a particular position in a
given location. One checks for specific tile coordinates, and the other checks
for presence in a rectangular area.

For specific tiles:

`ichortower.PositionalAudio_NPC_POSITION <name> <location> [<x> <y>]+`

The NPC name should be the internal name, as usual, and the location argument
works like any other game state query: it can be `Here`, `Target`, or a given
location's id. You can specify as many x,y pairs as you would like (but you
must give at least one); the query will return true if the named NPC is at any
of the given positions.

For a rectangle:

`ichortower.PositionalAudio_NPC_POSITION_RECT <name> <location> <x> <y> <w> <h>`

This works just like the coordinates version, except it expects exactly four
integers to define a rectangle (x, y, width, and height, in that order). If the
NPC is anywhere within that rectangle, it will return true.

### NPC Animations

This query checks whether an NPC is in a given location and currently
performing a schedule animation with a given key:

`ichortower.PositionalAudio_NPC_ANIMATING <name> <location> <animation name>`

Name and location work just like the other queries, and the animation name
should be a key specified in the `Data/animationDescriptions` asset. If the NPC
is in the given location and doing the named animation, this will return true.

### Getting Updates

NPC position and animation status are notoriously real-time bits of data, so you may be concerned
You can use these queries in your audio items' `Condition` fields without worryin

