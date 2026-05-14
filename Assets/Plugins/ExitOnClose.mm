#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>

extern "C" {
    void _ExitOnClose() {
        dispatch_async(dispatch_get_main_queue(), ^{
            [NSApp terminate:nil];
        });
    }
}
