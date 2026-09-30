# Unity → RealityKit para visionOS

Puente experimental para crear experiencias inmersivas con **lógica en Unity/C#** y presentación nativa en **SwiftUI + RealityKit + ARKit**, sin PolySpatial. El proyecto Unity exporta un build visionOS Windowed y un paso automático integra un host con `ImmersiveSpace` en el Xcode generado.

> **Estado:** prototipo de investigación. El usuario ha visto los modelos de la versión anterior en Vision Pro. La versión actual con arrastre, sonido espacial, manos y superficies compila para dispositivo, pero esas cuatro capacidades aún necesitan pruebas en Vision Pro. No equivale a PolySpatial ni traduce cualquier escena Unity automáticamente.

## Inicio rápido

**Requisitos:** macOS con Unity **6000.5.1f1** y visionOS Build Support, Xcode con SDK visionOS, Ruby con la gema `xcodeproj`, Python 3 con los paquetes de `scripts/requirements.txt` y Apple Vision Pro para la prueba final. Unity Personal genera el Player Windowed; la app inmersiva la aporta el host nativo propio.

1. Tras clonar, prepara el modelo de muestra **antes de abrir Unity**:

   ```sh
   python3 -m pip install -r scripts/requirements.txt
   python3 scripts/prepare_sample_model.py
   ruby -e 'require "xcodeproj"'
   ```

   El último comando comprueba la dependencia Ruby. Si falla, instala la gema `xcodeproj` para tu instalación de Ruby.

2. Abre `UnityProject/` con Unity Hub. La escena `InteractiveSculptureSensors.unity` está marcada en Build Settings. Los ejemplos de [programación desde C#](docs/unity-workflow.md) están en `Assets/Scripts/Examples/`.
3. Configura una vez el Bundle Identifier y el equipo de firma de Apple en Unity Player Settings.
4. Pulsa **MVP → Build immersive visionOS app**. Elige una carpeta de destino. Unity exporta el Player y añade automáticamente `NativeHost`, los modelos, las texturas, el audio y los permisos de sensores.
5. Abre el `.xcodeproj` resultante, selecciona el esquema **NativeHost**, firma y ejecuta en Vision Pro. El host abre solo el espacio inmersivo e inicia la lógica Unity; acepta los permisos de sensores que quieras usar.

**Modo debug / producción.** El menú **MVP → Debug host UI** (activo por defecto; un Development Build también lo activa) decide qué host se genera. En debug la ventana muestra estado, botones manuales, interruptor de inicio automático y diagnósticos (mensajes/s, tamaño, tiempo de decodificación, peor intervalo entre frames) y se ve el cubo magenta nativo. Con el menú desmarcado y sin Development Build, el host muestra solo «Cargando experiencia…», oculta la ventana al arrancar y no dibuja el marcador. Se escribe como `MVPDebugUI` en el `Info.plist` del host.

La copia local actual del export está en `Xcode/Generated/`; **no se sube a Git**. Después de clonar, genérala con el paso 4.

## Qué se programa en Unity

| Necesidad | API C# |
| --- | --- |
| Mover, girar o escalar un objeto | Cualquier `MonoBehaviour` que modifique su `Transform` |
| Tap sobre una entidad | `VisionObject.SelectionChanged` |
| Arrastre 3D | `VisionInput.ObjectDragged` |
| Mano y pinza personalizada | `VisionInput.HandUpdated` |
| Detectar mesas, suelo o paredes | `VisionInput.SurfaceUpdated` |
| Reproducir sonido desde un objeto | `VisionAudioSource.Play()` |
| Anclar contenido al mundo real (persistente) | `VisionAnchors.Ensure()`, `VisionObject.AttachToAnchor()` |
| Ver u ocluir con la malla de la habitación | `VisionMap.Mode` |

Los datos de manos y superficies requieren permisos independientes. Apple los ofrece mediante ARKit dentro de un espacio inmersivo; el sonido se emite desde la posición de la entidad RealityKit. Véanse [ARKit en visionOS](https://developer.apple.com/documentation/arkit/arkit-in-visionos), [permisos](https://developer.apple.com/documentation/visionos/setting-up-access-to-arkit-data/) y [audio espacial](https://developer.apple.com/documentation/realitykit/spatialaudiocomponent).

**Versión.** Se cambia en un solo sitio: Unity Player Settings (*Version* y, para visionOS, *Build*). El build la copia al `Info.plist` del host y la ventana debug la muestra.

## Estructura

```text
UnityProject/                 Proyecto Unity versionado (sin Library/)
  Assets/Scripts/             Lógica y API C#
  Assets/Editor/              Exportadores y paso de build automático
  Assets/Editor/VisionHost/   Fuentes Swift y script Ruby canónicos
Xcode/Generated/             Export local listo para abrir; ignorado por Git
sample-assets/result.glb      Modelo original de ejemplo
scripts/prepare_sample_model.py
scripts/requirements.txt
docs/                        Arquitectura, protocolo, límites y contribución
```

`result.obj` se genera localmente a partir del GLB original. Sus 113 MB superan el límite de tamaño de archivo de GitHub; `result.obj.meta` sí está versionado para conservar las referencias de la escena Unity. Los archivos USDA y el proyecto Xcode se regeneran durante el build.

## Documentación

- [Flujo de trabajo en Unity](docs/unity-workflow.md)
- [Arquitectura](docs/architecture.md)
- [Contrato del bridge](docs/protocol.md)
- [Capacidades y estado](docs/capabilities.md)
- [Limitaciones](docs/limitations.md)
- [Contribuir](CONTRIBUTING.md)

## Licencias y marcas

El código propio y los recursos de muestra necesitan una licencia explícita antes de publicarse. Unity, visionOS, RealityKit y ARKit pertenecen a sus respectivos titulares y requieren sus herramientas y licencias. Este repositorio no incluye código ni binarios de PolySpatial.

---

**English:** Experimental Unity C# logic bridge for a native immersive visionOS host. Build from `UnityProject/`; generated Xcode output stays local. See the Spanish guides above for setup, protocol and current limitations.
