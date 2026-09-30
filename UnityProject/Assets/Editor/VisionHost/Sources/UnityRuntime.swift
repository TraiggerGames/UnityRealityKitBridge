import Foundation
import UIKit
import UnityFramework

// The native app owns the SwiftUI lifecycle; Unity runs as an embedded library.
// This adapter is intentionally small so generated Unity files stay untouched.
@MainActor final class UnityRuntime {
    static let shared = UnityRuntime()
    private var framework: UnityFramework?
    func start() {
        guard framework == nil, let instance = UnityFramework.getInstance() else { return }
        // Unity's generated runEmbedded makes its own UIWindow key and visible.
        // Keep a reference to the SwiftUI host window so we can restore it.
        let hostWindow = UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
            .flatMap(\.windows)
            .first { $0.isKeyWindow }
        // The Xcode target copies Unity's Data folder into NativeHost.app/Data.
        // Leave Unity's bundle override unset so Filesystem.mm uses mainBundle.
        // Setting com.unity3d.framework here made IL2CPP look for metadata in
        // UnityFramework.framework/Data, which does not exist in this build.
        instance.runEmbedded(withArgc: CommandLine.argc,
                             argv: CommandLine.unsafeArgv,
                             appLaunchOpts: nil)
        // We use Unity for logic; its 2D window must not cover RealityKit.
        instance.appController()?.window.isHidden = true
        hostWindow?.makeKeyAndVisible()
        print("[MVP] Unity started; restored native host window")
        framework = instance
        Bridge.shared.sendSelectionToUnity = { [weak self] event in
            // Protocol v1 targets the old Cube; v2 targets the scene router.
            let receiver = Bridge.shared.protocolVersion == 2 ? "VisionSceneBridge" : "Cube"
            receiver.withCString { objectName in
                "OnNativeSelect".withCString { methodName in
                    event.withCString { payload in
                        self?.framework?.sendMessageToGO(withName: objectName,
                                                         functionName: methodName,
                                                         message: payload)
                    }
                }
            }
        }
        Bridge.shared.sendEventToUnity = { [weak self] json in
            "VisionSceneBridge".withCString { objectName in
                "OnNativeEvent".withCString { methodName in
                    json.withCString { payload in
                        self?.framework?.sendMessageToGO(withName: objectName,
                                                         functionName: methodName,
                                                         message: payload)
                    }
                }
            }
        }
    }
}
