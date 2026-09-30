import Foundation
import RealityKit

// Loads exported USDA files before opening the immersive scene. RealityView's
// update closure is synchronous, so it clones these preloaded templates.
@MainActor final class ModelAssetStore {
    static let shared = ModelAssetStore()
    private(set) var templates: [String: Entity] = [:]
    private(set) var failures: [String] = []
    private var loaded = false

    func loadAll() async {
        guard !loaded else { return }
        loaded = true
        let urls = Bundle.main.urls(forResourcesWithExtension: "usda", subdirectory: "VisionModels") ?? []
        for url in urls {
            let key = url.deletingPathExtension().lastPathComponent
            do {
                templates[key] = try await Entity(contentsOf: url)
                print("[MVP] Loaded USD model: \(key)")
            } catch {
                failures.append(key)
                print("[MVP] Failed USD model \(key): \(error)")
            }
        }
    }

    func clone(_ key: String) -> Entity? { templates[key]?.clone(recursive: true) }
    var count: Int { templates.count }
}
