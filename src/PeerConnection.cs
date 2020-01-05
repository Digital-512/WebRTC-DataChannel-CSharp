// Copyright (c) Microsoft Corporation.
// Modified by Digital-512.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC.Interop;

namespace Microsoft.MixedReality.WebRTC
{
    /// <summary>
    /// Type of ICE candidates offered to the remote peer.
    /// </summary>
    public enum IceTransportType : int
    {
        /// <summary>
        /// No ICE candidate offered.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only advertize relay-type candidates, like TURN servers, to avoid leaking the IP address of the client.
        /// </summary>
        Relay = 1,

        /// ?
        NoHost = 2,

        /// <summary>
        /// Offer all types of ICE candidates.
        /// </summary>
        All = 3
    }

    /// <summary>
    /// Bundle policy.
    /// See https://www.w3.org/TR/webrtc/#rtcbundlepolicy-enum.
    /// </summary>
    public enum BundlePolicy : int
    {
        /// <summary>
        /// Gather ICE candidates for each media type in use (audio, video, and data). If the remote endpoint is
        /// not bundle-aware, negotiate only one audio and video track on separate transports.
        /// </summary>
        Balanced = 0,

        /// <summary>
        /// Gather ICE candidates for only one track. If the remote endpoint is not bundle-aware, negotiate only
        /// one media track.
        /// </summary>
        MaxBundle = 1,

        /// <summary>
        /// Gather ICE candidates for each track. If the remote endpoint is not bundle-aware, negotiate all media
        /// tracks on separate transports.
        /// </summary>
        MaxCompat = 2
    }

    /// <summary>
    /// SDP semantic used for (re)negotiating a peer connection.
    /// </summary>
    public enum SdpSemantic : int
    {
        /// <summary>
        /// Unified plan, as standardized in the WebRTC 1.0 standard.
        /// </summary>
        UnifiedPlan = 0,

        /// <summary>
        /// Legacy Plan B, deprecated and soon removed.
        /// Only available for compatiblity with older implementations if needed.
        /// Do not use unless there is a problem with the Unified Plan.
        /// </summary>
        PlanB = 1
    }

    /// <summary>
    /// ICE server configuration (STUN and/or TURN).
    /// </summary>
    public class IceServer
    {
        /// <summary>
        /// List of TURN and/or STUN server URLs to use for NAT bypass, in order of preference.
        ///
        /// The scheme is defined in the core WebRTC implementation, and is in short:
        /// stunURI     = stunScheme ":" stun-host [ ":" stun-port ]
        /// stunScheme  = "stun" / "stuns"
        /// turnURI     = turnScheme ":" turn-host [ ":" turn-port ] [ "?transport=" transport ]
        /// turnScheme  = "turn" / "turns"
        /// </summary>
        public List<string> Urls = new List<string>();

        /// <summary>
        /// Optional TURN server username.
        /// </summary>
        public string TurnUserName = string.Empty;

        /// <summary>
        /// Optional TURN server credentials.
        /// </summary>
        public string TurnPassword = string.Empty;

        /// <summary>
        /// Format the ICE server data according to the encoded marshalling of the C++ API.
        /// </summary>
        /// <returns>The encoded string of ICE servers.</returns>
        public override string ToString()
        {
            if (Urls == null)
            {
                return string.Empty;
            }
            string ret = string.Join("\n", Urls);
            if (!string.IsNullOrEmpty(TurnUserName))
            {
                ret += $"\nusername:{TurnUserName}";
                if (!string.IsNullOrEmpty(TurnPassword))
                {
                    ret += $"\npassword:{TurnPassword}";
                }
            }
            return ret;
        }
    }

    /// <summary>
    /// Configuration to initialize a <see cref="PeerConnection"/>.
    /// </summary>
    public class PeerConnectionConfiguration
    {
        /// <summary>
        /// List of TURN and/or STUN servers to use for NAT bypass, in order of preference.
        /// </summary>
        public List<IceServer> IceServers = new List<IceServer>();

        /// <summary>
        /// ICE transport policy for the connection.
        /// </summary>
        public IceTransportType IceTransportType = IceTransportType.All;

        /// <summary>
        /// Bundle policy for the connection.
        /// </summary>
        public BundlePolicy BundlePolicy = BundlePolicy.Balanced;

