# Unity API reference

English · [Español](../es/api.md)

Everything is plain C# in `UnityProject/Assets/Scripts/`. You never edit Swift to build an experience: Unity decides, the headset draws. All positions are **Unity world coordinates in meters**.

> Status: the code compiles in the project, but gestures, sensors, anchors and audio still need to be verified on a real Apple Vision Pro. See [limitations](../limitations.md).

## 1. The 5-minute path

1. Put a `VisionObject` on every GameObject that must appear in the headset. Give it a **unique ID**.
2. Pick what you want to do and copy a **template** from `Assets/Scripts/Templates/` (section 7). Rename the class, add it to the object, edit the few lines marked in the comments.
3. Build with **MVP → Build immersive visionOS app** (see [workflow](../unity-workflow.md)).

## 2. `VisionObject` – an object that exists in the headset

Add it next to the mesh. It is the only component an object needs to be visible.

| Member | What it does |
| --- | --- |
| `Id` | Unique, stable ID. Duplicates are ignored with a warning. |
| `SelectionChanged(bool)` | The system tap: the user **looks at the object and pinches**. Fires each time. |
| `IsSelected`, `Select()` | Selection state. A tap toggles it. |
| `BaseColor`, `SelectedColor` | Colors you can change from code. `DisplayColor` is the one shown now. Applies to Box/Sphere; models keep their texture. |
| `AnchorId`, `SetAnchor(id)` | Name of the anchor the object is attached to (empty = free). |
| `AttachToAnchor(id, pos, rot)` | Creates the anchor **only if it does not exist yet** and attaches the object. The transform becomes local to the anchor. |
| `PlaceAnchor(id, pos, rot)` | Like the above but **replaces** an existing anchor. |
| `DetachFromAnchor()` | Frees the object, keeping its current world pose. The anchor is kept. |
| `Shape = Panel`, `PanelTitle`, `PanelText`, `SetPanelText(title, text)`, `PanelFaceUser` | A floating **text card** (see section 7b). Its scale X/Y is its width/height in meters. The object color tints the card's accent bar. |
| `PlayAudio(key)` | Plays a spatial sound from the object (see `VisionAudioSource`). |

An anchored object is **hidden until ARKit locates its anchor**, and while anchored its `transform` is *relative to the anchor*.

## 3. `VisionControls` – read the hands like `Input`

Static, no setup. Call it from any `Update()`. Needs hand-tracking permission.

```csharp
if (VisionControls.GetPinchDown(VisionHand.Right)) Fire();
transform.position = VisionControls.Hand(VisionHand.Right).PinchPoint;
```

| Member | Meaning |
| --- | --- |
| `Hand(hand)` → `VisionHandState` | `Tracked`, `IndexTip`, `ThumbTip`, `PinchPoint` (midpoint), `PinchDistance`, `IsPinching`. |
| `IsTracked(hand)` | The hand is currently seen. |
| `GetPinch(hand)` | Pinch is held. |
| `GetPinchDown(hand)` / `GetPinchUp(hand)` | True only in the frame the pinch starts / ends. |
| `PinchStarted`, `PinchEnded` | The same as events (`Action<VisionHand>`). |
| `PinchStartDistance` (2.5 cm), `PinchEndDistance` (4 cm) | Thresholds with hysteresis; you can change them. |

`VisionHand` is `Left` or `Right`. This pinch is a **distance threshold computed by us**, not Apple's system gesture, so the two can differ slightly.

## 4. `VisionInteractable` – react to the user per object

Add it next to a `VisionObject`. Every event also exists as a `UnityEvent` in the Inspector (`onTapped`, `onGrabBegan`, `onGrabEnded`, `onTouchBegan`, `onTouchEnded`).

| Event | When |
| --- | --- |
| `Tapped` | Look + pinch (the system tap). |
| `GrabBegan` / `GrabMoved(Vector3)` / `GrabEnded` | Look, pinch and move the hand. `GrabMoved` gives the object's new world position. |
| `TouchBegan(hand)` / `TouchEnded(hand)` | A fingertip enters/leaves the object (no pinch). Tick **Touchable**. |

Options: **Grabbable** (the object follows the hand while grabbed), **Detach Anchor On Grab** (an anchored object is released from its anchor when grabbed, so it can move). `IsGrabbed` tells the current state.

## 5. Anchors – make things stay in the real world

An anchor is a named point in the room that ARKit remembers **across app launches**.

