using System;
using System.Runtime.InteropServices;

[ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
class DesktopActivator {}
[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDesktopActivator {
    [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)]string appId,
        [MarshalAs(UnmanagedType.LPWStr)]string arguments,uint options,out uint processId);
}
internal static class CodexDesktop {
    public static void Open(){
        var activation=(IDesktopActivator)new DesktopActivator();
        uint pid;
        try{Marshal.ThrowExceptionForHR(activation.ActivateApplication("OpenAI.Codex_2p2nqsd0c76g0!App","",0,out pid));}
        finally{Marshal.ReleaseComObject(activation);}
    }
}
