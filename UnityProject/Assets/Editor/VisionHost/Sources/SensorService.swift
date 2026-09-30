import ARKit
import Foundation
import simd

// ARKit lives in the native host. Unity receives small, documented JSON
// samples and can react through ordinary C# event subscriptions.
@MainActor final class SensorService: ObservableObject {
    static let shared = SensorService()
    @Published private(set) var status = "Sensores inactivos"
    @Published private(set) var surfaceCount = 0
    private var session: ARKitSession?
    private var tasks: [Task<Void, Never>] = []
    private var surfaceIDs = Set<UUID>()
    private var lastHandSample: [String: TimeInterval] = [:]

    func start() async {
        guard session == nil else { return }
        let hands = HandTrackingProvider.isSupported ? HandTrackingProvider() : nil
        let planes = PlaneDetectionProvider.isSupported
            ? PlaneDetectionProvider(alignments: [.horizontal, .vertical]) : nil
        guard hands != nil || planes != nil else {
            status = "ARKit no disponible en este dispositivo"
            return
        }
        let newSession = ARKitSession()
        session = newSession
        status = "Solicitando acceso a manos y superficies…"
        var requested: [ARKitSession.AuthorizationType] = []
        if hands != nil { requested.append(.handTracking) }
        if planes != nil { requested.append(.worldSensing) }
        let authorization = await newSession.requestAuthorization(for: requested)
        guard session === newSession else { return }
        var providers: [any DataProvider] = []
        let allowedHands = hands != nil && authorization[.handTracking] == .allowed
        let allowedPlanes = planes != nil && authorization[.worldSensing] == .allowed
        if allowedHands, let hands { providers.append(hands) }
        if allowedPlanes, let planes { providers.append(planes) }
        guard !providers.isEmpty else {
            status = "Sin permiso para manos ni superficies"
            session = nil
            return
        }
        do {
            // One denied permission does not disable the other provider.
            try await newSession.run(providers)
            guard session === newSession else { newSession.stop(); return }
            status = "Sensores activos: \(allowedHands ? "manos " : "")\(allowedPlanes ? "superficies" : "")"
            if allowedHands, let hands {
                tasks.append(Task { await watchHands(hands) })
            }
            if allowedPlanes, let planes {
                tasks.append(Task { await watchPlanes(planes) })
            }
        } catch {
            status = "Sensores no disponibles: \(error.localizedDescription)"
            print("[MVP] ARKit session failed: \(error)")
            session = nil
        }
    }

    func stop() {
        tasks.forEach { $0.cancel() }
        tasks.removeAll()
        session?.stop()
        session = nil
        surfaceIDs.removeAll()
        surfaceCount = 0
        lastHandSample.removeAll()
        status = "Sensores inactivos"
    }

    private func watchHands(_ provider: HandTrackingProvider) async {
        for await update in provider.anchorUpdates {
            if Task.isCancelled { return }
            let anchor = update.anchor
            let hand = anchor.chirality == .left ? "left" : "right"
            let now = Date().timeIntervalSince1970
            if now - (lastHandSample[hand] ?? 0) < (1.0 / 30.0) { continue }
            lastHandSample[hand] = now
            guard anchor.isTracked, let skeleton = anchor.handSkeleton else {
                emit(["type": "hand", "id": hand, "tracked": false])
                continue
            }
            let index = skeleton.joint(.indexFingerTip)
            let thumb = skeleton.joint(.thumbTip)
            guard index.isTracked, thumb.isTracked else { continue }
            let indexWorld = anchor.originFromAnchorTransform * index.anchorFromJointTransform
            let thumbWorld = anchor.originFromAnchorTransform * thumb.anchorFromJointTransform
            emit(["type": "hand", "id": hand, "tracked": true,
                  "position": position(indexWorld.columns.3),
                  "secondary": position(thumbWorld.columns.3)])
        }
    }

    private func watchPlanes(_ provider: PlaneDetectionProvider) async {
        for await update in provider.anchorUpdates {
            if Task.isCancelled { return }
            let anchor = update.anchor
            let phase: String
            switch update.event {
            case .added: phase = "added"
            case .updated: phase = "updated"
            case .removed: phase = "removed"
            }
            if update.event == .removed { surfaceIDs.remove(anchor.id) }
            else { surfaceIDs.insert(anchor.id) }
            surfaceCount = surfaceIDs.count
            let extent = anchor.geometry.extent
            let world = anchor.originFromAnchorTransform * extent.anchorFromExtentTransform
            let normal = anchor.originFromAnchorTransform * SIMD4<Float>(0, 1, 0, 0)
            let classification: String
            if #available(visionOS 26.0, *) {
                classification = anchor.surfaceClassification.description
            } else {
                classification = anchor.classification.description
            }
            let alignment: String
            switch anchor.alignment {
            case .horizontal: alignment = "horizontal"
            case .vertical: alignment = "vertical"
            @unknown default: alignment = "unknown"
            }
            emit(["type": "surface", "id": anchor.id.uuidString,
                  "phase": phase,
                  "alignment": alignment,
                  "classification": classification,
                  "position": position(world.columns.3), "normal": point(normal),
                  "width": extent.width, "height": extent.height])
        }
    }

    // Positions use Unity's coordinates (the host display offset removed), like
    // drag and anchor events, so they can be compared with Unity transforms.
    private func position(_ v: SIMD4<Float>) -> [String: Float] {
        point(v - SIMD4<Float>(HostConfig.displayOffset, 0))
    }

    private func point(_ v: SIMD4<Float>) -> [String: Float] {
        ["x": v.x, "y": v.y, "z": v.z]
    }

    private func emit(_ object: [String: Any]) {
        guard let data = try? JSONSerialization.data(withJSONObject: object),
              let json = String(data: data, encoding: .utf8) else { return }
        Bridge.shared.sendNativeEvent(json)
    }
}
