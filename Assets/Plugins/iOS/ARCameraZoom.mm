#import <AVFoundation/AVFoundation.h>

extern "C" {
    float _GetMaxZoomFactor() {
        AVCaptureDevice *videoDevice = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
        if (videoDevice) {
            return videoDevice.activeFormat.videoMaxZoomFactor;
        }
        return 1.0f;
    }

    void _SetZoomFactor(float zoomFactor) {
        NSError *error = nil;
        AVCaptureDevice *videoDevice = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
        
        if (videoDevice && [videoDevice lockForConfiguration:&error]) {
            // Clamp zoom factor to device limits (and a safe practical limit like 5.0)
            float maxZoom = videoDevice.activeFormat.videoMaxZoomFactor;
            if (maxZoom > 5.0f) maxZoom = 5.0f; // Limit to 5x to avoid extreme shaking
            
            if (zoomFactor > maxZoom) zoomFactor = maxZoom;
            if (zoomFactor < 1.0f) zoomFactor = 1.0f;
            
            videoDevice.videoZoomFactor = zoomFactor;
            [videoDevice unlockForConfiguration];
        }
    }
}
