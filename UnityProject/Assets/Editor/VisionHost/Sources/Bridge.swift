import Foundation
import QuartzCore
import simd

// The bridge accepts the new multi-object protocol (v2) and the old cube
// protocol (v1), so a freshly built host can still open the older Unity export.
struct V3: Decodable {
    let x: Float, y: Float, z: Float
    var simd: SIMD3<Float> { [x, y, z] }
}
struct Q4: Decodable {
    let x: Float, y: Float, z: Float, w: Float
    var simd: simd_quatf { simd_quatf(ix: x, iy: y, iz: z, r: w) }
}
struct RGB: Decodable { let r: Float, g: Float, b: Float }
struct SceneObjectState: Decodable {
    let id: String
    let shape: String
    let asset: String?
    let audio: String?
    let audioSequence: Int?
    let position: V3
    let rotation: Q4
    let scale: V3
    let color: RGB
}
private struct SceneFrame: Decodable {
    let version: Int
    let objects: [SceneObjectState]
}
private struct LegacyCube: Decodable {
    let version: Int
    let id: String
    let selected: Bool
    let position: V3
    let rotation: Q4
    let scale: V3
    var asSceneObject: SceneObjectState {
        SceneObjectState(id: id, shape: "box", asset: nil, audio: nil,
                         audioSequence: nil, position: position,
                         rotation: rotation, scale: scale,
                         color: selected ? RGB(r: 1, g: 0.5, b: 0.1)
                                         : RGB(r: 0.1, g: 0.4, b: 1))
    }
}

// Lightweight counters for the debug window. Not @Published on purpose:
// publishing them would re-run the RealityView update on every frame.
struct BridgeStats {
    var messagesPerSecond = 0.0
    var lastPayloadBytes = 0
    var avgDecodeMs = 0.0
    var worstGapMs = 0.0
    var summary: String {
        String(format: "%.0f msg/s · %d B · decode %.2f ms · peor intervalo %.0f ms",
               messagesPerSecond, lastPayloadBytes, avgDecodeMs, worstGapMs)
    }
}

@MainActor final class Bridge: ObservableObject {
    static let shared = Bridge()
    @Published private(set) var objects: [SceneObjectState] = []
    @Published private(set) var messageCount = 0
    @Published private(set) var protocolVersion = 0
    var sendSelectionToUnity: ((String) -> Void)?
    var sendEventToUnity: ((String) -> Void)?
    private var observer: NSObjectProtocol?
    private var stats = BridgeStats()
    private var windowStart = CACurrentMediaTime()
    private var windowCount = 0
    private var lastReceive = 0.0

    private init() {
        observer = NotificationCenter.default.addObserver(
            forName: NSNotification.Name("MVP.CubeState"), object: nil, queue: nil
        ) { notification in
            guard let json = notification.userInfo?["json"] as? String else { return }
            Task { @MainActor in Bridge.shared.receive(json) }
        }
    }

    /// Returns the stats gathered since the previous call and restarts the window.
    func takeStats() -> BridgeStats {
        let now = CACurrentMediaTime()
        var out = stats
        out.messagesPerSecond = Double(windowCount) / max(now - windowStart, 0.001)
        stats.worstGapMs = 0
        windowStart = now
        windowCount = 0
        return out
    }

    func receive(_ json: String) {
        guard let data = json.data(using: .utf8) else { return }
        let began = CACurrentMediaTime()
        defer {
            let done = CACurrentMediaTime()
            windowCount += 1
            stats.lastPayloadBytes = data.count
            stats.avgDecodeMs += ((done - began) * 1000 - stats.avgDecodeMs) * 0.1
            if lastReceive > 0 { stats.worstGapMs = max(stats.worstGapMs, (began - lastReceive) * 1000) }
            lastReceive = began
        }
        if let frame = try? JSONDecoder().decode(SceneFrame.self, from: data), frame.version == 2 {
            // Reject duplicate IDs before RealityKit entity lookup becomes ambiguous.
            guard Set(frame.objects.map(\.id)).count == frame.objects.count else { return }
            objects = frame.objects
            protocolVersion = 2
        } else if let legacy = try? JSONDecoder().decode(LegacyCube.self, from: data),
                  legacy.version == 1, legacy.id == "cube" {
            objects = [legacy.asSceneObject]
            protocolVersion = 1
        } else {
            return
        }
        messageCount += 1
    }

    func selectObject(_ id: String) {
        let payload = protocolVersion == 1 ? "cube.select" : id
        sendSelectionToUnity?(payload)
    }

    func sendNativeEvent(_ json: String) { sendEventToUnity?(json) }
}