        /// <summary>
        /// SDP semantic for the connection.
        /// </summary>
        /// <remarks>Plan B is deprecated, do not use it.</remarks>
        public SdpSemantic SdpSemantic = SdpSemantic.UnifiedPlan;
    }

    /// <summary>
    /// State of an ICE connection.
    /// </summary>
    /// <remarks>
    /// Due to the underlying implementation, this is currently a mix of the
    /// <see href="https://www.w3.org/TR/webrtc/#rtcicegatheringstate-enum">RTPIceGatheringState</see>
    /// and the <see href="https://www.w3.org/TR/webrtc/#rtcpeerconnectionstate-enum">RTPPeerConnectionState</see>
    /// from the WebRTC 1.0 standard.
    /// </remarks>
    /// <seealso href="https://www.w3.org/TR/webrtc/#rtcicegatheringstate-enum"/>
    /// <seealso href="https://www.w3.org/TR/webrtc/#rtcpeerconnectionstate-enum"/>
    public enum IceConnectionState : int
    {
        /// <summary>
        /// Newly created ICE connection. This is the starting state.
        /// </summary>
        New = 0,

        /// <summary>
        /// ICE connection received an offer, but transports are not writable yet.
        /// </summary>
        Checking = 1,

        /// <summary>
        /// Transports are writable.
        /// </summary>
        Connected = 2,

        /// <summary>
        /// ICE connection finished establishing.
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Failed establishing an ICE connection.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// ICE connection is disconnected, there is no more writable transport.
        /// </summary>
        Disconnected = 5,

        /// <summary>
        /// The peer connection was closed entirely.
        /// </summary>
        Closed = 6,
    }

    /// <summary>
    /// State of an ICE gathering process.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webrtc/#rtcicegatheringstate-enum">RTPIceGatheringState</see>
    /// from the WebRTC 1.0 standard.
    /// </remarks>
    /// <seealso href="https://www.w3.org/TR/webrtc/#rtcicegatheringstate-enum"/>
    public enum IceGatheringState : int
    {
        /// <summary>
        /// There is no ICE transport, or none of them started gathering ICE candidates.
        /// </summary>
        New = 0,

        /// <summary>
        /// The gathering process started. At least one ICE transport is active and gathering
        /// some ICE candidates.
        /// </summary>
        Gathering = 1,

        /// <summary>
        /// The gathering process is complete. At least one ICE transport was active, and
        /// all transports finished gathering ICE candidates.
        /// </summary>
        Complete = 2,
    }

    /// <summary>
    /// The WebRTC peer connection object is the entry point to using WebRTC.
    /// </summary>
    public class PeerConnection : IDisposable
    {
        /// <summary>
        /// Delegate for <see cref="DataChannelAdded"/> event.
        /// </summary>
        /// <param name="channel">The newly added data channel.</param>
        public delegate void DataChannelAddedDelegate(DataChannel channel);

        /// <summary>
        /// Delegate for <see cref="DataChannelRemoved"/> event.
        /// </summary>
        /// <param name="channel">The data channel just removed.</param>
        public delegate void DataChannelRemovedDelegate(DataChannel channel);

        /// <summary>
        /// Delegate for <see cref="LocalSdpReadytoSend"/> event.
        /// </summary>
        /// <param name="type">SDP message type, one of "offer", "answer", or "ice".</param>
        /// <param name="sdp">Raw SDP message content.</param>
        public delegate void LocalSdpReadyToSendDelegate(string type, string sdp);

        /// <summary>
        /// Delegate for the <see cref="IceCandidateReadytoSend"/> event.
        /// </summary>
        /// <param name="candidate">Raw SDP message describing the ICE candidate.</param>
        /// <param name="sdpMlineindex">Index of the m= line.</param>
        /// <param name="sdpMid">Media identifier</param>
        public delegate void IceCandidateReadytoSendDelegate(string candidate, int sdpMlineindex, string sdpMid);

        /// <summary>
        /// Delegate for the <see cref="IceStateChanged"/> event.
        /// </summary>
        /// <param name="newState">The new ICE connection state.</param>
        public delegate void IceStateChangedDelegate(IceConnectionState newState);

        /// <summary>
        /// Delegate for the <see cref="IceGatheringStateChanged"/> event.
        /// </summary>
        /// <param name="newState">The new ICE gathering state.</param>
        public delegate void IceGatheringStateChangedDelegate(IceGatheringState newState);

