# Referencia de la API de Unity

[English](../en/api.md) · Español

Todo es C# normal en `UnityProject/Assets/Scripts/`. Nunca editas Swift para crear una experiencia: Unity decide, el visor dibuja. Todas las posiciones están en **coordenadas de mundo de Unity, en metros**.

> Estado: el código compila en el proyecto, pero gestos, sensores, anclas y audio aún necesitan comprobarse en un Apple Vision Pro real. Véanse las [limitaciones](../limitations.md).

## 1. El camino de 5 minutos

1. Pon un `VisionObject` en cada GameObject que deba verse en el visor. Dale un **ID único**.
2. Elige qué quieres hacer y copia una **plantilla** de `Assets/Scripts/Templates/` (sección 7). Cambia el nombre de la clase, añádela al objeto y edita las pocas líneas marcadas en los comentarios.
3. Compila con **MVP → Build immersive visionOS app** (véase el [flujo de trabajo](../unity-workflow.md)).

## 2. `VisionObject` – un objeto que existe en el visor

Se añade junto a la malla. Es el único componente que necesita un objeto para verse.

| Miembro | Qué hace |
| --- | --- |
| `Id` | ID único y estable. Los duplicados se ignoran con un aviso. |
| `SelectionChanged(bool)` | El tap del sistema: el usuario **mira el objeto y pellizca**. Se dispara cada vez. |
| `IsSelected`, `Select()` | Estado de selección. Un tap lo alterna. |
| `BaseColor`, `SelectedColor` | Colores que puedes cambiar desde código. `DisplayColor` es el que se ve ahora. Vale para Box/Sphere; los modelos conservan su textura. |
| `AnchorId`, `SetAnchor(id)` | Nombre del ancla a la que está unido el objeto (vacío = libre). |
| `AttachToAnchor(id, pos, rot)` | Crea el ancla **solo si aún no existe** y une el objeto. Su transform pasa a ser local al ancla. |
| `PlaceAnchor(id, pos, rot)` | Igual, pero **reemplaza** un ancla existente. |
| `DetachFromAnchor()` | Libera el objeto conservando su pose actual en el mundo. El ancla se mantiene. |
| `Shape = Panel`, `PanelTitle`, `PanelText`, `SetPanelText(título, texto)`, `PanelFaceUser` | Una **tarjeta de texto** flotante (véase la sección 7b). Su escala X/Y es su ancho/alto en metros. El color del objeto tiñe la barra de acento. |
| `PlayAudio(key)` | Reproduce un sonido espacial desde el objeto (véase `VisionAudioSource`). |

Un objeto anclado **permanece oculto hasta que ARKit localiza su ancla**, y mientras está anclado su `transform` es *relativo al ancla*.

## 3. `VisionControls` – lee las manos como `Input`

Estática y sin configuración. Llámala desde cualquier `Update()`. Requiere el permiso de seguimiento de manos.

```csharp
if (VisionControls.GetPinchDown(VisionHand.Right)) Disparar();
transform.position = VisionControls.Hand(VisionHand.Right).PinchPoint;
```

| Miembro | Significado |
| --- | --- |
| `Hand(hand)` → `VisionHandState` | `Tracked`, `IndexTip`, `ThumbTip`, `PinchPoint` (punto medio), `PinchDistance`, `IsPinching`. |
| `IsTracked(hand)` | La mano se ve ahora. |
| `GetPinch(hand)` | El pellizco se mantiene. |
| `GetPinchDown(hand)` / `GetPinchUp(hand)` | Verdadero solo en el frame en que el pellizco empieza / termina. |
| `PinchStarted`, `PinchEnded` | Lo mismo como eventos (`Action<VisionHand>`). |
| `PinchStartDistance` (2,5 cm), `PinchEndDistance` (4 cm) | Umbrales con histéresis; puedes cambiarlos. |

`VisionHand` es `Left` o `Right`. Este pellizco es un **umbral de distancia calculado por nosotros**, no el gesto del sistema de Apple, así que pueden diferir ligeramente.

## 4. `VisionInteractable` – reacciona al usuario por objeto

Se añade junto a un `VisionObject`. Cada evento existe también como `UnityEvent` en el Inspector (`onTapped`, `onGrabBegan`, `onGrabEnded`, `onTouchBegan`, `onTouchEnded`).

| Evento | Cuándo |
| --- | --- |
| `Tapped` | Mirar + pellizcar (el tap del sistema). |
| `GrabBegan` / `GrabMoved(Vector3)` / `GrabEnded` | Mirar, pellizcar y mover la mano. `GrabMoved` da la nueva posición de mundo del objeto. |
| `TouchBegan(hand)` / `TouchEnded(hand)` | Una yema entra/sale del objeto (sin pellizco). Marca **Touchable**. |

Opciones: **Grabbable** (el objeto sigue la mano mientras se agarra) y **Detach Anchor On Grab** (un objeto anclado se libera del ancla al agarrarlo, para poder moverlo). `IsGrabbed` indica el estado actual.

## 5. Anclas – que las cosas se queden en el mundo real

Un ancla es un punto con nombre en la habitación que ARKit recuerda **entre lanzamientos de la app**.

