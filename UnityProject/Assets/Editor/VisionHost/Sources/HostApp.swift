import SwiftUI
import RealityKit

// Build-time switches written into Info.plist by integrate_host.rb.
// MVPDebugUI=false is the production mode: no control window, no native marker.
enum HostConfig {
    static let debugUI = Bundle.main.object(forInfoDictionaryKey: "MVPDebugUI") as? Bool ?? true
}

@main struct HostApp: App {
    @StateObject private var bridge = Bridge.shared
    @State private var style: ImmersionStyle = .mixed
    var body: some SwiftUI.Scene {
        WindowGroup(id: "launch") { LaunchView().environmentObject(bridge) }
        ImmersiveSpace(id: "cube-space") {
            SceneSpace().environmentObject(bridge)
        }
        .immersionStyle(selection: $style, in: .mixed)
    }
}

// Startup is automatic: load assets -> open the immersive space -> start Unity.
// Debug builds keep the window with manual controls and live diagnostics;
// production builds show only a short loading message, then hide the window.
struct LaunchView: View {
    @EnvironmentObject private var bridge: Bridge
    @StateObject private var sensors = SensorService.shared
    @Environment(\.openImmersiveSpace) private var open
    @Environment(\.dismissImmersiveSpace) private var dismiss
    @Environment(\.dismissWindow) private var dismissWindow
    @AppStorage("mvp.autoStart") private var autoStart = true
    @State private var isOpen = false
    @State private var unityStarted = false
    @State private var failed = false
    @State private var message = "Preparando modelos exportados de Unity."
    @State private var modelsReady = false
    @State private var modelCount = 0

    var body: some View {
        Group {
            if HostConfig.debugUI { debugPanel } else { productionPanel }
        }
        .padding()
        .task { await prepare() }
    }

    private var productionPanel: some View {
        VStack(spacing: 16) {
            if failed {
                Text(message)
                Button("Reintentar") { Task { await run() } }
            } else {
                ProgressView()
                Text("Cargando experiencia…")
            }
        }
    }

    private var debugPanel: some View {
        VStack(spacing: 16) {
            Text("Unity → RealityKit · sensores v0.12 · DEBUG")
            Text(message)
            Text("Objetos: \(bridge.objects.count) · mensajes: \(bridge.messageCount) · modelos USD: \(modelCount)")
            Text("\(sensors.status) · superficies: \(sensors.surfaceCount)")
            TimelineView(.periodic(from: .now, by: 1)) { _ in
                // Polled once per second so diagnostics add no per-frame redraws.
                Text(bridge.takeStats().summary).monospacedDigit()
            }
            Toggle("Inicio automático", isOn: $autoStart).frame(maxWidth: 320)
            Button(isOpen ? "Cerrar espacio" : "1. Abrir espacio nativo") {
                Task {
                    if isOpen {
                        await dismiss()
                        isOpen = false
                        message = "Espacio cerrado."
                    } else {
                        await openSpace()
                    }
                }
            }
            Button("2. Iniciar lógica Unity") { startUnity() }
                .disabled(!isOpen || !modelsReady || unityStarted)
            Button("Ocultar esta ventana") { dismissWindow(id: "launch") }
        }
    }

    private func prepare() async {
        await ModelAssetStore.shared.loadAll()
        AudioAssetStore.shared.loadAll()
        modelCount = ModelAssetStore.shared.count
        modelsReady = true
        message = "Modelos preparados: \(modelCount)."
        if autoStart || !HostConfig.debugUI { await run() }
        else { message += " Abre el espacio." }
    }

    private func run() async {
        failed = false
        await openSpace()
        guard isOpen else { failed = true; return }
        startUnity()
        if !HostConfig.debugUI { dismissWindow(id: "launch") }
    }

    private func openSpace() async {
        guard !isOpen else { return }
        switch await open(id: "cube-space") {
        case .opened:
            isOpen = true
            message = "Espacio abierto; el marcador magenta es nativo."
            print("[MVP] Immersive space opened")
        case .userCancelled:
            message = "Apertura cancelada."
        case .error:
            message = "Error al abrir el espacio."
        @unknown default:
            message = "Resultado desconocido al abrir el espacio."
        }
    }

    private func startUnity() {
        guard isOpen, modelsReady, !unityStarted else { return }
        unityStarted = true
        UnityRuntime.shared.start()
        message = "Unity iniciado; las piezas vienen de C#."
    }
}

