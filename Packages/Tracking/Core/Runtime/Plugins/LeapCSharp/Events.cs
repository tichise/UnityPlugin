/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    using LeapInternal;
    using System;

    /// <summary>
    /// An enumeration defining the types of Leap Motion events.
    /// @since 3.0
    /// </summary>
    public enum LeapEvent
    {
        EVENT_CONNECTION,        //!< A connection event has occurred
        EVENT_CONNECTION_LOST,   //!< The connection with the service has been lost
        EVENT_DEVICE,            //!<  A device event has occurred
        EVENT_DEVICE_FAILURE,    //!< A device failure event has occurred
        EVENT_DEVICE_LOST,       //!< Event asserted when the underlying device object has been lost
        EVENT_POLICY_CHANGE,     //!< A change in policy occurred
        EVENT_CONFIG_RESPONSE,   //!< Response to a Config value request
        EVENT_CONFIG_CHANGE,     //!< Success response to a Config value change
        EVENT_FRAME,             //!< A tracking frame has been received
        EVENT_INTERNAL_FRAME,    //!< An internal tracking frame has been received
        EVENT_IMAGE_COMPLETE,    //!< A requested image is available
        EVENT_IMAGE_REQUEST_FAILED, //!< A requested image could not be provided
        EVENT_DISTORTION_CHANGE, //!< The distortion matrix used for image correction has changed
        EVENT_LOG_EVENT,         //!< A diagnostic event has occurred
        EVENT_INIT,
        EVENT_DROPPED_FRAME,
        EVENT_IMAGE,             //!< An unrequested image is available
        EVENT_POINT_MAPPING_CHANGE,
        EVENT_HEAD_POSE,
        EVENT_FIDUCIAL_POSE
    };
    /// <summary>
    /// A generic object with no arguments beyond the event type.
    /// @since 3.0
    /// </summary>
    public class LeapEventArgs : EventArgs
    {
        public LeapEventArgs(LeapEvent type)
        {
            this.type = type;
        }
        public LeapEvent type { get; set; }
    }

    /// <summary>
    /// Dispatched when a tracking frame is ready.
    ///
    /// Provides the Frame object as an argument.
    /// @since 3.0
    /// </summary>
    public class FrameEventArgs : LeapEventArgs
    {
        public FrameEventArgs(Frame frame) : base(LeapEvent.EVENT_FRAME)
        {
            this.frame = frame;
        }

        public Frame frame { get; set; }
    }

    public class InternalFrameEventArgs : LeapEventArgs
    {
        public InternalFrameEventArgs(ref LEAP_TRACKING_EVENT frame) : base(LeapEvent.EVENT_INTERNAL_FRAME)
        {
            this.frame = frame;
        }

        public LEAP_TRACKING_EVENT frame { get; set; }
    }

    /// <summary>
    /// Dispatched when loggable events are generated by the service and the
    /// service connection code.
    ///
    /// Provides the severity rating, log text, and timestamp as arguments.
    /// @since 3.0
    /// </summary>
    public class LogEventArgs : LeapEventArgs
    {
        public LogEventArgs(MessageSeverity severity, Int64 timestamp, string message) : base(LeapEvent.EVENT_LOG_EVENT)
        {
            this.severity = severity;
            this.message = message;
            this.timestamp = timestamp;
        }

        public MessageSeverity severity { get; set; }
        public Int64 timestamp { get; set; }
        public string message { get; set; }
    }

    /// <summary>
    /// Dispatched when a policy change is complete.
    ///
    /// Provides the current and previous policies as arguments.
    ///
    /// @since 3.0
    /// </summary>
    public class PolicyEventArgs : LeapEventArgs
    {
        public PolicyEventArgs(UInt64 currentPolicies, UInt64 oldPolicies, bool oldPolicyIsValid, Device device) : base(LeapEvent.EVENT_POLICY_CHANGE)
        {
            this.currentPolicies = currentPolicies;
            this.oldPolicies = oldPolicies;
            this.device = device;
        }

        /// <summary>
        /// Current policy flags
        /// </summary>
        public UInt64 currentPolicies { get; set; }

        /// <summary>
        /// Previous policy flags, if known
        /// </summary>
        public UInt64 oldPolicies { get; set; }

        /// <summary>
        /// Is the value for the old policy flags valid / known
        /// @since 5.7.0 (plugin)
        /// </summary>
        public bool oldPolicyIsValid { get; set; }

        /// <summary>
        /// The device associated with the policy flag change
        /// @since 5.7.0 (plugin)
        /// </summary>
        public Device device { get; set; }
    }

    /// <summary>
    /// Dispatched when the image distortion map changes.
    ///
    /// Provides the new distortion map as an argument.
    /// @since 3.0
    /// </summary>
    public class DistortionEventArgs : LeapEventArgs
    {
        public DistortionEventArgs(DistortionData distortion, Image.CameraType camera) : base(LeapEvent.EVENT_DISTORTION_CHANGE)
        {
            this.distortion = distortion;
            this.camera = camera;
        }
        public DistortionData distortion { get; protected set; }
        public Image.CameraType camera { get; protected set; }
    }

    /// <summary>
    /// Dispatched when a configuration change is completed.
    ///
    /// Provides the configuration key, whether the change was successful, and the id of the original change request.
    /// @since 3.0
    /// </summary>
    [Obsolete("Config is not used in Ultraleap's Tracking Service 5.X+. This will be removed in the next Major release")]
    public class ConfigChangeEventArgs : LeapEventArgs
    {
        public ConfigChangeEventArgs(string config_key, bool succeeded, uint requestId) : base(LeapEvent.EVENT_CONFIG_CHANGE)
        {
            this.ConfigKey = config_key;
            this.Succeeded = succeeded;
            this.RequestId = requestId;
        }
        public string ConfigKey { get; set; }
        public bool Succeeded { get; set; }
        public uint RequestId { get; set; }

    }

    /// <summary>
    /// Dispatched when a configuration change is completed.
    ///
    /// Provides the configuration key, whether the change was successful, and the id of the original change request.
    /// @since 3.0
    /// </summary>
    [Obsolete("Config.cs is not used in Ultraleap's Tracking Service 5.X+. This will be removed in the next Major release")]
    public class SetConfigResponseEventArgs : LeapEventArgs
    {
        public SetConfigResponseEventArgs(string config_key, Config.ValueType dataType, object value, uint requestId) : base(LeapEvent.EVENT_CONFIG_RESPONSE)
        {
            this.ConfigKey = config_key;
            this.DataType = dataType;
            this.Value = value;
            this.RequestId = requestId;
        }
        public string ConfigKey { get; set; }
        public Config.ValueType DataType { get; set; }
        public object Value { get; set; }
        public uint RequestId { get; set; }
    }

    /// <summary>
    /// Dispatched when the connection is established.
    /// @since 3.0
    /// </summary>
    public class ConnectionEventArgs : LeapEventArgs
    {
        public ConnectionEventArgs() : base(LeapEvent.EVENT_CONNECTION) { }
    }

    /// <summary>
    /// Dispatched when the connection is lost.
    /// @since 3.0
    /// </summary>
    public class ConnectionLostEventArgs : LeapEventArgs
    {
        public ConnectionLostEventArgs() : base(LeapEvent.EVENT_CONNECTION_LOST) { }
    }

    /// <summary>
    /// Dispatched when a device is plugged in.
    ///
    /// Provides the device as an argument.
    /// @since 3.0
    /// </summary>
    public class DeviceEventArgs : LeapEventArgs
    {
        public DeviceEventArgs(Device device) : base(LeapEvent.EVENT_DEVICE)
        {
            this.Device = device;
        }
        public Device Device { get; set; }
    }

    /// <summary>
    /// Dispatched when a device is plugged in, but fails to initialize or when
    /// a working device fails in use.
    ///
    /// Provides the failure reason and, if available, the serial number.
    /// @since 3.0
    /// </summary>
    public class DeviceFailureEventArgs : LeapEventArgs
    {
        public DeviceFailureEventArgs(uint code, string message, string serial) : base(LeapEvent.EVENT_DEVICE_FAILURE)
        {
            ErrorCode = code;
            ErrorMessage = message;
            DeviceSerialNumber = serial;
        }

        public uint ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string DeviceSerialNumber { get; set; }
    }

    public class DroppedFrameEventArgs : LeapEventArgs
    {
        public DroppedFrameEventArgs(Int64 frame_id, eLeapDroppedFrameType type) : base(LeapEvent.EVENT_DROPPED_FRAME)
        {
            frameID = frame_id;
            reason = type;
        }

        public Int64 frameID { get; set; }
        public eLeapDroppedFrameType reason { get; set; }
    }

    /// <summary>
    /// Dispatched when an unrequested Image is ready.
    ///
    /// Provides the Image object as an argument.
    /// @since 4.0
    /// </summary>
    public class ImageEventArgs : LeapEventArgs
    {
        public ImageEventArgs(Image image) : base(LeapEvent.EVENT_IMAGE)
        {
            this.image = image;
        }

        public Image image { get; set; }
    }

    /// <summary>
    /// Dispatched when point mapping change events are generated by the service.
    ///
    /// @since 4.0
    /// </summary>
    public class PointMappingChangeEventArgs : LeapEventArgs
    {
        public PointMappingChangeEventArgs(Int64 frame_id, Int64 timestamp, UInt32 nPoints) : base(LeapEvent.EVENT_POINT_MAPPING_CHANGE)
        {
            this.frameID = frame_id;
            this.timestamp = timestamp;
            this.nPoints = nPoints;
        }

        public Int64 frameID { get; set; }
        public Int64 timestamp { get; set; }
        public UInt32 nPoints { get; set; }
    }

    public class HeadPoseEventArgs : LeapEventArgs
    {
        public HeadPoseEventArgs(LEAP_VECTOR head_position, LEAP_QUATERNION head_orientation) : base(LeapEvent.EVENT_POINT_MAPPING_CHANGE)
        {
            this.headPosition = head_position;
            this.headOrientation = head_orientation;
        }

        public LEAP_VECTOR headPosition { get; set; }
        public LEAP_QUATERNION headOrientation { get; set; }
    }

    public class FiducialPoseEventArgs : LeapEventArgs
    {
        public FiducialPoseEventArgs(UInt64 id, float estimated_error, LEAP_VECTOR translation, LEAP_MATRIX_3x3 rotation) : base(LeapEvent.EVENT_FIDUCIAL_POSE)
        {
            this.id = id;
            this.estimated_error = estimated_error;
            this.translation = translation;
            this.rotation = rotation;
        }

        public UInt64 id { get; set; }
        public float estimated_error { get; set; }
        LEAP_VECTOR translation { get; set; }
        LEAP_MATRIX_3x3 rotation { get; set; }
    }

    public struct BeginProfilingForThreadArgs
    {
        public string threadName;
        public string[] blockNames;

        public BeginProfilingForThreadArgs(string threadName, params string[] blockNames)
        {
            this.threadName = threadName;
            this.blockNames = blockNames;
        }
    }

    public struct EndProfilingForThreadArgs { }

    public struct BeginProfilingBlockArgs
    {
        public string blockName;

        public BeginProfilingBlockArgs(string blockName)
        {
            this.blockName = blockName;
        }
    }

    public struct EndProfilingBlockArgs
    {
        public string blockName;

        public EndProfilingBlockArgs(string blockName)
        {
            this.blockName = blockName;
        }
    }
}