```csharp
VisionAnchors.Ensure("lampara", pos, rot);   // crea solo si falta (conserva la guardada)
VisionAnchors.Place("lampara", pos, rot);    // crea o reemplaza
VisionAnchors.Remove("lampara");
VisionAnchors.AnchorUpdated += a => Debug.Log(a.Id + " " + a.Phase + " tracked=" + a.Tracked);
VisionAnchors.TryGet("lampara", out var muestra);   // Position, Rotation, Tracked
```

Las fases son `added`, `updated`, `removed`. Los nombres se guardan en el dispositivo; **Borrar anclas** en la ventana debug los olvida todos. Requiere el permiso de percepción del entorno.

## 6. Eventos de bajo nivel y el mundo

`VisionInput` (eventos en bruto que alimentan las APIs anteriores):

| Evento | Datos |
| --- | --- |
| `HandUpdated` | `Hand`, `Tracked`, `IndexTip`, `ThumbTip` (≈30/s por mano) |
| `SurfaceUpdated` | `Id`, `Phase`, `Alignment` (`horizontal`/`vertical`), `Classification` (suelo, mesa, asiento, pared…), `Center`, `Normal`, `Width`, `Height` |
| `ObjectDragged` | `ObjectId`, `Phase` (`changed`/`ended`), `Position` |

Las superficies son **rectángulos estimados**, no la malla de la habitación. `VisionMap.Mode` = `Off` (por defecto), `Mesh` (alambre) u `Occlusion` (las superficies reales ocultan objetos virtuales); consume CPU y batería. `VisionAudioSource.Play()` reproduce desde su posición el clip asociado al objeto.

## 7. Plantillas (copiar, renombrar, editar)

En `Assets/Scripts/Templates/`. Los comentarios están en inglés y español.

| Plantilla | Qué consigues |
| --- | --- |
| `TemplateTapChangeColor` | Mirar + pellizcar → el objeto recorre una lista de colores. |
| `TemplatePinchChangeColor` | Pellizcar con la mano **cerca** del objeto → cambia de color mientras pellizcas. |
| `TemplateGrabHighlight` | Coger y mover un objeto; se resalta mientras lo sujetas. |
| `TemplateTouchButton` | Un "botón" que se pulsa con la yema (sin pellizco). |
| `TemplateFollowHand` | El objeto sigue tu punto de pellizco. |
| `TemplateGrabThenAnchor` | Mueve un objeto; donde lo sueltas queda **anclado para siempre**, incluso tras reiniciar. |
| `TemplateAnchorWithPinch` | Pellizca con la mano izquierda para dejar un ancla y mostrar un objeto ahí. |
| `TemplateMuseumInfo` | Tocar un cuadro → aparece a su lado una tarjeta de información que se puede mover. |

Hay ejemplos más completos en `Assets/Scripts/Examples/` (`VisionGrabAndPlace` apoya un objeto en una mesa o en el suelo al soltarlo, `VisionAnchorOnSurface`, `VisionSurfacePlacer`, `VisionDragMover`, `VisionHandPinch`, `VisionTouchable`).

### 7b. Tarjetas de información (carteles de museo)

Pon la **Shape** de un `VisionObject` en `Panel` y tendrás una tarjeta de cristal con título y texto, dibujada de forma nativa por el visor. Es un objeto normal: añade `VisionInteractable` (Grabbable) para que el usuario pueda **moverla**, ánclala, o muéstrala/ocúltala con `SetActive`. `TemplateMuseumInfo` resuelve el caso clásico: tocas un cuadro, aparece a su lado una tarjeta con su descripción, la mueves con la mano y la cierras tocándola.

```csharp
tarjeta.SetPanelText("Las Meninas", "Diego Velázquez, 1656.");
tarjeta.transform.position = cuadro.transform.position + new Vector3(0.6f, 0, 0);
tarjeta.gameObject.SetActive(true);
```

Límites: solo texto y una barra de color (sin imágenes ni botones); el tamaño lo da la escala y el texto no se ajusta solo; `PanelFaceUser` hace que gire hacia quien mira (así su rotación se ignora).

## 8. Lo que no se puede hacer (por diseño de visionOS)

- **No hay cursor ni mirilla de mirada.** Las apps no saben hacia dónde mira el usuario. El sistema ilumina (efecto hover) el objeto que recibirá el pellizco; fíate de eso.
- **Tap y arrastre siempre requieren mirar + pellizcar.** `VisionTouchable` y `VisionControls` funcionan con datos de las manos.
- No se usa el Input System de Unity; no hay ratón, teclado ni mandos.

## 9. Frecuencia de envío y rendimiento

`VisionSceneBridge` envía una instantánea completa **solo cuando algo cambia** (como máximo una por frame de Unity, ≈90/s) y dos veces por segundo si nada se mueve. Campos del Inspector: **Publish Rate** (máximo mientras algo se mueve, 0 = cada frame) e **Idle Rate**. La ventana debug muestra fps de `Render`, fps de `Unity X / Y`, mensajes por segundo, tiempo de decodificación y peor intervalo; úsalos cuando los objetos se vean con lag.

## 10. Versión

Se cambia solo en Unity **Player Settings** (*Version* y, para visionOS, *Build*). El build la copia al host y la ventana debug la muestra.
