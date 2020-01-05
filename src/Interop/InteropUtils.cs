// Copyright (c) Microsoft Corporation.
// Modified by Digital-512.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.MixedReality.WebRTC.Interop
{
    /// <summary>
    /// Interop boolean.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct mrsBool
    {
        public static readonly mrsBool True = new mrsBool(true);
        public static readonly mrsBool False = new mrsBool(false);
        private int _value;
        public mrsBool(bool value) { _value = (value ? -1 : 0); }
        public static explicit operator mrsBool(bool b) { return (b ? True : False); }
        public static explicit operator bool(mrsBool b) { return (b._value != 0); }
    }

    /// <summary>
    /// Attribute to decorate managed delegates used as native callbacks (reverse P/Invoke).
    /// Required by Mono in Ahead-Of-Time (AOT) compiling, and Unity with the IL2CPP backend.
    /// </summary>
    ///
    /// This attribute is required by Mono AOT and Unity IL2CPP, but not by .NET Core or Framework.
    /// The implementation was copied from the Mono source code (https://github.com/mono/mono).
    /// The type argument does not seem to be used anywhere in the code, and a stub implementation
    /// like this seems to be enough for IL2CPP to be able to marshal the delegate (untested on Mono).
    [AttributeUsage(AttributeTargets.Method)]
    sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type t) { }
    }

    internal static class Utils
    {
#if __IOS__ || UNITY_IOS && !UNITY_EDITOR
        internal const string dllPath = "__Internal";
#else
        internal const string dllPath = "Microsoft.MixedReality.WebRTC.Native.dll";
#endif

        // Error codes returned by the interop API -- see mrs_errors.h
        internal const uint MRS_SUCCESS = 0u;
        internal const uint MRS_E_UNKNOWN = 0x80000000u;
        internal const uint MRS_E_INVALID_PARAMETER = 0x80000001u;
        internal const uint MRS_E_INVALID_OPERATION = 0x80000002u;
        internal const uint MRS_E_WRONG_THREAD = 0x80000003u;
        internal const uint MRS_E_NOTFOUND = 0x80000004u;
        internal const uint MRS_E_INVALID_NATIVE_HANDLE = 0x80000005u;
        internal const uint MRS_E_NOT_INITIALIZED = 0x80000006u;
        internal const uint MRS_E_UNSUPPORTED = 0x80000007u;
        internal const uint MRS_E_OUT_OF_RANGE = 0x80000008u;
        internal const uint MRS_E_PEER_CONNECTION_CLOSED = 0x80000101u;
        internal const uint MRS_E_SCTP_NOT_NEGOTIATED = 0x80000301u;
        internal const uint MRS_E_INVALID_DATA_CHANNEL_ID = 0x80000302u;

        public static IntPtr MakeWrapperRef(object obj)
        {
            var handle = GCHandle.Alloc(obj, GCHandleType.Normal);
            var arg = GCHandle.ToIntPtr(handle);
            return arg;
        }

        public static T ToWrapper<T>(IntPtr peer) where T : class
        {
            var handle = GCHandle.FromIntPtr(peer);
            var wrapper = (handle.Target as T);
            return wrapper;
        }

        public static void ReleaseWrapperRef(IntPtr wrapperRef)
        {
            var handle = GCHandle.FromIntPtr(wrapperRef);
            handle.Free();
        }

        /// <summary>
        /// Helper to throw an exception based on an error code.
        /// </summary>
        /// <param name="res">The error code to turn into an exception, if not zero (MRS_SUCCESS).</param>
        public static void ThrowOnErrorCode(uint res)
        {
            if (res == MRS_SUCCESS)
            {
                return;
            }

            switch (res)
            {
                case MRS_E_UNKNOWN:
                default:
                    throw new Exception();

                case MRS_E_INVALID_PARAMETER:
                    throw new ArgumentException();

                case MRS_E_INVALID_OPERATION:
                    throw new InvalidOperationException();

                case MRS_E_WRONG_THREAD:
                    throw new InvalidOperationException("This method cannot be called on that thread.");

                case MRS_E_NOTFOUND:
                    throw new Exception("Object not found.");

                case MRS_E_INVALID_NATIVE_HANDLE:
                    throw new InvalidInteropNativeHandleException();

                case MRS_E_NOT_INITIALIZED:
                    throw new InvalidOperationException("Object not initialized.");

                case MRS_E_UNSUPPORTED:
                    throw new NotSupportedException();

                case MRS_E_OUT_OF_RANGE:
                    throw new ArgumentOutOfRangeException();

                case MRS_E_SCTP_NOT_NEGOTIATED:
                    throw new SctpNotNegotiatedException();

                case MRS_E_PEER_CONNECTION_CLOSED:
                    throw new InvalidOperationException("The operation cannot complete because the peer connection was closed.");

                case MRS_E_INVALID_DATA_CHANNEL_ID:
                    throw new ArgumentOutOfRangeException("Invalid ID passed to AddDataChannelAsync().");
            }
        }
    }
}