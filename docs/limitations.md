# Limitaciones y validación

## Comprobado

- El usuario observó en Vision Pro los modelos de la escena anterior y el cubo controlado por Unity que cambiaba al tocarlo.
- El proyecto Unity actual compiló en una copia aislada con los ejemplos C# de arrastre, pinza y colocación.
- El export con `NativeHost`, dos modelos USD, textura, WAV y permisos de sensores compiló para `generic/platform=visionOS` con firma desactivada. `usdchecker` validó el USDA del modelo de muestra. Los archivos `global-metadata.dat`, `user-model.usda`, `user-model.jpeg` y `chime.wav` están en el paquete generado.

## Pendiente en Vision Pro

- Anclas (persistencia tras cerrar y reabrir la app, pérdida de seguimiento, rechazo del permiso) y malla de la habitación (coste y estabilidad): ni se ha compilado el Swift nuevo ni se ha probado en dispositivo.
- Arrastre, reproducción de audio espacial, posiciones de dedos y detección de superficies de esta versión.
- Aceptar y rechazar cada permiso de ARKit por separado, abrir/cerrar el espacio repetidas veces y probar pérdida de seguimiento.
- Medir latencia, consumo y estabilidad. Builds anteriores mostraron advertencias Metal al ejecutar Unity con su ventana oculta.

## Alcance técnico

La sincronización de objetos envía una instantánea JSON completa a 10 Hz. Manos se muestrean hasta unas 30 veces/s por mano. El exportador admite `MeshFilter` estático, UV y una textura de color base; no traduce materiales o efectos avanzados. Las superficies son rectángulos estimados, no mallas de la habitación. El ejemplo de pinza usa solo una distancia entre dedos. El modelo de muestra tiene más de un millón de triángulos y puede requerir simplificación.

El export local se compiló para dispositivo; no se validó una variante de simulador. Unity Personal continúa produciendo un Player Windowed que el host ejecuta como biblioteca. Este proyecto no ofrece compatibilidad general con cualquier escena ni paridad con PolySpatial.
