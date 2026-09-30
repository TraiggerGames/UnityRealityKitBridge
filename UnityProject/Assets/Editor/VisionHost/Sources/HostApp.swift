import SwiftUI
import RealityKit

// Build-time switches written into Info.plist by integrate_host.rb.
// MVPDebugUI=false is the production mode: no control window, no native marker.
enum HostConfig {
    static let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "?"
    static let build = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? "?"
    static let debugUI = Bundle.main.object(forInfoDictionaryKey: "MVPDebugUI") as? Bool ?? true
    // Demo shift between Unity's origin and the native marker. Anchors use the
    // same shift so Unity coordinates mean the same place everywhere.
    static let displayOffset: SIMD3<Float> = [0.6, 0, 0]
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
    @StateObject private var anchors = AnchorService.shared
    @StateObject private var map = MapService.shared
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
            Text("Unity → RealityKit v\(HostConfig.version) (\(HostConfig.build)) · DEBUG")
            Text(message)
            Text("Objetos: \(bridge.objects.count) · mensajes: \(bridge.messageCount) · modelos USD: \(modelCount)")
            Text("\(sensors.status) · superficies: \(sensors.surfaceCount)")
            Text("\(anchors.status) · ancladas: \(anchors.poses.count) · malla: \(map.meshCount)")
            Picker("Malla", selection: Binding(get: { map.mode }, set: { map.setMode($0) })) {
                ForEach(MapMode.allCases, id: \.self) { Text($0.rawValue).tag($0) }
            }.pickerStyle(.segmented).frame(maxWidth: 360)
            Button("Borrar anclas") { anchors.clearAll() }
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
    @StateObject private var anchors = AnchorService.shared
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
            // Room mesh (empty unless Unity or the debug window enables it).
            MapService.shared.root.removeFromParent()
            content.add(MapService.shared.root)
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
                if let anchorID = state.anchor, !anchorID.isEmpty {
                    // Anchored: position/rotation are local to the anchor, and
                    // the object stays hidden until ARKit knows where it is.
                    let local = Transform(scale: state.scale.simd, rotation: state.rotation.simd,
                                          translation: state.position.simd).matrix
                    if let pose = anchors.poses[anchorID] {
                        root.transform = Transform(matrix: pose * local)
                        root.isEnabled = true
                    } else {
                        root.isEnabled = false
                    }
                } else {
                    root.isEnabled = true
                    root.transform = Transform(scale: state.scale.simd,
                                               rotation: state.rotation.simd,
                                               translation: state.position.simd + HostConfig.displayOffset)
                }
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
            if HostConfig.debugUI { syncAnchorMarkers(content) }
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
        .task { await anchors.start() }
        .task { await MapService.shared.resume() }
        .onAppear { print("[MVP] SceneSpace appeared") }
        .onDisappear {
            sensors.stop()
            anchors.stop()
            MapService.shared.stop()
            print("[MVP] SceneSpace disappeared")
        }
    }

    // Debug only: a small cyan sphere at every tracked anchor.
    private func syncAnchorMarkers(_ content: RealityViewContent) {
        let prefix = "anchor:"
        for entity in content.entities where entity.name.hasPrefix(prefix) {
            if anchors.poses[String(entity.name.dropFirst(prefix.count))] == nil { content.remove(entity) }
        }
        for (name, pose) in anchors.poses {
            let marker: Entity
            if let existing = content.entities.first(where: { $0.name == prefix + name }) {
                marker = existing
            } else {
                marker = ModelEntity(mesh: .generateSphere(radius: 0.03),
                                     materials: [UnlitMaterial(color: .cyan)])
                marker.name = prefix + name
                content.add(marker)
            }
            marker.transform = Transform(matrix: pose)
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
                // Inverse of the host's display offset.
                let payload: [String: Any] = ["type": "drag", "id": id,
                    "phase": phase,
                    "position": ["x": point.x - HostConfig.displayOffset.x, "y": point.y, "z": point.z]]
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
