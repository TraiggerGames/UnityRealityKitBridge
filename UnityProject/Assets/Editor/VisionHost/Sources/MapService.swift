import ARKit
import RealityKit
import UIKit

// Optional room mesh from ARKit scene reconstruction. Unity picks the mode:
// "mesh" draws a wireframe, "occlusion" hides virtual content behind real
// geometry. It is off by default because the mesh costs CPU and battery.
enum MapMode: String, CaseIterable {
    case off, mesh, occlusion
}

@MainActor final class MapService: ObservableObject {
    static let shared = MapService()
    @Published private(set) var mode: MapMode = .off
    @Published private(set) var meshCount = 0
    /// Added to the immersive space by SceneSpace.
    let root = Entity()
    private var session: ARKitSession?
    private var watcher: Task<Void, Never>?
    private var entities: [UUID: ModelEntity] = [:]

    func setMode(_ newMode: MapMode) {
        guard newMode != mode else { return }
        mode = newMode
        if newMode == .off {
            stop()
        } else {
            for entity in entities.values { entity.model?.materials = [material()] }
            Task { await start() }
        }
    }

    /// Reopening the immersive space restarts the session for the current mode.
    func resume() async {
        if mode != .off { await start() }
    }

    func start() async {
        guard session == nil, mode != .off, SceneReconstructionProvider.isSupported else { return }
        let newSession = ARKitSession()
        let provider = SceneReconstructionProvider()
        session = newSession
        let authorization = await newSession.requestAuthorization(for: [.worldSensing])
        guard session === newSession else { return }
        guard authorization[.worldSensing] == .allowed else { session = nil; return }
        do {
            try await newSession.run([provider])
        } catch {
            print("[MVP] Scene reconstruction failed: \(error)")
            session = nil
            return
        }
        guard session === newSession else { newSession.stop(); return }
        watcher = Task { await watch(provider) }
    }

    /// Stops the session and clears the mesh; the chosen mode is kept.
    func stop() {
        watcher?.cancel()
        watcher = nil
        session?.stop()
        session = nil
        for entity in entities.values { entity.removeFromParent() }
        entities.removeAll()
        meshCount = 0
    }

    private func watch(_ provider: SceneReconstructionProvider) async {
        for await update in provider.anchorUpdates {
            if Task.isCancelled { return }
            let anchor = update.anchor
            switch update.event {
            case .removed:
                entities.removeValue(forKey: anchor.id)?.removeFromParent()
            case .added, .updated:
                guard let mesh = try? await MeshResource.generate(from: [descriptor(anchor.geometry)]),
                      !Task.isCancelled else { continue }
                let matrix = anchor.originFromAnchorTransform
                if let existing = entities[anchor.id] {
                    existing.model?.mesh = mesh
                    existing.transform = Transform(matrix: matrix)
                } else {
                    let entity = ModelEntity(mesh: mesh, materials: [material()])
                    entity.name = "mesh:" + anchor.id.uuidString
                    entity.transform = Transform(matrix: matrix)
                    root.addChild(entity)
                    entities[anchor.id] = entity
                }
            }
            meshCount = entities.count
        }
    }

    private func material() -> any RealityKit.Material {
        if mode == .occlusion { return OcclusionMaterial() }
        var wire = UnlitMaterial(color: UIColor.cyan)
        wire.triangleFillMode = .lines
        return wire
    }

    private func descriptor(_ geometry: MeshAnchor.Geometry) -> MeshDescriptor {
        let vertices = geometry.vertices
        var positions: [SIMD3<Float>] = []
        positions.reserveCapacity(vertices.count)
        let base = vertices.buffer.contents().advanced(by: vertices.offset)
        for i in 0..<vertices.count {
            let at = vertices.stride * i
            positions.append([base.loadUnaligned(fromByteOffset: at, as: Float.self),
                              base.loadUnaligned(fromByteOffset: at + 4, as: Float.self),
                              base.loadUnaligned(fromByteOffset: at + 8, as: Float.self)])
        }
        let faces = geometry.faces
        var indices: [UInt32] = []
        indices.reserveCapacity(faces.count * 3)
        let raw = faces.buffer.contents()
        for i in 0..<(faces.count * 3) {
            indices.append(faces.bytesPerIndex == 2
                ? UInt32(raw.loadUnaligned(fromByteOffset: i * 2, as: UInt16.self))
                : raw.loadUnaligned(fromByteOffset: i * 4, as: UInt32.self))
        }
        var mesh = MeshDescriptor(name: "room")
        mesh.positions = MeshBuffer(positions)
        mesh.primitives = .triangles(indices)
        return mesh
    }
}
