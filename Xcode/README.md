# Proyecto Xcode

`Generated/` contiene una copia local del export visionOS más reciente y está excluido de Git. Se genera de nuevo desde el proyecto Unity con **MVP → Build immersive visionOS app**. El script de post build añade `NativeHost`, empaqueta `UnityFramework`, modelos y audio, y configura los permisos de manos y entorno.

En Xcode selecciona el esquema **NativeHost** para firmar, instalar y depurar en Vision Pro. Configura el identificador de paquete y el equipo de firma en Unity Player Settings; el target nativo hereda esos valores. La copia local actual puede requerir esa configuración antes de instalarse.

El código Swift que se versiona está en `../UnityProject/Assets/Editor/VisionHost/Sources/`. No edites los archivos de `Generated/`: otro build los sustituirá.
