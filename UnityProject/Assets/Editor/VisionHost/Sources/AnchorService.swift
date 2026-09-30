import ARKit
import Foundation
import simd

// Unity asks for anchors by name; ARKit keeps them across launches (WorldAnchor)
// and the name -> UUID table is stored in UserDefaults. Unity receives one
// "anchor" event per change and objects that declare an anchor are placed
// relative to its pose by SceneSpace.
struct AnchorCommand: Decodable {
    let id: String
    let op: String // ensure (create if missing), place (replace), remove
    let sequence: Int
    let position: V3?
    let rotation: Q4?
}

@MainActor final class AnchorService: ObservableObject {
    static let shared = AnchorService()
    @Published private(set) var poses: [String: simd_float4x4] = [:]
    @Published private(set) var status = "Anclas inactivas"
    private let storeKey = "mvp.anchors"
    private var names: [String: UUID] = [:]
    private var session: ARKitSession?
    private var provider: WorldTrackingProvider?
    private var watcher: Task<Void, Never>?
    private var chain: Task<Void, Never>?
    private var lastSequence = 0
    private var lastEvents: [String: String] = [:]

    private init() {
        let stored = UserDefaults.standard.dictionary(forKey: storeKey) as? [String: String] ?? [:]
        names = stored.compactMapValues { UUID(uuidString: $0) }
    }

    func start() async {
        guard session == nil else { return }
        guard WorldTrackingProvider.isSupported else {
            status = "Anclas no disponibles en este dispositivo"
            return
        }
        let newSession = ARKitSession()
        let newProvider = WorldTrackingProvider()
        session = newSession
        status = "Solicitando acceso para anclas…"
        let authorization = await newSession.requestAuthorization(for: [.worldSensing])
        guard session === newSession else { return }
        guard authorization[.worldSensing] == .allowed else {
            status = "Sin permiso para anclas"
            session = nil
            return
        }
        do {
            try await newSession.run([newProvider])
        } catch {
            status = "Anclas no disponibles: \(error.localizedDescription)"
            print("[MVP] Anchor session failed: \(error)")
            session = nil
            return
        }
        guard session === newSession else { newSession.stop(); return }
        provider = newProvider
        status = "Anclas activas (\(names.count) guardadas)"
        watcher = Task { await watch(newProvider) }
    }

    func stop() {
        watcher?.cancel()
        watcher = nil
        session?.stop()
        session = nil
        provider = nil
        poses.removeAll()
        status = "Anclas inactivas"
    }

    /// Runs commands from the repeating Unity frame once each. Until the
    /// session is ready nothing is consumed, so the next frame retries.
    func apply(_ commands: [AnchorCommand]) {
        guard provider != nil else { return }
        for command in commands.sorted(by: { $0.sequence < $1.sequence })
        where command.sequence > lastSequence {
            lastSequence = command.sequence
            let previous = chain
            chain = Task { await previous?.value; await execute(command) }
        }
    }

    func clearAll() {
        for name in Array(names.keys) {
            let previous = chain
            chain = Task { await previous?.value; await remove(name) }
        }
    }

    /// Unity may start after ARKit restored the anchors; resend what we know.
    func replay() {
        for json in lastEvents.values { Bridge.shared.sendNativeEvent(json) }
    }

    private func execute(_ command: AnchorCommand) async {
        switch command.op {
        case "remove": await remove(command.id)
        case "ensure": if names[command.id] == nil { await place(command) }
        case "place": await place(command)
        default: print("[MVP] Unknown anchor op: \(command.op)")
        }
    }

    private func place(_ command: AnchorCommand) async {
        guard let provider, let position = command.position, let rotation = command.rotation else { return }
        let world = Transform(scale: .one, rotation: rotation.simd,
                              translation: position.simd + HostConfig.displayOffset).matrix
        let anchor = WorldAnchor(originFromAnchorTransform: world)
        let old = names[command.id]
        // Saved before addAnchor so the first ARKit update can already be named.
        names[command.id] = anchor.id
        persist()
        do {
            try await provider.addAnchor(anchor)
            if let old { try? await provider.removeAnchor(forID: old) }
            status = "Anclas activas (\(names.count) guardadas)"
        } catch {
            print("[MVP] addAnchor failed for \(command.id): \(error)")
            names[command.id] = old
            persist()
        }
    }

    private func remove(_ name: String) async {
        guard let id = names.removeValue(forKey: name) else { return }
        persist()
        poses[name] = nil
        emit(name, phase: "removed", tracked: false, pose: nil)
        if let provider { try? await provider.removeAnchor(forID: id) }
    }

    private func watch(_ provider: WorldTrackingProvider) async {
        for await update in provider.anchorUpdates {
            if Task.isCancelled { return }
            let anchor = update.anchor
            guard let name = names.first(where: { $0.value == anchor.id })?.key else { continue }
            switch update.event {
            case .removed:
                // ARKit lost it: forget the name so Unity can ensure it again.
                names[name] = nil
                persist()
                poses[name] = nil
                emit(name, phase: "removed", tracked: false, pose: nil)
            case .added, .updated:
                // An untracked anchor keeps its last pose until tracking returns.
                if anchor.isTracked { poses[name] = anchor.originFromAnchorTransform }
                let phase = update.event == .added ? "added" : "updated"
                emit(name, phase: phase, tracked: anchor.isTracked, pose: poses[name])
            }
        }
    }

    private func persist() {
        UserDefaults.standard.set(names.mapValues { $0.uuidString }, forKey: storeKey)
    }

    private func emit(_ name: String, phase: String, tracked: Bool, pose: simd_float4x4?) {
        var payload: [String: Any] = ["type": "anchor", "id": name,
                                      "phase": phase, "tracked": tracked]
        if let pose {
            let p = pose.columns.3 - SIMD4<Float>(HostConfig.displayOffset, 0)
            let q = simd_quatf(pose)
            payload["position"] = ["x": p.x, "y": p.y, "z": p.z]
            payload["rotation"] = ["x": q.imag.x, "y": q.imag.y, "z": q.imag.z, "w": q.real]
        }
        guard let data = try? JSONSerialization.data(withJSONObject: payload),
              let json = String(data: data, encoding: .utf8) else { return }
        if phase == "removed" { lastEvents[name] = nil } else { lastEvents[name] = json }
        Bridge.shared.sendNativeEvent(json)
    }
}
