# Positional Audio - Author Guide

This document explains how to use this mod to add your position-dependent
audio to Stardew Valley.


## Adding Audio Items

There are two necessary steps to add a positional audio item with this mod.
First, you must add the audio itself via `Data/AudioChanges`, in order to
create an audio cue in the game's sound bank. Second, you must edit this mod's
data asset:

`Mods/ichortower.PositionalAudio/Data`

The asset is a `string->object` dictionary. The `string` keys are note IDs, and
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
reevaluation is as frequent as it is.

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
Looped) determine behavior within this mod.

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

*Default:* `{"Floor": 2.0, "Shelf": 4.0, "Maximum": 8.0}`

</td>
</tr>

<tr>
<td><code>RepeatDelay</code></td>
<td>array(integer)</td>
<td>

*Default:* `[800, 1000, 1200]`

</td>
</tr>

<tr>
<td><code>TilePosition</code></td>
<td>Point</td>
<td>

*Default:* `{"X": -1, "Y": -1}`

</td>
</tr>

</table>