struct SceneSpace: View {
    @EnvironmentObject private var bridge: Bridge
    @StateObject private var sensors = SensorService.shared
    private let entityPrefix = "vision:"

    var body: some View {
        RealityView { content in
            // Debug-only fixed reference marker. It never receives a Unity transform.
            if HostConfig.debugUI {
                let marker = ModelEntity(mesh: .generateBox(size: 0.3),
                                         materials: [UnlitMaterial(color: .magenta)])
                marker.name = "native-marker"
                marker.position = [-0.8, 1.45, -1.4]
                content.add(marker)
            }
            print("[MVP] RealityView created")
        } update: { content in
            let currentIDs = Set(bridge.objects.map(\.id))
            // A v2 frame is a complete snapshot: remove objects absent from it.
            for entity in content.entities where entity.name.hasPrefix(entityPrefix) {
                let id = String(entity.name.dropFirst(entityPrefix.count))
                if !currentIDs.contains(id) {
                    AudioPlaybackTracker.shared.remove(id: id)
                    content.remove(entity)
                }
            }
            for state in bridge.objects {
                let name = entityPrefix + state.id
                let root: Entity
                if let existing = content.entities.first(where: { $0.name == name }) {
                    root = existing
                } else {
                    root = Entity()
                    root.name = name
                    if state.shape == "model", let asset = state.asset,
                       let model = ModelAssetStore.shared.clone(asset) {
                        root.addChild(model)
                    } else {
                        let mesh: MeshResource = state.shape == "sphere"
                            ? .generateSphere(radius: 0.5) : .generateBox(size: 1)
                        root.addChild(ModelEntity(mesh: mesh,
                            materials: [SimpleMaterial(color: .white, isMetallic: false)]))
                    }
                    // Approximate unit collision; later derive it from mesh bounds.
                    root.components.set(InputTargetComponent())
                    root.components.set(CollisionComponent(shapes: [.generateBox(size: [1, 1, 1])]))
                    content.add(root)
                }
                root.transform = Transform(scale: state.scale.simd,
                                           rotation: state.rotation.simd,
                                           translation: state.position.simd + [0.6, 0, 0])
                AudioPlaybackTracker.shared.update(id: state.id, key: state.audio,
                                                   sequence: state.audioSequence,
                                                   entity: root)
                let c = state.color
                let color = UIColor(red: CGFloat(c.r), green: CGFloat(c.g),
                                    blue: CGFloat(c.b), alpha: 1)
                // USD models keep their authored texture. Primitive pieces use
                // the color provided by C# in each protocol frame.
                if state.shape != "model" { colorize(root, color: color) }
            }
        }
        .gesture(SpatialTapGesture().targetedToAnyEntity().onEnded { value in
            var entity: Entity? = value.entity
            while let current = entity {
                if current.name.hasPrefix(entityPrefix) {
                    bridge.selectObject(String(current.name.dropFirst(entityPrefix.count)))
                    break
                }
                entity = current.parent
            }
        })
        .simultaneousGesture(DragGesture(minimumDistance: 0.02).targetedToAnyEntity()
            .onChanged { value in sendDrag(value, phase: "changed") }
            .onEnded { value in sendDrag(value, phase: "ended") })
        .task { await sensors.start() }
        .onAppear { print("[MVP] SceneSpace appeared") }
        .onDisappear {
            sensors.stop()
            print("[MVP] SceneSpace disappeared")
        }
    }

    private func colorize(_ entity: Entity, color: UIColor) {
        if let model = entity as? ModelEntity {
            model.model?.materials = [SimpleMaterial(color: color, isMetallic: false)]
        }
        for child in entity.children { colorize(child, color: color) }
    }

    private func sendDrag(_ value: EntityTargetValue<DragGesture.Value>, phase: String) {
        var entity: Entity? = value.entity
        while let current = entity {
            if current.name.hasPrefix(entityPrefix) {
                let id = String(current.name.dropFirst(entityPrefix.count))
                let point = value.convert(value.location3D, from: .local, to: .scene)
                // Inverse of the host's +0.6 X display offset.
                let payload: [String: Any] = ["type": "drag", "id": id,
                    "phase": phase,
                    "position": ["x": point.x - 0.6, "y": point.y, "z": point.z]]
                if let data = try? JSONSerialization.data(withJSONObject: payload),
                   let json = String(data: data, encoding: .utf8) {
                    bridge.sendNativeEvent(json)
                }
                break
            }
            entity = current.parent
        }
    }

}
