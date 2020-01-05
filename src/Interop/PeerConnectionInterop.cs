// Copyright (c) Microsoft Corporation.
// Modified by Digital-512.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.MixedReality.WebRTC.Interop
{
    /// <summary>
    /// Handle to a native peer connection object.
    /// </summary>
    internal sealed class PeerConnectionHandle : SafeHandle
    {
        /// <summary>
        /// Check if the current handle is invalid, which means it is not referencing
        /// an actual native object. Note that a valid handle only means that the internal
        /// handle references a native object, but does not guarantee that the native
        /// object is still accessible. It is only safe to access the native object if
        /// the handle is not closed, which implies it being valid.
        /// </summary>
        public override bool IsInvalid
        {
            get
            {
                return (handle == IntPtr.Zero);
            }
        }

        /// <summary>
        /// Default constructor for an invalid handle.
        /// </summary>
        public PeerConnectionHandle() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        /// <summary>
        /// Constructor for a valid handle referencing the given native object.
        /// </summary>
        /// <param name="handle">The valid internal handle to the native object.</param>
        public PeerConnectionHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Release the native object while the handle is being closed.
        /// </summary>
        /// <returns>Return <c>true</c> if the native object was successfully released.</returns>
        protected override bool ReleaseHandle()
        {
            PeerConnectionInterop.PeerConnection_RemoveRef(handle);
            return true;
        }
    }

    internal class PeerConnectionInterop
    {
        // Types of trampolines for MonoPInvokeCallback
        private delegate void ConnectedDelegate(IntPtr peer);
        private delegate void DataChannelCreateObjectDelegate(IntPtr peer, DataChannelInterop.CreateConfig config,
            out DataChannelInterop.Callbacks callbacks);
        private delegate void DataChannelAddedDelegate(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle);
        private delegate void DataChannelRemovedDelegate(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle);
        private delegate void LocalSdpReadytoSendDelegate(IntPtr peer, string type, string sdp);
        private delegate void IceCandidateReadytoSendDelegate(IntPtr peer, string candidate, int sdpMlineindex, string sdpMid);
        private delegate void IceStateChangedDelegate(IntPtr peer, IceConnectionState newState);
        private delegate void IceGatheringStateChangedDelegate(IntPtr peer, IceGatheringState newState);
        private delegate void RenegotiationNeededDelegate(IntPtr peer);
        private delegate void TrackAddedDelegate(IntPtr peer, PeerConnection.TrackKind trackKind);
        private delegate void TrackRemovedDelegate(IntPtr peer, PeerConnection.TrackKind trackKind);
        private delegate void DataChannelMessageDelegate(IntPtr peer, IntPtr data, ulong size);
        private delegate void DataChannelBufferingDelegate(IntPtr peer, ulong previous, ulong current, ulong limit);
        private delegate void DataChannelStateDelegate(IntPtr peer, int state, int id);

        /// <summary>
        /// Utility to lock all low-level interop delegates registered with the native plugin for the duration
        /// of the peer connection wrapper lifetime, and prevent their garbage collection.
        /// </summary>
        /// <remarks>
        /// The delegate don't need to be pinned, just referenced to prevent garbage collection.
        /// So referencing them from this class is enough to keep them alive and usable.
        /// </remarks>
        public class InteropCallbacks
        {
            public PeerConnection Peer;
            public DataChannelInterop.CreateObjectCallback DataChannelCreateObjectCallback;
        }

        /// <summary>
        /// Utility to lock all optional delegates registered with the native plugin for the duration
        /// of the peer connection wrapper lifetime, and prevent their garbage collection.
        /// </summary>
        /// <remarks>
        /// The delegate don't need to be pinned, just referenced to prevent garbage collection.
        /// So referencing them from this class is enough to keep them alive and usable.
        /// </remarks>
        public class PeerCallbackArgs
        {
            public PeerConnection Peer;
            public PeerConnectionDataChannelAddedCallback DataChannelAddedCallback;
            public PeerConnectionDataChannelRemovedCallback DataChannelRemovedCallback;
            public PeerConnectionConnectedCallback ConnectedCallback;
            public PeerConnectionLocalSdpReadytoSendCallback LocalSdpReadytoSendCallback;
            public PeerConnectionIceCandidateReadytoSendCallback IceCandidateReadytoSendCallback;
            public PeerConnectionIceStateChangedCallback IceStateChangedCallback;
            public PeerConnectionIceGatheringStateChangedCallback IceGatheringStateChangedCallback;
            public PeerConnectionRenegotiationNeededCallback RenegotiationNeededCallback;
            public PeerConnectionTrackAddedCallback TrackAddedCallback;
            public PeerConnectionTrackRemovedCallback TrackRemovedCallback;
        }

        [MonoPInvokeCallback(typeof(ConnectedDelegate))]
        public static void ConnectedCallback(IntPtr userData)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnConnected();
        }

        [MonoPInvokeCallback(typeof(DataChannelAddedDelegate))]
        public static void DataChannelAddedCallback(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle)
        {
            var peerWrapper = Utils.ToWrapper<PeerConnection>(peer);
            var dataChannelWrapper = Utils.ToWrapper<DataChannel>(dataChannel);
            // Ensure that the DataChannel wrapper knows about its native object.
            // This is not always the case, if created via the interop constructor,
            // as the wrapper is created before the native object exists.
            DataChannelInterop.SetHandle(dataChannelWrapper, dataChannelHandle);
            peerWrapper.OnDataChannelAdded(dataChannelWrapper);
        }

        [MonoPInvokeCallback(typeof(DataChannelRemovedDelegate))]
        public static void DataChannelRemovedCallback(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle)
        {
            var peerWrapper = Utils.ToWrapper<PeerConnection>(peer);
            var dataChannelWrapper = Utils.ToWrapper<DataChannel>(dataChannel);
            peerWrapper.OnDataChannelRemoved(dataChannelWrapper);
        }

        [MonoPInvokeCallback(typeof(LocalSdpReadytoSendDelegate))]
        public static void LocalSdpReadytoSendCallback(IntPtr userData, string type, string sdp)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnLocalSdpReadytoSend(type, sdp);
        }

        [MonoPInvokeCallback(typeof(IceCandidateReadytoSendDelegate))]
        public static void IceCandidateReadytoSendCallback(IntPtr userData, string candidate, int sdpMlineindex, string sdpMid)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnIceCandidateReadytoSend(candidate, sdpMlineindex, sdpMid);
        }

        [MonoPInvokeCallback(typeof(IceStateChangedDelegate))]
        public static void IceStateChangedCallback(IntPtr userData, IceConnectionState newState)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnIceStateChanged(newState);
        }

        [MonoPInvokeCallback(typeof(IceGatheringStateChangedDelegate))]
        public static void IceGatheringStateChangedCallback(IntPtr userData, IceGatheringState newState)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnIceGatheringStateChanged(newState);
        }

        [MonoPInvokeCallback(typeof(RenegotiationNeededDelegate))]
        public static void RenegotiationNeededCallback(IntPtr userData)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnRenegotiationNeeded();
        }

        [MonoPInvokeCallback(typeof(TrackAddedDelegate))]
        public static void TrackAddedCallback(IntPtr userData, PeerConnection.TrackKind trackKind)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnTrackAdded(trackKind);
        }

        [MonoPInvokeCallback(typeof(TrackRemovedDelegate))]
        public static void TrackRemovedCallback(IntPtr userData, PeerConnection.TrackKind trackKind)
        {
            var peer = Utils.ToWrapper<PeerConnection>(userData);
            peer.OnTrackRemoved(trackKind);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct MarshaledInteropCallbacks
        {
            public DataChannelInterop.CreateObjectCallback DataChannelCreateObjectCallback;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct PeerConnectionConfiguration
        {
            public string EncodedIceServers;
            public IceTransportType IceTransportType;
            public BundlePolicy BundlePolicy;
            public SdpSemantic SdpSemantic;
        }


        #region Unmanaged delegates

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionDataChannelAddedCallback(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionDataChannelRemovedCallback(IntPtr peer, IntPtr dataChannel, IntPtr dataChannelHandle);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionInteropCallbacks(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionConnectedCallback(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionLocalSdpReadytoSendCallback(IntPtr userData,
            string type, string sdp);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionIceCandidateReadytoSendCallback(IntPtr userData,
            string candidate, int sdpMlineindex, string sdpMid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionIceStateChangedCallback(IntPtr userData,
            IceConnectionState newState);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionIceGatheringStateChangedCallback(IntPtr userData,
            IceGatheringState newState);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionRenegotiationNeededCallback(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionTrackAddedCallback(IntPtr userData, PeerConnection.TrackKind trackKind);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate void PeerConnectionTrackRemovedCallback(IntPtr userData, PeerConnection.TrackKind trackKind);

        #endregion


        #region P/Invoke static functions

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionAddRef")]
        public static extern void PeerConnection_AddRef(PeerConnectionHandle handle);

        // Note - This is used during SafeHandle.ReleaseHandle(), so cannot use PeerConnectionHandle
        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRemoveRef")]
        public static extern void PeerConnection_RemoveRef(IntPtr handle);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionCreate")]
        public static extern uint PeerConnection_Create(PeerConnectionConfiguration config, IntPtr peer,
            out PeerConnectionHandle peerHandleOut);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterInteropCallbacks")]
        public static extern void PeerConnection_RegisterInteropCallbacks(PeerConnectionHandle peerHandle,
            in MarshaledInteropCallbacks callback);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterConnectedCallback")]
        public static extern void PeerConnection_RegisterConnectedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionConnectedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterLocalSdpReadytoSendCallback")]
        public static extern void PeerConnection_RegisterLocalSdpReadytoSendCallback(PeerConnectionHandle peerHandle,
            PeerConnectionLocalSdpReadytoSendCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterIceCandidateReadytoSendCallback")]
        public static extern void PeerConnection_RegisterIceCandidateReadytoSendCallback(PeerConnectionHandle peerHandle,
            PeerConnectionIceCandidateReadytoSendCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterIceStateChangedCallback")]
        public static extern void PeerConnection_RegisterIceStateChangedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionIceStateChangedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterIceGatheringStateChangedCallback")]
        public static extern void PeerConnection_RegisterIceGatheringStateChangedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionIceGatheringStateChangedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterRenegotiationNeededCallback")]
        public static extern void PeerConnection_RegisterRenegotiationNeededCallback(PeerConnectionHandle peerHandle,
            PeerConnectionRenegotiationNeededCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterTrackAddedCallback")]
        public static extern void PeerConnection_RegisterTrackAddedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionTrackAddedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterTrackRemovedCallback")]
        public static extern void PeerConnection_RegisterTrackRemovedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionTrackRemovedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterDataChannelAddedCallback")]
        public static extern void PeerConnection_RegisterDataChannelAddedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionDataChannelAddedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRegisterDataChannelRemovedCallback")]
        public static extern void PeerConnection_RegisterDataChannelRemovedCallback(PeerConnectionHandle peerHandle,
            PeerConnectionDataChannelRemovedCallback callback, IntPtr userData);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionAddDataChannel")]
        public static extern uint PeerConnection_AddDataChannel(PeerConnectionHandle peerHandle, IntPtr dataChannel,
            DataChannelInterop.CreateConfig config, DataChannelInterop.Callbacks callbacks,
            ref IntPtr dataChannelHandle);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionRemoveDataChannel")]
        public static extern uint PeerConnection_RemoveDataChannel(PeerConnectionHandle peerHandle, IntPtr dataChannelHandle);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionAddIceCandidate")]
        public static extern void PeerConnection_AddIceCandidate(PeerConnectionHandle peerHandle, string sdpMid,
            int sdpMlineindex, string candidate);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionCreateOffer")]
        public static extern uint PeerConnection_CreateOffer(PeerConnectionHandle peerHandle);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionCreateAnswer")]
        public static extern uint PeerConnection_CreateAnswer(PeerConnectionHandle peerHandle);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionSetRemoteDescription")]
        public static extern uint PeerConnection_SetRemoteDescription(PeerConnectionHandle peerHandle, string type, string sdp);

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsPeerConnectionClose")]
        public static extern uint PeerConnection_Close(PeerConnectionHandle peerHandle);

        #endregion
    }
}