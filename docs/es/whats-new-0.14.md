# Novedades de la 0.14

[English](../en/whats-new-0.14.md) · Español

Versión **0.14.0** (2026-10-02). El resumen está en el [registro de cambios](../../CHANGELOG.md); la API completa, en la [referencia de la API](api.md). Esta página explica qué cambió, por qué, y qué debes hacer para actualizar.

## 1. Cómo actualizar (haz esto primero)

1. `git pull` (o GitHub Desktop → *Pull origin*).
2. Abre `UnityProject/` en Unity. Importará el paquete *Visual Studio Editor*; espera a que termine y a que la Consola no tenga errores en rojo.
3. Comprueba que **Edit → Project Settings → Player → Version** muestra `0.14.0`.
4. **Regenera el proyecto Xcode desde Unity** con **MVP → Build immersive visionOS app** y compila **NativeHost** en Xcode. El código Swift del host cambió en esta versión y la carpeta de Xcode es un resultado generado: bajar el repositorio **no** actualiza un proyecto Xcode que ya generaste.
5. Abre la ventana debug; su título debe decir `Unity → RealityKit v0.14.0 (…) · DEBUG`.

## 2. Movimiento fluido: envío dinámico

**El problema.** El visor dibuja unos 90 fotogramas por segundo, pero Unity enviaba la escena solo 10 veces por segundo y el host no suavizaba los huecos. Los objetos en movimiento avanzaban a saltos visibles.

**La solución.** `VisionSceneBridge` decide ahora por sí mismo cuándo enviar:

| Situación | Qué se envía |
| --- | --- |
| Algo se mueve, cambia de color, aparece, desaparece, o se da una orden de ancla o de audio | Una instantánea **en ese frame de Unity** (hasta ~90 por segundo) |
| Todo está quieto | Un pequeño latido, 2 por segundo |

Se ajusta en el componente `VisionSceneBridge`: **Publish Rate** (máximo de instantáneas por segundo mientras algo se mueve; `0` = cada frame) e **Idle Rate** (latido). Si una escena con muchísimos objetos carga la CPU, pon Publish Rate en 60.

**Cómo comprobarlo.** En la ventana debug: los fps de `Render` y de `Unity` deben ser altos, y `msg/s` debe subir hacia los fps mientras algo se mueve. Con la escena quieta, `msg/s` ≈ 2 y un peor intervalo ≈ 500 ms es **normal**.

## 3. Una API sencilla de controles

Tres capas, de la más simple a la más flexible:

1. **Plantillas** — copia un archivo de `Assets/Scripts/Templates/`, cambia el nombre de la clase y añádelo a un objeto. Siete comportamientos listos (tap, pellizco, agarrar, tocar, seguir la mano, anclar) y la tarjeta de museo. Véase la tabla en la [referencia](api.md#7-plantillas-copiar-renombrar-editar).
2. **`VisionInteractable`** — eventos por objeto (`Tapped`, `GrabBegan`, `TouchBegan`…) en C# o en el Inspector.
3. **`VisionControls`** — lee las manos desde cualquier `Update()`: `if (VisionControls.GetPinchDown(VisionHand.Right)) …`.

Por debajo siguen existiendo los eventos en bruto de `VisionInput`, sin cambios.

**Nota honesta:** el pellizco de `VisionControls` es un umbral de distancia calculado en Unity (2,5 cm para empezar, 4 cm para terminar). No es el gesto del sistema de Apple, así que pueden diferir ligeramente. El tap del sistema (mirar + pellizcar) es `VisionObject.SelectionChanged` / `VisionInteractable.Tapped`.

## 4. Tarjetas de información (carteles de museo)

Pon la **Shape** de un `VisionObject` en **Panel**. El visor dibuja una tarjeta de cristal con título y texto:

- Ancho y alto = la escala **X** e **Y** del objeto, en metros (p. ej. `0,5 × 0,3`).
- `SetPanelText(título, texto)` cambia el contenido en cualquier momento.
- El color del objeto tiñe la barra de acento de la tarjeta.
- `Panel Face User` (activado por defecto) hace que la tarjeta gire hacia quien mira.
- Es un objeto normal: ponle `VisionInteractable` (Grabbable) para que el usuario pueda **moverla**, ánclala, o ocúltala con `SetActive(false)`.

Usa `TemplateMuseumInfo` para el flujo completo: tocas un cuadro → aparece la tarjeta a su lado → la mueves con la mano → la tocas para cerrarla. Los pasos de montaje están al principio de ese archivo.

Límites: solo texto y una barra de color; sin imágenes ni botones; el texto no se ajusta solo a la tarjeta, así que agranda la escala para textos largos.

## 5. Cambios que pueden afectar a tus escenas

| Cambio | Qué puedes notar | Qué hacer |
| --- | --- | --- |
| `displayOffset` 0,6 m → `0` | Los objetos aparecen 0,6 m a la **izquierda** de donde estaban | Nada, las coordenadas de Unity ya son exactas. Mueve tus objetos +0,6 m en X si quieres el aspecto anterior. |
| Envío dinámico | Una escena quieta envía solo ~2 msg/s | Es normal. Véase la sección 2. |
| Cubo magenta eliminado | Ya no está el marcador nativo | Nada. |
| `AttachToAnchor` frente a `PlaceAnchor` | `AttachToAnchor` sigue creando el ancla solo si falta | Usa `PlaceAnchor` cuando quieras **reemplazar** un ancla. |
| Agarrar un objeto anclado | `VisionInteractable` ahora lo suelta de su ancla | Vuelve a anclarlo al soltar con `PlaceAnchor` (véase `TemplateGrabThenAnchor`). |

## 6. Ayudas de depuración

- **Ventana debug:** `Render N fps · Unity X / Y fps máx` y las estadísticas de mensajes. `Y` es lo máximo que Unity puede ejecutar (frame rate objetivo, o la frecuencia de la pantalla dividida por el intervalo de vSync).
- **VS Code / Visual Studio:** en Unity, **Settings → External Tools → External Script Editor** = Visual Studio Code, y abre un script desde Unity (**Assets → Open C# Project**). El paquete está declarado en `Packages/manifest.json`.
- **Mirada:** las apps no pueden leer hacia dónde mira el usuario. El sistema resalta el objeto que recibirá el pellizco; fíate de ese resalte.

## 7. Lo que sigue sin verificarse

El C# compila y el Swift pasa la comprobación de tipos con el SDK de visionOS. El pellizco, el toque, el agarre, las anclas, las superficies, el audio y las tarjetas **no** se han verificado en un Apple Vision Pro real en esta versión. Cuéntanos lo que observes (dispositivo, versión de visionOS, permisos concedidos).
