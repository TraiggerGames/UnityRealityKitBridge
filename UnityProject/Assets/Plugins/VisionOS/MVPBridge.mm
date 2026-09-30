#import <Foundation/Foundation.h>

// Compiled into UnityFramework, where IL2CPP resolves DllImport("__Internal").
// The SwiftUI host observes the notification in the same process.
extern "C" void MVP_SendCubeState(const char *utf8JSON)
{
    if (utf8JSON == nullptr) return;
    NSString *json = [NSString stringWithUTF8String:utf8JSON];
    if (json == nil) return;
    [[NSNotificationCenter defaultCenter]
        postNotificationName:@"MVP.CubeState"
        object:nil
        userInfo:@{ @"json": json }];
}
