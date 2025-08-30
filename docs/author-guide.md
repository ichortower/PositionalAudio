# Positional Audio - Author Guide

This document explains how to use this mod to add your position-dependent
audio to Stardew Valley.

## Contents

* [Adding Audio Items](#adding-audio-items)
  * [Data Format](#data-format)
  * [Content Patcher example](#content-patcher-example)
* [Audio Behavior](#audio-behavior)
  * [Audio Types](#audio-types)
  * [Refreshing](#refreshing)
  * [Calculating Volume](#calculating-volume)
  * [Cues With Multiple File Paths](#cues-with-multiple-file-paths)
* [New Game State Queries](#new-game-state-queries)
  * [NPC Position](#npc-position)
  * [NPC Animations](#npc-animations)


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
like most 1.6-era data items. The object has the following format:

### Data Format

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
allow the active items to change in (near) real time: see the
[Refreshing](#refreshing) section for details.

This mod also adds [some queries](#new-game-state-queries) to provide a few
NPC status checks which may be helpful.

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
cue's native volume, so e.g. `0.85` would be 85%.

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
a percentage of the normal volume, so e.g. `0.1` would be 10%.

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
position changes. See [Calculating Volume](#calculating-volume) for more
details.

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


## Content Patcher Example

Let's say you want to make Pam's television in the trailer audible to the
player. The patches to accomplish this might look like this:

```js
{
  "Target": "Data/AudioChanges",
  "Action": "EditData",
  "Entries": {
    "{{ModId}}_PamsTeeveeTrailerCue": {
      "Id": "{{ModId}}_PamsTeeveeTrailerCue",
      "FilePaths": [
        "{{AbsoluteFilePath: assets/music/PamTeevee1.ogg}}",
        "{{AbsoluteFilePath: assets/music/PamTeevee2.ogg}}",
        "{{AbsoluteFilePath: assets/music/PamTeevee3.ogg}}"
      ],
      "Category": "Music",
      "StreamedVorbis": true,
      "Looped": true
    }
  }
},
{
  "Target": "Mods/ichortower.PositionalAudio/Data",
  "Action": "EditData",
  "Entries": {
    "{{ModId}}_PamsTeeveeTrailerPA": {
      "Condition": "ichortower.PositionalAudio_NPC_ANIMATING Pam Here pam_sit_down",
      "CueName": "{{ModId}}_PamsTeeveeTrailerCue",
      "LocationName": "Trailer",
      "MinimumBgmVolume": 0.1,
      "Radius": {
        "Floor": 3.0,
        "Shelf": 3.0,
        "Maximum": 15.0
      },
      "TilePosition": { "X": 15, "Y": 8 }
    }
  }
}
```

This creates an audio item centered on the television which will play whenever
Pam is in the trailer sitting on her couch. The maximum radius is high enough
that it is faintly audible in Penny's room.

Since the cue is `Looped`, one of the three tracks will be selected on location
entry or activation time and will play continuously. If the player leaves and
re-enters the location, a new one will be chosen (see [Cues With Multiple File
Paths](#cues-with-multiple-file-paths)).


## Audio Behavior

### Audio Types

The `Category` of an audio item determines its behavior in this mod in a few
ways, beyond just determining which volume slider in the game options applies
to it.

First, the data asset field `RepeatDelay` applies only to cues in the Sound
category, and is ignored for Music and Ambient cues.

Second, the field `Looped` in a Music or Ambient cue applies normally, but this
mod will automatically restart any such cue that should be playing but has
stopped. The result is that all music or ambient cues will (effectively) loop
when used with this framework, but if you don't specify `"Looped": true`, you
will hear a brief fade-in as the mod restarts the cue and gently ramps up its
volume. This may be what you want, but be aware of it.

### Refreshing

In order to allow audio to begin playing as soon as possible (for example, as
soon as an NPC arrives at the location that an audio item checks for), this mod
attempts to keep the active audio items up to date in real time, without
requiring user action, by reexamining the data asset with some frequency. This
occurs whenever one of the following happens:

1. The player changes locations.
2. The in-game time advances to the next 10 minutes.
3. Any NPC leaves their current schedule spot and departs for the next one.
4. Any NPC arrives at their next schedule spot.
5. Any NPC starts or stops playing a schedule animation.

With the exception of the player changing location (which is tied to the
PlayerWarped event), the mod checks for state changes only every 20 frames, in
an attempt to reduce performance impact. This means that on average, you should
expect a delay of 10 frames before audio starts or stops in response to a
change in condition evaluation.

(I have not profiled this. The state check does not proceed to reevaluation of
the audio items (the costlier operation) unless it sees that something relevant
has changed. The state check is not very costly on its own, but it iterates
over all NPCs, and as that gets worse with more mods installed, I don't think I
want to run that every frame if it can be avoided.)

### Calculating Volume

This mod calculates the volume for its audio items in two ways: first, when a
Music or Ambient item has just started playing, it will start at zero volume
and fade in to its target volume (see below), and likewise when it stops
playing due to condition changes, it will fade out gradually to zero instead of
abruptly stopping. Sound category cues do not have this behavior and will play
immediately at full strength.

Second, when the player moves around in a location with active items, on every
frame that their position changes, each item will calculate the player's
distance and use its `Radius`, `MaximumIntensity`, and `MinimumBgmVolume`
fields to figure out 1. how loud this item should be right now, and 2. how
quiet the background music should be in response. If multiple items are active,
they will all play, but only the quietest calculated BGM volume will apply.

The item's volume scales from 0 (any position outside of the Maximum radius) to
MaximumIntensity (any position within the Floor radius). The intensity is
calculated with a square root curve, which is not linear, but due to how
loudness is perceived, it should sound approximately linear to human ears. The
same applies to the background music, except scaled from MinimumBgmVolume to 1
over the distance from Shelf to Maximum radius.

If a Music category cue's playing volume goes over 0.75 by this calculation, it
will be added to the player's heard music tracks and will show up in the
jukebox like any other. If you don't want your tracks appearing there,
[make sure to account for it](https://stardewvalleywiki.com/Modding:Jukebox_tracks).

### Cues With Multiple File Paths

When patching `Data/AudioChanges`, you can specify as many file paths as you
wish for a given audio cue: as normal, one file will be chosen at random when
the cue is played. For cues with Looped: true, this means that the selected
file will loop and continue to play until the cue stops, typically by
conditions changing or by the player leaving the location. If Looped is false,
the cue will be replayed, so a new random selection will be made.


## New Game State Queries

This mod adds three game state queries which you can use to control when audio
should play. Two of them are variants of each other, so there are only two new
capabilities: checking NPC positions and checking active NPC animations.

### NPC Position

There are two queries to check for an NPC to be at a particular position in a
given location (and not moving to a new spot: see below). One checks for
specific tile coordinates, and the other checks for presence in a rectangular
area, which is useful when an NPC is square walking.

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

**Careful**: both of these queries also rely on the NPC not being in transit to
their next schedule point (thankfully, they are not considered in transit
during a square-walking stop on their schedule). This check addresses the case
where an NPC starts leaving their spot, triggering a refresh, but they are
still calculated to be in the same tile position, so the query would still be
satisfied and the audio would continue until the next refresh, up to seven-ish
seconds later.

### NPC Animations

This query checks whether an NPC is in a given location and currently
performing a schedule animation with a given key:

`ichortower.PositionalAudio_NPC_ANIMATING <name> <location> <animation name>`

Name and location work just like the other queries, and the animation name
should be a key specified in the `Data/animationDescriptions` asset. If the NPC
is in the given location and doing the named animation, this will return true.

