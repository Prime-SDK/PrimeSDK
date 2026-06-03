using System.Runtime.InteropServices;

namespace PrimeGames.SDK.Common {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DelegateInt(int senderId, int value);

}