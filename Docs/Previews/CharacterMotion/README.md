# Actual Unity Character Previews

Generated from the committed FBX, prefab and procedural bone evaluator in Unity
2022.3.32f1 on 2026-10-17. No Play Mode, input automation or generated concept images
were used. Each motion frame bakes the real posed skinned mesh before Camera.Render.

| Action | Animation | Eight sampled phases | Timing |
| --- | --- | --- | --- |
| Idle | [GIF](Idle.gif) | [Sheet](Idle-Sequence.png) | 3.2 s, 20 fps |
| Walk | [GIF](Walk.gif) | [Sheet](Walk-Sequence.png) | One complete cycle, 32 fps |
| Turn | [GIF](Turn.gif) | [Sheet](Turn-Sequence.png) | 1.6 s, 20 fps |
| Left-hand pickup | [GIF](Pickup.gif) | [Sheet](Pickup-Sequence.png) | 1.2 s, 30 fps |

[Imported model four views](Protagonist-FourViews.png)

[Saved Room304Greybox camera](Room304.png) shows the actual saved scene at 1280x720.
It is an offline world render; Screen Space Overlay UI is not included. Additional
4:3 and 21:9 renders passed framing/pixel checks and were inspected locally.

Walk is in place at an inspection cadence, not footage of traversing the room.
Runtime gait phase uses measured player velocity. Turn and pickup are one-shot
actions; the GIF viewer's loop resets to their start. The pickup table/paper are
disposable preview geometry; actual sample clue contact is checked separately.

Regenerate using the commands in [Verification](../../../Tools/Verification/README.md).
The character remains first-pass art: simplified face/fingers, coarse garment
attachment and linear skinning. See [CharacterMotion](../../CharacterMotion.md)
for behavior, limitations and the manual acceptance checklist.