        /// <summary>
        /// Kind of WebRTC track.
        /// </summary>
        public enum TrackKind : uint
        {
            /// <summary>
            /// Unknown track kind. Generally not initialized or error.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Audio track.
            /// </summary>
            Audio = 1,

            /// <summary>
            /// Video track.
            /// </summary>
            Video = 2,

            /// <summary>
            /// Data track.
            /// </summary>
            Data = 3
        };

        /// <summary>
        /// Boolean property indicating whether the peer connection has been initialized.
        /// </summary>
        public bool Initialized
        {
            get
            {
                lock (_openCloseLock)
                {
                    return (_initTask != null);
                }
            }
        }

        /// <summary>
        /// Indicates whether the peer connection is established and can exchange some
        /// track content (audio/video/data) with the remote peer.
        /// </summary>
        /// <remarks>
        /// This does not indicate whether the ICE exchange is done, as it
        /// may continue after the peer connection negotiated a first session.
        /// For ICE connection status, see the <see cref="IceStateChanged"/> event.
        /// </remarks>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Event fired when a connection is established.
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// Event fired when a data channel is added to the peer connection.
        /// This event is always fired, whether the data channel is created by the local peer
        /// or the remote peer, and is negotiated (out-of-band) or not (in-band).
        /// If an in-band data channel is created by the local peer, the <see cref="DataChannel.ID"/>
        /// field is not yet available when this event is fired, because the ID has not been
        /// agreed upon with the remote peer yet.
        /// </summary>
        public event DataChannelAddedDelegate DataChannelAdded;

        /// <summary>
        /// Event fired when a data channel is removed from the peer connection.
        /// This event is always fired, whatever its creation method (negotiated or not)
        /// and original creator (local or remote peer).
        /// </summary>
        public event DataChannelRemovedDelegate DataChannelRemoved;

        /// <summary>
        /// Event that occurs when a local SDP message is ready to be transmitted.
        /// </summary>
        public event LocalSdpReadyToSendDelegate LocalSdpReadytoSend;

        /// <summary>
        /// Event that occurs when a local ICE candidate is ready to be transmitted.
        /// </summary>
        public event IceCandidateReadytoSendDelegate IceCandidateReadytoSend;

        /// <summary>
        /// Event that occurs when the state of the ICE connection changed.
        /// </summary>
        public event IceStateChangedDelegate IceStateChanged;

        /// <summary>
        /// Event that occurs when the state of the ICE gathering changed.
        /// </summary>
        public event IceGatheringStateChangedDelegate IceGatheringStateChanged;

        /// <summary>
        /// Event that occurs when a renegotiation of the session is needed.
        /// This generally occurs as a result of adding or removing tracks,
        /// and the user should call <see cref="CreateOffer"/> to actually
        /// start a renegotiation.
        /// </summary>
        public event Action RenegotiationNeeded;

        /// <summary>
        /// Event that occurs when a remote track is added to the current connection.
        /// </summary>
        public event Action<TrackKind> TrackAdded;

        /// <summary>
        /// Event that occurs when a remote track is removed from the current connection.
        /// </summary>
        public event Action<TrackKind> TrackRemoved;

        /// <summary>
        /// GCHandle to self for the various native callbacks.
        /// This also acts as a marker of a connection created or in the process of being created.
        /// </summary>
        private GCHandle _selfHandle;

        /// <summary>
        /// Handle to the native PeerConnection object.
        /// </summary>
        /// <remarks>
        /// In native land this is a <code>Microsoft::MixedReality::WebRTC::PeerConnectionHandle</code>.
        /// </remarks>
        private PeerConnectionHandle _nativePeerhandle = new PeerConnectionHandle();

        /// <summary>
        /// Initialization task returned by <see cref="InitializeAsync"/>.
        /// </summary>
        private Task _initTask = null;

        /// <summary>
        /// Boolean to indicate if <see cref="Close"/> has been called and is waiting for a pending
        /// initializing task <see cref="_initTask"/> to complete or cancel.
        /// </summary>
        private bool _isClosing = false;

        /// <summary>
        /// Lock for asynchronous opening and closing of the connection, protecting
        /// changes to <see cref="_nativePeerhandle"/>, <see cref="_selfHandle"/>,
        /// <see cref="_initTask"/>, and <see cref="_isClosing"/>.
        /// </summary>
        private object _openCloseLock = new object();

        private PeerConnectionInterop.InteropCallbacks _interopCallbacks;
        private PeerConnectionInterop.PeerCallbackArgs _peerCallbackArgs;


