using System.Runtime.InteropServices;
using CFAllocatorRef = System.IntPtr;
using CFStringEncoding = System.Int32;
using CFStringRef = System.IntPtr;
using CFTypeRef = System.IntPtr;

namespace ManagedMidi.CoreMidi;

internal class CoreFoundationInterop
{
    const string LibraryName = "/System/Library/Frameworks/CoreFoundation.framework/Resources/BridgeSupport/CoreFoundation.dylib";

    public const CFStringEncoding kCFStringEncodingUTF8 = 0x08000100;

    [DllImport(LibraryName)]
    internal static extern void CFRelease(CFTypeRef cf);

    [DllImport(LibraryName)]
    internal static extern CFStringRef CFStringCreateWithCString(CFAllocatorRef alloc, string cStr, CFStringEncoding encoding);

    [DllImport(LibraryName)]
    internal static extern CFStringRef CFStringCreateWithCStringNoCopy(CFAllocatorRef alloc, string cStr, CFStringEncoding encoding, CFAllocatorRef contentsDeallocator);

    [DllImport(LibraryName)]
    internal static extern IntPtr CFStringGetCStringPtr(CFStringRef theString, CFStringEncoding encoding);
}
