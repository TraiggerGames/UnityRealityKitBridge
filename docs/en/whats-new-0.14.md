# What's new in 0.14

English · [Español](../es/whats-new-0.14.md)

Version **0.14.0** (2026-10-02). The short list is in the [changelog](../../CHANGELOG.md); the full API is in the [API reference](api.md). This page explains what changed, why, and what you must do to update.

## 1. How to update (do this first)

1. `git pull` (or GitHub Desktop → *Pull origin*).
2. Open `UnityProject/` in Unity. It will import the *Visual Studio Editor* package; wait until it finishes and the Console has no red errors.
3. Check **Edit → Project Settings → Player → Version** shows `0.14.0`.
4. **Rebuild the Xcode project from Unity** with **MVP → Build immersive visionOS app**, then build **NativeHost** in Xcode. The host's Swift code changed in this release, and the Xcode folder is generated output: pulling the repository does **not** update an Xcode project you already generated.
5. Open the debug window; its title should read `Unity → RealityKit v0.14.0 (…) · DEBUG`.

## 2. Smooth motion: dynamic sending

**The problem.** The headset draws about 90 frames per second, but Unity sent the scene only 10 times per second and the host did not smooth the gaps. Moving objects advanced in visible steps.

**The fix.** `VisionSceneBridge` now decides by itself when to send:

| Situation | What is sent |
| --- | --- |
| Something moves, changes color, appears, disappears, or an anchor/audio command is issued | A snapshot on **that Unity frame** (up to ~90 per second) |
| Everything is still | A small heartbeat, 2 per second |

You can tune it on the `VisionSceneBridge` component: **Publish Rate** (maximum snapshots per second while something moves; `0` = every frame) and **Idle Rate** (heartbeat). If a scene with very many objects is heavy on the CPU, set Publish Rate to 60.

**How to check it.** In the debug window: `Render` and `Unity` fps should be high, and `msg/s` should rise toward the fps while something moves. With a still scene `msg/s` ≈ 2 and a worst interval ≈ 500 ms is **normal**.

## 3. A simple controls API

Three layers, from simplest to most flexible:

1. **Templates** — copy a file from `Assets/Scripts/Templates/`, rename the class, add it to an object. Seven ready-made behaviors (tap, pinch, grab, touch, follow the hand, anchor) plus the museum card. See the table in the [API reference](api.md#7-templates-copy-rename-edit).
2. **`VisionInteractable`** — events per object (`Tapped`, `GrabBegan`, `TouchBegan`…) in C# or in the Inspector.
3. **`VisionControls`** — read the hands from any `Update()`: `if (VisionControls.GetPinchDown(VisionHand.Right)) …`.

Underneath, the raw `VisionInput` events still exist and have not changed.

**Honest note:** the pinch in `VisionControls` is a distance threshold computed in Unity (2.5 cm to start, 4 cm to end). It is not Apple's system gesture, so the two can differ slightly. The system tap (look + pinch) is `VisionObject.SelectionChanged` / `VisionInteractable.Tapped`.

## 4. Info cards (museum labels)

Set a `VisionObject`'s **Shape** to **Panel**. The headset draws a glass card with a title and a text:

- Width and height = the object's scale **X** and **Y**, in meters (e.g. `0.5 × 0.3`).
- `SetPanelText(title, text)` changes the content at any time.
- The object's color tints the card's accent bar.
- `Panel Face User` (default on) makes the card turn toward the viewer.
- It is a normal object: give it `VisionInteractable` (Grabbable) so the user can **move it**, or anchor it, or hide it with `SetActive(false)`.

Use `TemplateMuseumInfo` for the full flow: tap a painting → the card appears beside it → move it with your hand → tap it to close. Setup steps are at the top of that file.

Limits: text and a color bar only; no images or buttons; text does not auto-fit the card, so enlarge the scale for long texts.

## 5. Changes that may affect your scenes

| Change | What you may notice | What to do |
| --- | --- | --- |
| `displayOffset` 0.6 m → `0` | Objects appear 0.6 m to the **left** of where they used to | Nothing, Unity coordinates are now exact. Shift your objects +0.6 m on X if you want the old look. |
| Dynamic sending | A still scene sends only ~2 msg/s | Normal. See section 2. |
| Magenta cube removed | The native marker is gone | Nothing. |
| `AttachToAnchor` vs `PlaceAnchor` | `AttachToAnchor` still creates the anchor only if missing | Use `PlaceAnchor` when you want to **replace** an anchor. |
| Grabbing an anchored object | `VisionInteractable` now releases it from its anchor | Re-anchor on release with `PlaceAnchor` (see `TemplateGrabThenAnchor`). |

## 6. Debugging aids

- **Debug window:** `Render N fps · Unity X / Y fps máx` and the message statistics. `Y` is the most Unity is allowed to run (target frame rate, or the display refresh divided by the vSync interval).
- **VS Code / Visual Studio:** in Unity, **Settings → External Tools → External Script Editor** = Visual Studio Code, then open a script from Unity (**Assets → Open C# Project**). The package is declared in `Packages/manifest.json`.
- **Gaze:** apps cannot read where the user looks. The system highlights the object that will receive the pinch; trust that highlight.

## 7. What is still unverified

The C# compiles and the Swift type-checks against the visionOS SDK. Hand pinch, touch, grab, anchors, surfaces, audio and info cards have **not** been verified on a real Apple Vision Pro in this release. Please report what you observe (device, visionOS version, permissions granted).
