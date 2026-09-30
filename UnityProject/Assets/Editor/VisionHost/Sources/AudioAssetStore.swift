import Foundation
import RealityKit

// Preload audio from the app bundle so a Unity Play() command can trigger it
// without file I/O inside RealityView's synchronous update closure.
@MainActor final class AudioAssetStore {
    static let shared = AudioAssetStore()
    private var resources: [String: AudioFileResource] = [:]
    private var loaded = false

    func loadAll() {
        guard !loaded else { return }
        loaded = true
        for ext in ["wav", "aif", "aiff"] {
            for url in Bundle.main.urls(forResourcesWithExtension: ext, subdirectory: "VisionAudio") ?? [] {
                let key = url.deletingPathExtension().lastPathComponent
                do {
                    resources[key] = try AudioFileResource.load(
                        contentsOf: url, withName: key,
                        configuration: AudioFileResource.Configuration())
                    print("[MVP] Loaded spatial audio: \(key)")
                } catch {
                    print("[MVP] Failed spatial audio \(key): \(error)")
                }
            }
        }
    }

    func resource(_ key: String) -> AudioFileResource? { resources[key] }
    var count: Int { resources.count }
}

// A complete Unity snapshot repeats at 10 Hz. The sequence number makes an
// audio command play exactly once while the entity continues to move.
@MainActor final class AudioPlaybackTracker {
    static let shared = AudioPlaybackTracker()
    private var lastSequence: [String: Int] = [:]

    func update(id: String, key: String?, sequence: Int?, entity: Entity) {
        guard let key, let sequence, sequence > 0 else { return }
        let previous = lastSequence[id] ?? 0
        guard sequence > previous else { return }
        lastSequence[id] = sequence
        if let resource = AudioAssetStore.shared.resource(key) {
            entity.playAudio(resource)
        }
    }

    func remove(id: String) { lastSequence.removeValue(forKey: id) }
}
