import SwiftUI
import RealityKit

// Floating text panels (museum labels, info cards). Unity sends a title, a text
// and a size; the host draws them as a SwiftUI view attached to the entity.
// Moving, hiding and anchoring work like any other object because the panel is
// an ordinary VisionObject whose shape is "panel".
struct InfoPanelView: View {
    let title: String
    let text: String
    let accent: Color
    let width: CGFloat   // points
    let height: CGFloat

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Capsule().fill(accent).frame(width: 64, height: 6)
            if !title.isEmpty { Text(title).font(.title2.bold()) }
            if !text.isEmpty { Text(text).font(.body) }
            Spacer(minLength: 0)
        }
        .padding(28)
        .frame(width: width, height: height, alignment: .topLeading)
        .glassBackgroundEffect()
    }
}

// Remembers what a panel entity currently shows, so it is rebuilt only on change.
struct InfoPanelSignature: Component { var value: String }

@MainActor enum InfoPanels {
    // RealityKit view attachments: 1 meter = 1360 points.
    static let pointsPerMeter: CGFloat = 1360

    static func update(_ entity: Entity, state: SceneObjectState) {
        // Panel size comes from the Unity scale (x = width, y = height, meters).
        let widthM = max(CGFloat(state.scale.x), 0.1)
        let heightM = max(CGFloat(state.scale.y), 0.05)
        let c = state.color
        let signature = "\(state.title ?? "")|\(state.text ?? "")|\(widthM)|\(heightM)|\(c.r),\(c.g),\(c.b)"
        if entity.components[InfoPanelSignature.self]?.value != signature {
            entity.components.set(InfoPanelSignature(value: signature))
            entity.components.set(ViewAttachmentComponent(rootView: InfoPanelView(
                title: state.title ?? "", text: state.text ?? "",
                accent: Color(red: Double(c.r), green: Double(c.g), blue: Double(c.b)),
                width: widthM * pointsPerMeter, height: heightM * pointsPerMeter)))
            entity.components.set(CollisionComponent(
                shapes: [.generateBox(size: [Float(widthM), Float(heightM), 0.03])]))
        }
        if state.faceUser == true {
            if entity.components[BillboardComponent.self] == nil {
                entity.components.set(BillboardComponent())
            }
        } else if entity.components[BillboardComponent.self] != nil {
            entity.components.remove(BillboardComponent.self)
        }
    }
}
