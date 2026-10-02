# Contrato Unity ↔ host visionOS

La fuente C# está en `UnityProject/Assets/Scripts/`; la fuente nativa en `UnityProject/Assets/Editor/VisionHost/Sources/`. El plugin `MVPBridge.mm` mantiene el nombre histórico `MVP_SendCubeState`, aunque ahora transporta una escena completa.

## Unity → RealityKit

`VisionSceneBridge` publica una instantánea completa cuando algo cambia (hasta una por frame; `publishRate` limita el máximo) y a `idleRate` Hz (2 por defecto) si todo está quieto; un ID ausente se elimina. Cada ID debe ser único y estable.

```json
{
  "version": 2,
  "objects": [{
    "id": "user-model", "shape": "model", "asset": "user-model",
    "position": {"x": 1.15, "y": 1.55, "z": -2.15},
    "rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
    "scale": {"x": 0.45, "y": 0.45, "z": 0.45},
    "color": {"r": 1, "g": 1, "b": 1},
    "audio": "chime", "audioSequence": 1
  }]
}
```

Campos opcionales de anclaje y mapeo, en el mismo marco:

```json
{
  "version": 2,
  "objects": [{ "id": "user-model", "anchor": "sculpture-anchor", "...": "..." }],
  "anchors": [{"id": "sculpture-anchor", "op": "ensure", "sequence": 3,
               "position": {"x": 0.2, "y": 0.9, "z": -1.5},
               "rotation": {"x": 0, "y": 0, "z": 0, "w": 1}}],
  "map": "off",
  "unityFps": 58.7,
  "unityMaxFps": 90
}
```

`unityFps` y `unityMaxFps` son diagnósticos opcionales para la ventana debug: los fotogramas por segundo que Unity ejecutó desde el mensaje anterior y el máximo permitido (`Application.targetFrameRate`, o la frecuencia de la pantalla dividida por el intervalo de vSync).

- `anchors` son órdenes de un solo uso: el host ejecuta cada `sequence` mayor que la última vista, en orden, y las repite hasta que Unity las retira (máximo 64). `ensure` crea el ancla solo si no existe (clave para contenido que se conserva entre sesiones), `place` la reemplaza y `remove` la borra. No se consume ninguna hasta que la sesión ARKit de anclas esté activa; el siguiente marco las reintenta.
- Un objeto con `anchor` interpreta `position`, `rotation` y `scale` como locales al ancla y permanece oculto hasta que ARKit localiza el ancla. Sin `anchor` se mantiene el comportamiento anterior.
- `map` es `off`, `mesh` (malla de la habitación en alambre) u `occlusion` (las superficies reales ocultan los objetos virtuales). Un cambio hecho desde Unity sustituye la elección de la ventana debug.

`shape` admite `box`, `sphere`, `model` y `panel`. Un `panel` es una tarjeta de texto: añade `title`, `text` y `faceUser` al objeto, su `scale.x`/`scale.y` es el ancho/alto en metros y `color` tiñe la barra de acento; el host la dibuja con una vista SwiftUI adjunta a la entidad. `asset` nombra un USDA en `VisionModels/`. `audio` nombra un WAV/AIFF en `VisionAudio/`; cuando `audioSequence` aumenta, RealityKit lo reproduce una vez desde la posición de la entidad. El color modifica primitivas; los modelos conservan su textura. Posición y escala están en metros. El bridge refleja Z y convierte el cuaternión de Unity al sistema de RealityKit.

## RealityKit y ARKit → Unity

El host usa `UnityFramework.sendMessageToGO` hacia el GameObject `VisionSceneBridge`. `OnNativeSelect(string id)` procesa el tap. `OnNativeEvent(string json)` entrega los eventos siguientes a `VisionInput`:

| `type` | Campos relevantes | Evento C# |
| --- | --- | --- |
| `drag` | `id`, `phase` (`changed`/`ended`), `position` | `ObjectDragged` |
| `hand` | `id` (`left`/`right`), `tracked`, `position` (índice), `secondary` (pulgar) | `HandUpdated` |
| `anchor` | `id` (nombre), `phase` (`added`/`updated`/`removed`), `tracked`, `position`, `rotation` | `VisionAnchors.AnchorUpdated` |
| `surface` | `id` UUID, `phase` (`added`/`updated`/`removed`), `alignment`, `classification`, `position`, `normal`, `width`, `height` | `SurfaceUpdated` |

Los nombres de ancla y su UUID de ARKit se guardan en `UserDefaults` (`mvp.anchors`); ARKit conserva las anclas entre lanzamientos. Si ARKit pierde una, llega `removed` y se olvida su nombre, de modo que `ensure` la vuelve a crear. Los eventos `anchor` anteriores al primer marco de Unity se reenvían al recibirlo. Las posiciones de `hand`, `surface`, `drag` y `anchor` ya descuentan el desplazamiento de visualización del host, de modo que coinciden con las coordenadas Unity de los objetos (las de `hand` y `surface` no lo hacían hasta ahora). Llegan a los scripts C# con Z reflejada a coordenadas Unity. Las manos se limitan a unas 30 muestras/s por mano. `surface` representa el rectángulo estimado del plano, no la malla completa de la habitación. Un `removed` invalida su UUID.

## Compatibilidad y evolución

El host aún entiende el cubo v1 anterior. Los campos de audio son opcionales en v2. Cambiar un campo existente o añadir otro tipo de evento requiere actualizar C#, Swift y este documento en la misma revisión. El protocolo no tiene confirmaciones, diffs, envío de la malla de la habitación a Unity ni control de flujo; medir antes de escalar a escenas grandes.
