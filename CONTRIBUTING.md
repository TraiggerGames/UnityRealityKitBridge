# Contribuir

Gracias por ayudar a convertir este prototipo en una librería reutilizable. Antes de proponer una función, consulta [capacidades](docs/capabilities.md), [arquitectura](docs/architecture.md) y [protocolo](docs/protocol.md).

## Fuentes de verdad

- Comportamiento y API: `UnityProject/Assets/Scripts/`.
- Exportación y post build: `UnityProject/Assets/Editor/`.
- Host Swift/RealityKit/ARKit: `UnityProject/Assets/Editor/VisionHost/Sources/`.
- Script que integra Xcode: `UnityProject/Assets/Editor/VisionHost/integrate_host.rb`.
- `Xcode/Generated/`, `UnityProject/Library/` y `Assets/VisionExport/` son resultados locales. No los añadas a Git.

Conserva los archivos `.meta` de Unity y los IDs de objetos de ejemplo. Si cambias el contrato JSON, actualiza `docs/protocol.md` y ambos lados del bridge en el mismo cambio. Si una capacidad usa datos sensibles, añade su descripción de permiso y una vía que siga funcionando cuando se deniegue.

## Comprobación

1. Prepara el modelo con `python3 scripts/prepare_sample_model.py` y abre `UnityProject/`.
2. Comprueba que Unity no muestra errores C# y genera un build desde **MVP → Build immersive visionOS app** en una carpeta nueva.
3. Compila el esquema **NativeHost** para visionOS físico. Una compilación satisfactoria no valida gestos, sensores ni audio en hardware.
4. Si tienes Vision Pro, describe en tu contribución el dispositivo, versión de visionOS, permisos concedidos y pasos observados. Si no lo tienes, indica claramente que la prueba en hardware sigue pendiente.

Evita incluir logs personales, identificadores de firma, perfiles de aprovisionamiento o binarios generados. Los assets de ejemplo nuevos necesitan origen y permisos de redistribución claros. Para cambios grandes, abre primero una propuesta con el contrato C# previsto y su coste de serialización.
