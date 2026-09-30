# Programar experiencias desde Unity

## Escena y componentes

Abre `UnityProject/` en Unity 6000.5.1f1. La escena de ejemplo `Assets/Scenes/InteractiveSculptureSensors.unity` ya está en Build Settings. Cualquier objeto que deba aparecer en el espacio nativo necesita un `VisionObject` con **ID único**. Elige `Box`, `Sphere` o `Model`. Para `Model`, añade `MeshFilter` y `MeshRenderer` y define una clave de modelo sin espacios; el exportador admite una malla estática con UV y una textura de color base. Activa **Read/Write** en el importador del modelo.

La lógica es un `MonoBehaviour` ordinario. Cambia `transform.position`, `rotation` o `localScale` en `Update()`; `VisionSceneBridge` los envía al host. Activa o desactiva `VisionObject` para crear o retirar su representación nativa. El bridge busca todos los objetos activos y publica una instantánea 10 veces por segundo.

## Entrada desde visionOS

- **Tap:** suscríbete a `VisionObject.SelectionChanged` y cancela la suscripción en `OnDisable`. `UserModelBehaviour.cs` gira el modelo, lo agranda y reproduce un sonido al seleccionarlo.
- **Arrastre 3D:** `VisionInput.ObjectDragged` envía ID, fase y posición. `Assets/Scripts/Examples/VisionDragMover.cs` mantiene la posición relativa entre el punto agarrado y el objeto.
- **Manos:** `VisionInput.HandUpdated` envía punta de índice y pulgar por mano. `Assets/Scripts/Examples/VisionHandPinch.cs` calcula una pinza simple en C#; ajústala con filtros para producción.
- **Superficies:** `VisionInput.SurfaceUpdated` envía centro, normal, tamaño y ciclo de vida de planos horizontales o verticales. `Assets/Scripts/Examples/VisionSurfacePlacer.cs` coloca un objeto sobre la primera superficie horizontal suficientemente grande.
- **Anclas persistentes:** `VisionAnchors.Ensure(id, posición, rotación)` crea un ancla en el mundo real que sobrevive entre lanzamientos (`Place` la reemplaza, `Remove` la borra). `VisionObject.AttachToAnchor(id, posición, rotación)` ancla el objeto: desde ese momento su `transform` es local al ancla y el objeto está oculto hasta que ARKit la localiza. `VisionAnchors.AnchorUpdated` informa de altas, movimientos y pérdidas. `Assets/Scripts/Examples/VisionAnchorOnSurface.cs` ancla un objeto a la primera superficie horizontal y lo conserva en los siguientes arranques. Para borrar todas las anclas guardadas usa **Borrar anclas** en la ventana debug.
- **Malla de la habitación:** `VisionMap.Mode = VisionMapMode.Mesh` dibuja la malla en alambre y `Occlusion` hace que las superficies reales oculten los objetos virtuales. Empieza en `Off` porque consume CPU y batería; en debug también se cambia desde la ventana.

Los ejemplos de arrastre, pinza y colocación son **opcionales**: añade el componente al GameObject que elijas. La escena de muestra ya tiene `VisionAudioSource` en `user-model`, con `Assets/Audio/chime.wav`. Para otro sonido, asigna un WAV/AIFF mono importado y llama a `GetComponent<VisionAudioSource>().Play()` desde C#.

## Un build desde Unity

1. Guarda la escena y confirma que es la única habilitada en Build Settings. **MVP → Use sensor demo scene** selecciona la escena de muestra sin recrearla.
2. Configura en Unity Player Settings el identificador de paquete y el equipo de firma Apple. Instala Ruby y la gema `xcodeproj` una vez; comprueba `ruby -e 'require "xcodeproj"'`.
3. Pulsa **MVP → Build immersive visionOS app** y elige una carpeta. El menú crea una subcarpeta con fecha y hora. El callback previo exporta mallas y audio; el posterior integra `NativeHost` en el proyecto Xcode nuevo.
4. Abre el `.xcodeproj`, selecciona **NativeHost**, firma e instala en Vision Pro. Abre el espacio y luego inicia la lógica Unity. visionOS puede pedir acceso a manos y entorno por separado. La ventana muestra el estado de sensores y el número de superficies detectadas.

No edites archivos del Xcode generado para cambiar el comportamiento de una escena. Los Swift y el integrador que usarán los futuros builds están en `Assets/Editor/VisionHost/` dentro del proyecto Unity.

## Qué todavía requiere ampliar la librería

No se transfieren automáticamente animaciones esqueléticas, partículas, cámaras, luces, física de Unity, shaders arbitrarios, la malla de la habitación como datos en Unity (solo se dibuja o ocluye en el host). Un nuevo tipo de recurso o evento requiere una implementación genérica en C# y Swift. Después, las escenas pueden usarla desde Unity sin otra edición nativa.
