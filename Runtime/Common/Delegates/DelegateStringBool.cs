using System.Runtime.InteropServices;

namespace PrimeGames.SDK.Common {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DelegateStringBool(int senderId, string stringValue, bool boolValue);

}