        #region Initializing and shutdown

        /// <summary>
        /// Create a new peer connection object. The object is initially created empty, and cannot be used
        /// until <see cref="InitializeAsync(PeerConnectionConfiguration, CancellationToken)"/> has completed
        /// successfully.
        /// </summary>
        public PeerConnection()
        {
        }

        /// <summary>
        /// Initialize the current peer connection object asynchronously.
        /// 
        /// Most other methods will fail unless this call completes successfully, as it initializes the
        /// underlying native implementation object required to create and manipulate the peer connection.
        /// 
        /// Once this call asynchronously completed, the <see cref="Initialized"/> property becomes <c>true</c>.
        /// </summary>
        /// <param name="config">Configuration for initializing the peer connection.</param>
        /// <param name="token">Optional cancellation token for the initialize task. This is only used if
        /// the singleton task was created by this call, and not a prior call.</param>
        /// <returns>The singleton task used to initialize the underlying native peer connection.</returns>
        /// <remarks>This method is multi-thread safe, and will always return the same task object
        /// from the first call to it until the peer connection object is deinitialized. This allows
        /// multiple callers to all execute some action following the initialization, without the need
        /// to force a single caller and to synchronize with it.</remarks>
        public Task InitializeAsync(PeerConnectionConfiguration config = null, CancellationToken token = default)
        {
            lock (_openCloseLock)
            {
                // If Close() is waiting for _initTask to finish, do not return it.
                if (_isClosing)
                {
                    throw new OperationCanceledException("A closing operation is pending.");
                }

                // If _initTask has already been created, return it.
                if (_initTask != null)
                {
                    return _initTask;
                }

                // Allocate a GC handle to self for static P/Invoke callbacks to be able to call
                // back into methods of this peer connection object. The handle is released when
                // the peer connection is closed, to allow this object to be destroyed.
                Debug.Assert(!_selfHandle.IsAllocated);
                _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);

                // Create and lock in memory delegates for all the static callback wrappers (see below).
                // This avoids delegates being garbage-collected, since the P/Invoke mechanism by itself
                // does not guarantee their lifetime.
                _interopCallbacks = new PeerConnectionInterop.InteropCallbacks()
                {
                    Peer = this,
                    DataChannelCreateObjectCallback = DataChannelInterop.DataChannelCreateObjectCallback,
                };
                _peerCallbackArgs = new PeerConnectionInterop.PeerCallbackArgs()
                {
                    Peer = this,
                    DataChannelAddedCallback = PeerConnectionInterop.DataChannelAddedCallback,
                    DataChannelRemovedCallback = PeerConnectionInterop.DataChannelRemovedCallback,
                    ConnectedCallback = PeerConnectionInterop.ConnectedCallback,
                    LocalSdpReadytoSendCallback = PeerConnectionInterop.LocalSdpReadytoSendCallback,
                    IceCandidateReadytoSendCallback = PeerConnectionInterop.IceCandidateReadytoSendCallback,
                    IceStateChangedCallback = PeerConnectionInterop.IceStateChangedCallback,
                    IceGatheringStateChangedCallback = PeerConnectionInterop.IceGatheringStateChangedCallback,
                    RenegotiationNeededCallback = PeerConnectionInterop.RenegotiationNeededCallback,
                    TrackAddedCallback = PeerConnectionInterop.TrackAddedCallback,
                    TrackRemovedCallback = PeerConnectionInterop.TrackRemovedCallback
                };

                // Cache values in local variables before starting async task, to avoid any
                // subsequent external change from affecting that task.
                // Also set default values, as the native call doesn't handle NULL.
                PeerConnectionInterop.PeerConnectionConfiguration nativeConfig;
                if (config != null)
                {
                    nativeConfig = new PeerConnectionInterop.PeerConnectionConfiguration
                    {
                        EncodedIceServers = string.Join("\n\n", config.IceServers),
                        IceTransportType = config.IceTransportType,
                        BundlePolicy = config.BundlePolicy,
                        SdpSemantic = config.SdpSemantic,
                    };
                }
                else
                {
                    nativeConfig = new PeerConnectionInterop.PeerConnectionConfiguration();
                }

                // On UWP PeerConnectionCreate() fails on main UI thread, so always initialize the native peer
                // connection asynchronously from a background worker thread.
                _initTask = Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    uint res = PeerConnectionInterop.PeerConnection_Create(nativeConfig, GCHandle.ToIntPtr(_selfHandle), out _nativePeerhandle);

                    lock (_openCloseLock)
                    {
                        // Handle errors
                        if ((res != Utils.MRS_SUCCESS) || _nativePeerhandle.IsInvalid)
                        {
                            if (_selfHandle.IsAllocated)
                            {
                                _interopCallbacks = null;
                                _peerCallbackArgs = null;
                                _selfHandle.Free();
                            }

                            Utils.ThrowOnErrorCode(res);
                            throw new Exception(); // if res == MRS_SUCCESS but handle is NULL
                        }

                        // The connection may have been aborted while being created, either via the
                        // cancellation token, or by calling Close() after the synchronous codepath
                        // above but before this task had a chance to run in the background.
                        if (token.IsCancellationRequested)
                        {
                            // Cancelled by token
                            _nativePeerhandle.Close();
                            throw new OperationCanceledException(token);
                        }
                        if (!_selfHandle.IsAllocated)
                        {
                            // Cancelled by calling Close()
                            _nativePeerhandle.Close();
                            throw new OperationCanceledException();
                        }

                        // Register all trampoline callbacks. Note that even passing a static managed method
                        // for the callback is not safe, because the compiler implicitly creates a delegate
                        // object (a static method is not a delegate itself; it can be wrapped inside one),
                        // and that delegate object will be garbage collected immediately at the end of this
                        // block. Instead, a delegate needs to be explicitly created and locked in memory.
                        // Since the current PeerConnection instance is already locked via _selfHandle,
                        // and it references all delegates via _peerCallbackArgs, those also can't be GC'd.
                        var self = GCHandle.ToIntPtr(_selfHandle);
                        var interopCallbacks = new PeerConnectionInterop.MarshaledInteropCallbacks
                        {
                            DataChannelCreateObjectCallback = _interopCallbacks.DataChannelCreateObjectCallback
                        };
                        PeerConnectionInterop.PeerConnection_RegisterInteropCallbacks(
                            _nativePeerhandle, in interopCallbacks);
                        PeerConnectionInterop.PeerConnection_RegisterConnectedCallback(
                            _nativePeerhandle, _peerCallbackArgs.ConnectedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterLocalSdpReadytoSendCallback(
                            _nativePeerhandle, _peerCallbackArgs.LocalSdpReadytoSendCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterIceCandidateReadytoSendCallback(
                            _nativePeerhandle, _peerCallbackArgs.IceCandidateReadytoSendCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterIceStateChangedCallback(
                            _nativePeerhandle, _peerCallbackArgs.IceStateChangedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterIceGatheringStateChangedCallback(
                            _nativePeerhandle, _peerCallbackArgs.IceGatheringStateChangedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterRenegotiationNeededCallback(
                            _nativePeerhandle, _peerCallbackArgs.RenegotiationNeededCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterTrackAddedCallback(
                            _nativePeerhandle, _peerCallbackArgs.TrackAddedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterTrackRemovedCallback(
                            _nativePeerhandle, _peerCallbackArgs.TrackRemovedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterDataChannelAddedCallback(
                            _nativePeerhandle, _peerCallbackArgs.DataChannelAddedCallback, self);
                        PeerConnectionInterop.PeerConnection_RegisterDataChannelRemovedCallback(
                            _nativePeerhandle, _peerCallbackArgs.DataChannelRemovedCallback, self);
                    }
                }, token);

                return _initTask;
            }
        }

        /// <summary>
        /// Close the peer connection and destroy the underlying native resources.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="Dispose"/>.</remarks>
        /// <seealso cref="Dispose"/>
        public void Close()
        {
            // Begin shutdown sequence
            Task initTask = null;
            lock (_openCloseLock)
            {
                // If the connection is not initialized, return immediately.
                if (_initTask == null)
                {
                    return;
                }

                // Indicate to InitializeAsync() that it should stop returning _initTask
                // or create a new instance of it, even if it is NULL.
                _isClosing = true;

                // Ensure both Initialized and IsConnected return false
                initTask = _initTask;
                _initTask = null; // This marks the Initialized property as false
                IsConnected = false;

                // Unregister all callbacks and free the delegates
                var interopCallbacks = new PeerConnectionInterop.MarshaledInteropCallbacks();
                PeerConnectionInterop.PeerConnection_RegisterInteropCallbacks(
                    _nativePeerhandle, in interopCallbacks);
                PeerConnectionInterop.PeerConnection_RegisterConnectedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterLocalSdpReadytoSendCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterIceCandidateReadytoSendCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterIceStateChangedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterIceGatheringStateChangedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterRenegotiationNeededCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterTrackAddedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterTrackRemovedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterDataChannelAddedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                PeerConnectionInterop.PeerConnection_RegisterDataChannelRemovedCallback(
                    _nativePeerhandle, null, IntPtr.Zero);
                if (_selfHandle.IsAllocated)
                {
                    _interopCallbacks = null;
                    _peerCallbackArgs = null;
                    _selfHandle.Free();
                }
            }

            // Wait for any pending initializing to finish.
            // This must be outside of the lock because the initialization task will
            // eventually need to acquire the lock to complete.
            initTask.Wait();

            // Close the native peer connection, disconnecting from the remote peer if currently connected.
            PeerConnectionInterop.PeerConnection_Close(_nativePeerhandle);

            // Destroy the native peer connection object. This may be delayed if a P/Invoke callback is underway,
            // but will be handled at some point anyway, even if the PeerConnection managed instance is gone.
            _nativePeerhandle.Close();

            // Complete shutdown sequence and re-enable InitializeAsync()
            lock (_openCloseLock)
            {
                _isClosing = false;
            }
        }

        /// <summary>
        /// Dispose of native resources by closing the peer connection.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="Close"/>.</remarks>
        /// <seealso cref="Close"/>
        public void Dispose() => Close();

        #endregion


        #region Data tracks

        /// <summary>
        /// Add a new out-of-band data channel with the given ID.
        ///
        /// A data channel is negotiated out-of-band when the peers agree on an identifier by any mean
        /// not known to WebRTC, and both open a data channel with that ID. The WebRTC will match the
        /// incoming and outgoing pipes by this ID to allow sending and receiving through that channel.
        ///
        /// This requires some external mechanism to agree on an available identifier not otherwise taken
        /// by another channel, and also requires to ensure that both peers explicitly open that channel.
        /// </summary>
        /// <param name="id">The unique data channel identifier to use.</param>
        /// <param name="label">The data channel name.</param>
        /// <param name="ordered">Indicates whether data channel messages are ordered (see
        /// <see cref="DataChannel.Ordered"/>).</param>
        /// <param name="reliable">Indicates whether data channel messages are reliably delivered
        /// (see <see cref="DataChannel.Reliable"/>).</param>
        /// <returns>Returns a task which completes once the data channel is created.</returns>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        /// <exception cref="SctpNotNegotiatedException">SCTP not negotiated. Call <see cref="CreateOffer()"/> first.</exception>
        /// <exception xref="ArgumentOutOfRangeException">Invalid data channel ID, must be in [0:65535].</exception>
        /// <remarks>
        /// Data channels use DTLS over SCTP, which ensure in particular that messages are encrypted. To that end,
        /// while establishing a connection with the remote peer, some specific SCTP handshake must occur. This
        /// handshake is only performed if at least one data channel was added to the peer connection when the
        /// connection starts its negotiation with <see cref="CreateOffer"/>. Therefore, if the user wants to use
        /// a data channel at any point during the lifetime of this peer connection, it is critical to add at least
        /// one data channel before <see cref="CreateOffer"/> is called. Otherwise all calls will fail with an
        /// <see cref="SctpNotNegotiatedException"/> exception.
        /// </remarks>
        public async Task<DataChannel> AddDataChannelAsync(ushort id, string label, bool ordered, bool reliable)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException("id", id, "Data channel ID must be greater than or equal to zero.");
            }
            return await AddDataChannelAsyncImpl(id, label, ordered, reliable);
        }

        /// <summary>
        /// Add a new in-band data channel whose ID will be determined by the implementation.
        ///
        /// A data channel is negotiated in-band when one peer requests its creation to the WebRTC core,
        /// and the implementation negotiates with the remote peer an appropriate ID by sending some
        /// SDP offer message. In that case once accepted the other peer will automatically create the
        /// appropriate data channel on its side with that negotiated ID, and the ID will be returned on
        /// both sides to the user for information.
        ///
        /// Compared to out-of-band messages, this requires exchanging some SDP messages, but avoids having
        /// to determine a common unused ID and having to explicitly open the data channel on both sides.
        /// </summary>
        /// <param name="label">The data channel name.</param>
        /// <param name="ordered">Indicates whether data channel messages are ordered (see
        /// <see cref="DataChannel.Ordered"/>).</param>
        /// <param name="reliable">Indicates whether data channel messages are reliably delivered
        /// (see <see cref="DataChannel.Reliable"/>).</param>
        /// <returns>Returns a task which completes once the data channel is created.</returns>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        /// <exception cref="SctpNotNegotiatedException">SCTP not negotiated. Call <see cref="CreateOffer()"/> first.</exception>
        /// <exception xref="ArgumentOutOfRangeException">Invalid data channel ID, must be in [0:65535].</exception>
        /// <remarks>
        /// See the critical remark about SCTP handshake in <see cref="AddDataChannelAsync(ushort, string, bool, bool)"/>.
        /// </remarks>
        public async Task<DataChannel> AddDataChannelAsync(string label, bool ordered, bool reliable)
        {
            return await AddDataChannelAsyncImpl(-1, label, ordered, reliable);
        }

        /// <summary>
        /// Add a new in-band or out-of-band data channel.
        /// </summary>
        /// <param name="id">Identifier in [0:65535] of the out-of-band data channel, or <c>-1</c> for in-band.</param>
        /// <param name="label">The data channel name.</param>
        /// <param name="ordered">Indicates whether data channel messages are ordered (see
        /// <see cref="DataChannel.Ordered"/>).</param>
        /// <param name="reliable">Indicates whether data channel messages are reliably delivered
        /// (see <see cref="DataChannel.Reliable"/>).</param>
        /// <returns>Returns a task which completes once the data channel is created.</returns>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        /// <exception xref="InvalidOperationException">SCTP not negotiated.</exception>
        /// <exception xref="ArgumentOutOfRangeException">Invalid data channel ID, must be in [0:65535].</exception>
        private async Task<DataChannel> AddDataChannelAsyncImpl(int id, string label, bool ordered, bool reliable)
        {
            // Preconditions
            ThrowIfConnectionNotOpen();

            // Create the wrapper
            var config = new DataChannelInterop.CreateConfig
            {
                id = id,
                label = label,
                flags = (ordered ? 0x1u : 0x0u) | (reliable ? 0x2u : 0x0u)
            };
            DataChannelInterop.Callbacks callbacks;
            var dataChannel = DataChannelInterop.CreateWrapper(this, config, out callbacks);
            if (dataChannel == null)
            {
                return null;
            }

            // Create the native channel
            return await Task.Run(() =>
            {
                IntPtr nativeHandle = IntPtr.Zero;
                var wrapperGCHandle = GCHandle.Alloc(dataChannel, GCHandleType.Normal);
                var wrapperHandle = GCHandle.ToIntPtr(wrapperGCHandle);
                uint res = PeerConnectionInterop.PeerConnection_AddDataChannel(_nativePeerhandle, wrapperHandle, config, callbacks, ref nativeHandle);
                if (res == Utils.MRS_SUCCESS)
                {
                    DataChannelInterop.SetHandle(dataChannel, nativeHandle);
                    return dataChannel;
                }

                // Some error occurred, callbacks are not registered, so remove the GC lock.
                wrapperGCHandle.Free();
                dataChannel.Dispose();
                dataChannel = null;

                Utils.ThrowOnErrorCode(res);
                return null; // for the compiler
            });
        }

        internal bool RemoveDataChannel(IntPtr dataChannelHandle)
        {
            ThrowIfConnectionNotOpen();
            return (PeerConnectionInterop.PeerConnection_RemoveDataChannel(_nativePeerhandle, dataChannelHandle) == Utils.MRS_SUCCESS);
        }

        #endregion


        #region Signaling

        /// <summary>
        /// Inform the WebRTC peer connection of a newly received ICE candidate.
        /// </summary>
        /// <param name="sdpMid"></param>
        /// <param name="sdpMlineindex"></param>
        /// <param name="candidate"></param>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        public void AddIceCandidate(string sdpMid, int sdpMlineindex, string candidate)
        {
            ThrowIfConnectionNotOpen();
            PeerConnectionInterop.PeerConnection_AddIceCandidate(_nativePeerhandle, sdpMid, sdpMlineindex, candidate);
        }

        /// <summary>
        /// Create an SDP offer message as an attempt to establish a connection.
        /// Once the message is ready to be sent, the <see cref="LocalSdpReadytoSend"/> event is fired
        /// to allow the user to send that message to the remote peer via its selected signaling solution.
        /// </summary>
        /// <returns><c>true</c> if the offer creation task was successfully submitted.</returns>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        /// <remarks>
        /// The SDP offer message is not successfully created until the <see cref="LocalSdpReadytoSend"/>
        /// event is triggered, and may still fail even if this method returns <c>true</c>, for example if
        /// the peer connection is not in a valid state to create an offer.
        /// </remarks>
        public bool CreateOffer()
        {
            ThrowIfConnectionNotOpen();
            return (PeerConnectionInterop.PeerConnection_CreateOffer(_nativePeerhandle) == Utils.MRS_SUCCESS);
        }

        /// <summary>
        /// Create an SDP answer message to a previously-received offer, to accept a connection.
        /// Once the message is ready to be sent, the <see cref="LocalSdpReadytoSend"/> event is fired
        /// to allow the user to send that message to the remote peer via its selected signaling solution.
        /// </summary>
        /// <returns><c>true</c> if the answer creation task was successfully submitted.</returns>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        /// <remarks>
        /// The SDP answer message is not successfully created until the <see cref="LocalSdpReadytoSend"/>
        /// event is triggered, and may still fail even if this method returns <c>true</c>, for example if
        /// the peer connection is not in a valid state to create an answer.
        /// </remarks>
        public bool CreateAnswer()
        {
            ThrowIfConnectionNotOpen();
            return (PeerConnectionInterop.PeerConnection_CreateAnswer(_nativePeerhandle) == Utils.MRS_SUCCESS);
        }

        /// <summary>
        /// Pass the given SDP description received from the remote peer via signaling to the
        /// underlying WebRTC implementation, which will parse and use it.
        ///
        /// This must be called by the signaler when receiving a message.
        /// </summary>
        /// <param name="type">The type of SDP message ("offer" or "answer")</param>
        /// <param name="sdp">The content of the SDP message</param>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        public void SetRemoteDescription(string type, string sdp)
        {
            ThrowIfConnectionNotOpen();
            PeerConnectionInterop.PeerConnection_SetRemoteDescription(_nativePeerhandle, type, sdp);
        }

        #endregion


        /// <summary>
        /// Utility to throw an exception if a method is called before the underlying
        /// native peer connection has been initialized.
        /// </summary>
        /// <exception xref="InvalidOperationException">The peer connection is not intialized.</exception>
        private void ThrowIfConnectionNotOpen()
        {
            lock (_openCloseLock)
            {
                if (_nativePeerhandle.IsClosed)
                {
                    throw new InvalidOperationException("Cannot invoke native method with invalid peer connection handle.");
                }
            }
        }

        internal void OnConnected()
        {
            IsConnected = true;
            Connected?.Invoke();
        }

        internal void OnDataChannelAdded(DataChannel dataChannel)
        {
            DataChannelAdded?.Invoke(dataChannel);
        }

        internal void OnDataChannelRemoved(DataChannel dataChannel)
        {
            DataChannelRemoved?.Invoke(dataChannel);
        }

        /// <summary>
        /// Callback invoked by the internal WebRTC implementation when it needs a SDP message
        /// to be dispatched to the remote peer.
        /// </summary>
        /// <param name="type">The SDP message type.</param>
        /// <param name="sdp">The SDP message content.</param>
        internal void OnLocalSdpReadytoSend(string type, string sdp)
        {
            LocalSdpReadytoSend?.Invoke(type, sdp);
        }

        internal void OnIceCandidateReadytoSend(string candidate, int sdpMlineindex, string sdpMid)
        {
            IceCandidateReadytoSend?.Invoke(candidate, sdpMlineindex, sdpMid);
        }

        internal void OnIceStateChanged(IceConnectionState newState)
        {
            IceStateChanged?.Invoke(newState);
        }

        internal void OnIceGatheringStateChanged(IceGatheringState newState)
        {
            IceGatheringStateChanged?.Invoke(newState);
        }

        internal void OnRenegotiationNeeded()
        {
            RenegotiationNeeded?.Invoke();
        }

        internal void OnTrackAdded(TrackKind trackKind)
        {
            TrackAdded?.Invoke(trackKind);
        }

        internal void OnTrackRemoved(TrackKind trackKind)
        {
            TrackRemoved?.Invoke(trackKind);
        }
    }
}