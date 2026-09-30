import Foundation
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

@MainActor final class Bridge: ObservableObject {
    static let shared = Bridge()
    @Published private(set) var objects: [SceneObjectState] = []
    @Published private(set) var messageCount = 0
    @Published private(set) var protocolVersion = 0
    var sendSelectionToUnity: ((String) -> Void)?
    var sendEventToUnity: ((String) -> Void)?
    private var observer: NSObjectProtocol?

    private init() {
        observer = NotificationCenter.default.addObserver(
            forName: NSNotification.Name("MVP.CubeState"), object: nil, queue: nil
        ) { notification in
            guard let json = notification.userInfo?["json"] as? String else { return }
            Task { @MainActor in Bridge.shared.receive(json) }
        }
    }

    func receive(_ json: String) {
        guard let data = json.data(using: .utf8) else { return }
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
