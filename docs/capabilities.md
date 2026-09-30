# Capacidades de la librería Unity → RealityKit

| Capacidad | API desde Unity | Estado |
| --- | --- | --- |
| Modelos estáticos con textura | `VisionObject(Model)` + `MeshFilter`/`MeshRenderer` | Observado por el usuario en Vision Pro; exportador de una textura de color base. |
| Transformaciones y lógica | Cualquier `MonoBehaviour` que cambie `Transform` | Flujo Unity→RealityKit observado; actualización a 10 Hz. |
| Tap/select | `VisionObject.SelectionChanged` | Tap del cubo confirmado por el usuario; los modelos necesitan prueba específica. |
| Arrastrar | `VisionInput.ObjectDragged` | Compilado para visionOS; posición 3D del gesto, sin movimiento automático. Prueba en dispositivo pendiente. |
| Audio espacial | `VisionAudioSource.Play()` | Compilado para visionOS con WAV mono de ejemplo; reproducción en Vision Pro pendiente. |
| Manos | `VisionInput.HandUpdated` | Punta de índice y pulgar por mano; requiere permiso y prueba en Vision Pro. |
| Superficies | `VisionInput.SurfaceUpdated` | Planos horizontales/verticales con centro, normal y extensión; requiere permiso y prueba en Vision Pro. |

## Modelo de programación

Unity mantiene la lógica y los estados de la experiencia. RealityKit representa objetos y reproduce audio. ARKit proporciona datos sensibles de manos y habitación solamente dentro de un espacio inmersivo y tras la autorización del usuario. Los scripts C# se suscriben a `VisionInput` y usan `VisionObject`/`VisionAudioSource`; el host Swift se cambia solo al añadir **una nueva capacidad a la librería**, no al crear cada escena.

Esta versión ofrece **datos de manos y superficies**, no un sistema completo de gestos personalizados ni anclaje automático. Un script Unity puede calcular pinza, distancia o colocación a partir de las muestras recibidas. La siguiente ampliación debería incluir filtros de gestos, anclaje persistente, oclusión con la malla de la habitación, audio en bucle/control de ganancia y una política de rendimiento para modelos grandes. Todo ello requiere implementación y validación por separado.

### Fuentes Apple

- [ARKit en visionOS](https://developer.apple.com/documentation/arkit/arkit-in-visionos)
- [Acceso a datos ARKit y permisos](https://developer.apple.com/documentation/visionos/setting-up-access-to-arkit-data/)
- [Gestos dirigidos a entidades](https://developer.apple.com/documentation/visionos/adding-3d-content-to-your-app)
- [Audio espacial RealityKit](https://developer.apple.com/documentation/realitykit/spatialaudiocomponent)
