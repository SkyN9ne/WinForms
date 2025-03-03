﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Interop;

namespace System.Drawing;

/// <summary>
///  The ComWrappers implementation for System.Drawing.Common's COM interop usages.
/// </summary>
/// <remarks>
///  <para>
///   Supports IStream and IPicture COM interfaces.
///  </para>
/// </remarks>
internal unsafe partial class DrawingCom : ComWrappers
{
    private const int S_OK = (int)HRESULT.S_OK;
    private static readonly Guid IID_IStream = new(0x0000000C, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

    private static readonly ComInterfaceEntry* s_wrapperEntry = InitializeComInterfaceEntry();

    internal static DrawingCom Instance { get; } = new DrawingCom();

    private DrawingCom() { }

    private static ComInterfaceEntry* InitializeComInterfaceEntry()
    {
        GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

        IntPtr iStreamVtbl = IStreamVtbl.Create(fpQueryInterface, fpAddRef, fpRelease);

        ComInterfaceEntry* wrapperEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DrawingCom), sizeof(ComInterfaceEntry));
        wrapperEntry->IID = IID_IStream;
        wrapperEntry->Vtable = iStreamVtbl;
        return wrapperEntry;
    }

    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        Debug.Assert(obj is Ole32.IStream);
        Debug.Assert(s_wrapperEntry != null);

        // Always return the same table mappings.
        count = 1;
        return s_wrapperEntry;
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

        Guid pictureIID = IPicture.IID;
#if NET8_0_OR_GREATER
        int hr = Marshal.QueryInterface(externalComObject, in pictureIID, out IntPtr comObject);
#else
        int hr = Marshal.QueryInterface(externalComObject, ref pictureIID, out IntPtr comObject);
#endif
        if (hr == S_OK)
        {
            return new PictureWrapper(comObject);
        }

        throw new NotImplementedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }

    internal static IStreamWrapper GetComWrapper(Ole32.IStream stream)
    {
        IntPtr streamWrapperPtr = Instance.GetOrCreateComInterfaceForObject(stream, CreateComInterfaceFlags.None);

        Guid streamIID = IID_IStream;

#if NET8_0_OR_GREATER
        int hr = Marshal.QueryInterface(streamWrapperPtr, in streamIID, out IntPtr streamPtr);
#else
        int hr = Marshal.QueryInterface(streamWrapperPtr, ref streamIID, out IntPtr streamPtr);
#endif

        Marshal.Release(streamWrapperPtr);

        ThrowExceptionForHR(hr);

        return new IStreamWrapper(streamPtr);
    }

    internal static void ThrowExceptionForHR(int errorCode)
    {
        // Pass -1 for errorInfo to indicate that Windows' GetErrorInfo shouldn't be called, and only
        // throw the Exception corresponding to the specified errorCode.
        Marshal.ThrowExceptionForHR(errorCode, errorInfo: new IntPtr(-1));
    }

    internal static class IStreamVtbl
    {
        public static IntPtr Create(IntPtr fpQueryInterface, IntPtr fpAddRef, IntPtr fpRelease)
        {
            IntPtr* vtblRaw = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), IntPtr.Size * 14);
            vtblRaw[0] = fpQueryInterface;
            vtblRaw[1] = fpAddRef;
            vtblRaw[2] = fpRelease;
            vtblRaw[3] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)&Read;
            vtblRaw[4] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)&Write;
            vtblRaw[5] = (IntPtr)(delegate* unmanaged<IntPtr, long, SeekOrigin, ulong*, int>)&Seek;
            vtblRaw[6] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, int>)&SetSize;
            vtblRaw[7] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, ulong, ulong*, ulong*, int>)&CopyTo;
            vtblRaw[8] = (IntPtr)(delegate* unmanaged<IntPtr, uint, int>)&Commit;
            vtblRaw[9] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&Revert;
            vtblRaw[10] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, ulong, uint, int>)&LockRegion;
            vtblRaw[11] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, ulong, uint, int>)&UnlockRegion;
            vtblRaw[12] = (IntPtr)(delegate* unmanaged<IntPtr, Ole32.STATSTG*, Ole32.STATFLAG, int>)&Stat;
            vtblRaw[13] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr*, int>)&Clone;

            return (IntPtr)vtblRaw;
        }

        [UnmanagedCallersOnly]
        private static int Read(IntPtr thisPtr, byte* pv, uint cb, uint* pcbRead)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Read(pv, cb, pcbRead);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int Write(IntPtr thisPtr, byte* pv, uint cb, uint* pcbWritten)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Write(pv, cb, pcbWritten);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int Seek(IntPtr thisPtr, long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Seek(dlibMove, dwOrigin, plibNewPosition);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int SetSize(IntPtr thisPtr, ulong libNewSize)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.SetSize(libNewSize);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int CopyTo(IntPtr thisPtr, IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);

                return (int)inst.CopyTo(pstm, cb, pcbRead, pcbWritten);
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }

        [UnmanagedCallersOnly]
        private static int Commit(IntPtr thisPtr, uint grfCommitFlags)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Commit(grfCommitFlags);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int Revert(IntPtr thisPtr)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Revert();
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int LockRegion(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                return (int)inst.LockRegion(libOffset, cb, dwLockType);
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }

        [UnmanagedCallersOnly]
        private static int UnlockRegion(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                return (int)inst.UnlockRegion(libOffset, cb, dwLockType);
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }

        [UnmanagedCallersOnly]
        private static int Stat(IntPtr thisPtr, Ole32.STATSTG* pstatstg, Ole32.STATFLAG grfStatFlag)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Stat(pstatstg, grfStatFlag);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return S_OK;
        }

        [UnmanagedCallersOnly]
        private static int Clone(IntPtr thisPtr, IntPtr* ppstm)
        {
            try
            {
                Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Ole32.IStream>((ComInterfaceDispatch*)thisPtr);

                return (int)inst.Clone(ppstm);
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }
    }

    internal interface IPicture : IDisposable
    {
        static readonly Guid IID = new(0x7BF80980, 0xBF32, 0x101A, 0x8B, 0xBB, 0, 0xAA, 0x00, 0x30, 0x0C, 0xAB);

        // NOTE: Only SaveAsFile is invoked. The other methods on IPicture are not necessary

        int SaveAsFile(IntPtr pstm, int fSaveMemCopy, int* pcbSize);
    }

    private sealed class PictureWrapper : IPicture
    {
        private readonly IntPtr _wrappedInstance;

        public PictureWrapper(IntPtr wrappedInstance)
        {
            _wrappedInstance = wrappedInstance;
        }

        public void Dispose()
        {
            Marshal.Release(_wrappedInstance);
        }

        public unsafe int SaveAsFile(IntPtr pstm, int fSaveMemCopy, int* pcbSize)
        {
            // Get the IStream implementation, since the ComWrappers runtime returns a pointer to the IUnknown interface implementation
            Guid streamIID = IID_IStream;

#if NET8_0_OR_GREATER
            ThrowExceptionForHR(Marshal.QueryInterface(pstm, in streamIID, out IntPtr pstmImpl));
#else
            ThrowExceptionForHR(Marshal.QueryInterface(pstm, ref streamIID, out IntPtr pstmImpl));
#endif

            try
            {
                return ((delegate* unmanaged<IntPtr, IntPtr, int, int*, int>)(*(*(void***)_wrappedInstance + 15 /* IPicture.SaveAsFile slot */)))
                    (_wrappedInstance, pstmImpl, fSaveMemCopy, pcbSize);
            }
            finally
            {
                Marshal.Release(pstmImpl);
            }
        }
    }
}
