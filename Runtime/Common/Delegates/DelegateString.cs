using System.Runtime.InteropServices;

namespace PrimeGames.SDK.Common {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DelegateString(int senderId, string value);

}