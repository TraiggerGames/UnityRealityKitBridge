# Changelog / Registro de cambios

English first, español después. Detailed upgrade guides: [What's new in 0.14](docs/en/whats-new-0.14.md) · [Novedades de la 0.14](docs/es/whats-new-0.14.md).

---

## English

### 0.14.0 — 2026-10-02

This release makes the project easy to *program*: a simple controls API, copy-and-edit templates, info cards, smooth motion, better debugging, and bilingual documentation.

**Added**
- **Controls API — `VisionControls`.** Read the hands like Unity's `Input`: `GetPinch`, `GetPinchDown`, `GetPinchUp`, `Hand(...).PinchPoint`, and the events `PinchStarted` / `PinchEnded`. No setup. *Why:* before, you had to subscribe to raw hand events and do the math yourself.
- **`VisionInteractable`.** Put it next to a `VisionObject` to get `Tapped`, `GrabBegan` / `GrabMoved` / `GrabEnded` and `TouchBegan` / `TouchEnded`, also as Inspector `UnityEvent`s (no code). Optional "follows the hand while grabbed" and "leaves its anchor when grabbed".
- **Info cards (`VisionObject` → Shape = `Panel`).** Floating glass cards with a title and text, drawn natively by the headset. They can be moved, anchored, shown and hidden like any object. *Why:* museum-style use cases ("touch a painting, read about it").
- **Templates** in `Assets/Scripts/Templates/` (comments in English and Spanish): `TemplateTapChangeColor`, `TemplatePinchChangeColor`, `TemplateGrabHighlight`, `TemplateTouchButton`, `TemplateFollowHand`, `TemplateGrabThenAnchor`, `TemplateAnchorWithPinch`, `TemplateMuseumInfo`.
- **`VisionObject` additions:** `BaseColor` / `SelectedColor` (change colors from code), `PlaceAnchor` (create *or replace* an anchor), `DetachFromAnchor`, and the panel members (`PanelTitle`, `PanelText`, `SetPanelText`, `PanelFaceUser`).
- **`VisionGrabAndPlace` example.** Grab an object and, when released over a detected table or floor, it settles on top.
- **Debug window diagnostics.** Native render fps (RealityKit), Unity fps and the maximum Unity may run at, next to the existing message statistics.
- **Bilingual documentation:** [API reference](docs/en/api.md), this changelog, and the 0.14 upgrade guide.

**Changed**
- **Dynamic sending (fixes choppy motion).** Unity used to send the scene at a fixed 10 Hz while the headset draws at ~90 Hz, so objects moved in visible steps. Now it sends a snapshot **only when something changed** (at most once per Unity frame) and a 2 Hz heartbeat when everything is still. New Inspector fields on `VisionSceneBridge`: `Publish Rate` (0 = every frame) and `Idle Rate`.
- **`displayOffset` is now `0`.** It was 0.6 m on X, only to keep the demo away from the old debug cube. Unity coordinates are now exactly the headset's coordinates. Hands, surfaces and anchors were already consistent with each other.
- `VisionTouchable` exposes `SelectOnTouch`.
- The Unity project includes the *Visual Studio Editor* package (`com.unity.ide.visualstudio`), so VS Code / Visual Studio recognise Unity types such as `MonoBehaviour`. Generated IDE files (`.sln`, `.csproj`, `.vscode/`) are now git-ignored.
- App version is now **0.14.0** (Unity Player Settings).

**Removed**
- The magenta native debug cube.

**Fixed**
- `AnchorService.swift` failed to compile: `Transform` requires `import RealityKit`.

**Known limits**
- The C# compiles and the Swift type-checks against the visionOS SDK, but gestures, sensors, anchors, audio and info cards still need testing on a real Apple Vision Pro.
- The host does not interpolate between snapshots; at the maximum rate this should look smooth, but irregular arrival can still show as slight jitter.
- Info cards: text and a color bar only; no images or buttons.

### 0.13.0
- The app version comes from one place: Unity Player Settings.

### Earlier (since the initial release)
- Fingertip touch example, gaze hover effect, consistent hand/surface coordinates.
- Persistent world anchors and optional room mesh (`VisionAnchors`, `VisionMap`).
- Automatic host start-up with debug/production modes and diagnostics.
- Initial release: Unity logic with a native SwiftUI + RealityKit + ARKit host.

---

## Español

### 0.14.0 — 2026-10-02

Esta versión hace el proyecto fácil de *programar*: una API sencilla de controles, plantillas para copiar y editar, tarjetas de información, movimiento fluido, mejor depuración y documentación bilingüe.

**Añadido**
- **API de controles — `VisionControls`.** Lee las manos como el `Input` de Unity: `GetPinch`, `GetPinchDown`, `GetPinchUp`, `Hand(...).PinchPoint` y los eventos `PinchStarted` / `PinchEnded`. Sin configuración. *Por qué:* antes había que suscribirse a los eventos en bruto de las manos y hacer los cálculos a mano.
- **`VisionInteractable`.** Colócalo junto a un `VisionObject` y obtienes `Tapped`, `GrabBegan` / `GrabMoved` / `GrabEnded` y `TouchBegan` / `TouchEnded`, también como `UnityEvent` del Inspector (sin código). Opcional: "sigue la mano mientras se agarra" y "se suelta de su ancla al agarrarlo".
- **Tarjetas de información (`VisionObject` → Shape = `Panel`).** Tarjetas de cristal flotantes con título y texto, dibujadas de forma nativa por el visor. Se pueden mover, anclar, mostrar y ocultar como cualquier objeto. *Por qué:* casos de uso tipo museo ("toco un cuadro y leo sobre él").
- **Plantillas** en `Assets/Scripts/Templates/` (comentarios en inglés y español): `TemplateTapChangeColor`, `TemplatePinchChangeColor`, `TemplateGrabHighlight`, `TemplateTouchButton`, `TemplateFollowHand`, `TemplateGrabThenAnchor`, `TemplateAnchorWithPinch`, `TemplateMuseumInfo`.
- **Novedades de `VisionObject`:** `BaseColor` / `SelectedColor` (cambiar colores desde código), `PlaceAnchor` (crea *o reemplaza* un ancla), `DetachFromAnchor` y los miembros de panel (`PanelTitle`, `PanelText`, `SetPanelText`, `PanelFaceUser`).
- **Ejemplo `VisionGrabAndPlace`.** Coge un objeto y, al soltarlo sobre una mesa o suelo detectados, se asienta encima.
- **Diagnósticos en la ventana debug.** Fps de render nativo (RealityKit), fps de Unity y el máximo al que puede ejecutarse, junto a las estadísticas de mensajes que ya existían.
- **Documentación bilingüe:** [referencia de la API](docs/es/api.md), este registro de cambios y la guía de actualización a la 0.14.

**Cambiado**
- **Envío dinámico (corrige el movimiento a tirones).** Unity enviaba la escena a 10 Hz fijos mientras el visor dibuja a ~90 Hz, así que los objetos se movían a saltos visibles. Ahora envía una instantánea **solo cuando algo cambia** (como máximo una por frame de Unity) y un latido de 2 Hz si todo está quieto. Campos nuevos en el Inspector de `VisionSceneBridge`: `Publish Rate` (0 = cada frame) e `Idle Rate`.
- **`displayOffset` ahora es `0`.** Era 0,6 m en X, solo para separar la demo del antiguo cubo de depuración. Las coordenadas de Unity son ahora exactamente las del visor. Manos, superficies y anclas ya eran coherentes entre sí.
- `VisionTouchable` expone `SelectOnTouch`.
- El proyecto Unity incluye el paquete *Visual Studio Editor* (`com.unity.ide.visualstudio`), de modo que VS Code / Visual Studio reconocen tipos de Unity como `MonoBehaviour`. Los archivos generados del IDE (`.sln`, `.csproj`, `.vscode/`) ya se ignoran en git.
- La versión de la app es ahora **0.14.0** (Unity Player Settings).

**Eliminado**
- El cubo magenta nativo de depuración.

**Corregido**
- `AnchorService.swift` no compilaba: `Transform` requiere `import RealityKit`.

**Límites conocidos**
- El C# compila y el Swift pasa la comprobación de tipos con el SDK de visionOS, pero gestos, sensores, anclas, audio y tarjetas aún necesitan pruebas en un Apple Vision Pro real.
- El host no interpola entre instantáneas; a la frecuencia máxima debería verse fluido, pero una llegada irregular aún puede notarse como un leve temblor.
- Tarjetas: solo texto y una barra de color; sin imágenes ni botones.

### 0.13.0
- La versión de la app sale de un solo sitio: Unity Player Settings.

### Anteriores (desde la versión inicial)
- Ejemplo de toque con la yema, resalte al mirar, coordenadas coherentes de manos y superficies.
- Anclas persistentes en el mundo y malla opcional de la habitación (`VisionAnchors`, `VisionMap`).
- Arranque automático del host con modos debug/producción y diagnósticos.
- Versión inicial: lógica en Unity con host nativo SwiftUI + RealityKit + ARKit.