```csharp
VisionAnchors.Ensure("lamp", pos, rot);   // create only if missing (keeps the saved one)
VisionAnchors.Place("lamp", pos, rot);    // create or replace
VisionAnchors.Remove("lamp");
VisionAnchors.AnchorUpdated += a => Debug.Log(a.Id + " " + a.Phase + " tracked=" + a.Tracked);
VisionAnchors.TryGet("lamp", out var sample);   // Position, Rotation, Tracked
```

Phases are `added`, `updated`, `removed`. Names are stored on the device; **Borrar anclas** in the debug window forgets them all. Needs the world-sensing permission.

## 6. Low-level events and the world

`VisionInput` (raw events that feed the APIs above):

| Event | Data |
| --- | --- |
| `HandUpdated` | `Hand`, `Tracked`, `IndexTip`, `ThumbTip` (≈30/s per hand) |
| `SurfaceUpdated` | `Id`, `Phase`, `Alignment` (`horizontal`/`vertical`), `Classification` (floor, table, seat, wall…), `Center`, `Normal`, `Width`, `Height` |
| `ObjectDragged` | `ObjectId`, `Phase` (`changed`/`ended`), `Position` |

Surfaces are **estimated rectangles**, not the room mesh. `VisionMap.Mode` = `Off` (default), `Mesh` (wireframe) or `Occlusion` (real surfaces hide virtual objects); it costs CPU and battery. `VisionAudioSource.Play()` plays the clip attached to the object from its position.

## 7. Templates (copy, rename, edit)

In `Assets/Scripts/Templates/`. Comments are in English and Spanish.

| Template | You get |
| --- | --- |
| `TemplateTapChangeColor` | Look + pinch → the object cycles through a list of colors. |
| `TemplatePinchChangeColor` | Pinch with your hand **near** the object → it changes color while pinched. |
| `TemplateGrabHighlight` | Grab and move an object; it highlights while held. |
| `TemplateTouchButton` | A fingertip "button" (no pinch). |
| `TemplateFollowHand` | The object follows your pinch point. |
| `TemplateGrabThenAnchor` | Move an object; where you let go it is **anchored for good**, even after restarting. |
| `TemplateAnchorWithPinch` | Pinch with the left hand to drop an anchor and show an object there. |
| `TemplateMuseumInfo` | Tap a painting → a movable info card appears next to it. |

More complete examples are in `Assets/Scripts/Examples/` (`VisionGrabAndPlace` sets an object on a table or floor when released, `VisionAnchorOnSurface`, `VisionSurfacePlacer`, `VisionDragMover`, `VisionHandPinch`, `VisionTouchable`).

### 7b. Info cards (museum labels)

Set a `VisionObject`'s **Shape** to `Panel` to get a glass card with a title and text, drawn natively by the headset. It is a normal object: add `VisionInteractable` (Grabbable) to let the user **move it**, anchor it, or show/hide it with `SetActive`. `TemplateMuseumInfo` does the classic case: tap a painting, a card with its description appears beside it, move it by hand, tap it to close.

```csharp
card.SetPanelText("Las Meninas", "Diego Velázquez, 1656.");
card.transform.position = painting.transform.position + new Vector3(0.6f, 0, 0);
card.gameObject.SetActive(true);
```

Limits: text and a color bar only (no images, no buttons); the card size is set by its scale, text does not auto-fit; `PanelFaceUser` makes it turn to face the viewer (so its rotation is ignored).

## 8. What you cannot do (by design of visionOS)

- **No gaze cursor or crosshair.** Apps are not told where the user looks. The system lights up (hover effect) the object that will receive the pinch; trust that.
- **Tap and drag always need look + pinch.** `VisionTouchable` and `VisionControls` work from hand data instead.
- The Unity Input System is not used; there is no mouse, keyboard or controller.

## 9. Sending rate and performance

`VisionSceneBridge` sends a full scene snapshot **only when something changed** (at most one per Unity frame, ≈90/s) and twice a second when nothing moves. Inspector fields: **Publish Rate** (maximum while moving, 0 = every frame) and **Idle Rate**. The debug window shows `Render` fps, `Unity X / Y` fps and messages per second, decode time and worst interval; use them when objects look laggy.

## 10. Version

Set it only in Unity **Player Settings** (*Version*, and *Build* for visionOS). The build copies it to the host and the debug window shows it.
