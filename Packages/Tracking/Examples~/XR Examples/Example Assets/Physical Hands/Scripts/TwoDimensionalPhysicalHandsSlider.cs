using Leap.Unity.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.PhysicalHands;

namespace Leap.Unity.PhysicalHandsExamples
{
    [RequireComponent(typeof(Rigidbody))]
    public class TwoDimensionalPhysicalHandsSlider : MonoBehaviour
    {
        [SerializeField, OnEditorChange("AutoAssignConnectedButton")]
        GameObject _slideableObject;

        #region Direction Enums

        public enum SliderType
        {
            ONE_DIMENSIONAL,
            TWO_DIMENSIONAL,
        }
        [SerializeField, Leap.Unity.Attributes.OnEditorChange("SliderTypeChanged")]
        internal SliderType _sliderType = SliderType.ONE_DIMENSIONAL;

        private void SliderTypeChanged()
        {
            Debug.Log("SliderTypeChanged");
            if (_slideableObject)
            {
                foreach (var joint in _slideableObject.GetComponents<ConfigurableJoint>())
                {
                    DestroyImmediate(joint);
                }
            }
        }

        public enum SliderDirection
        {
            X,
            Z
        }
        [SerializeField]
        internal SliderDirection _sliderDirection = SliderDirection.X;

        #endregion

        public UnityEvent<Vector2> TwoDimSliderChangeEvent = new UnityEvent<Vector2>();
        public UnityEvent<Vector2> TwoDimSliderButtonPressedEvent = new UnityEvent<Vector2>();
        public UnityEvent<Vector2> TwoDimSliderButtonUnPressedEvent = new UnityEvent<Vector2>();

        /// <summary>
        /// The travel distance of the two-dimensional slider.
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider) in both X and Y axes.
        /// </summary>
        [SerializeField]
        public Vector2 SliderTravelDistance = new Vector2(0.22f, 0.22f);

        /// <summary>
        /// Number of segments for the one-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public int _numberOfSegments = 0;

        /// <summary>
        /// Number of segments for the two-dimensional slider to use.
        /// 0 = unlimited segments.
        /// </summary>
        [SerializeField]
        public Vector2 _twoDimNumberOfSegments = Vector2.zero;

        [SerializeField]
        public Vector2 _twoDimStartPosition = Vector2.zero;

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private PhysicalHandsButton _connectedButton;

        private bool _freezeIfNotActive = true;

        [SerializeField]
        private Vector3 _sliderValue = Vector3.zero;

        private Rigidbody _slideableObjectRigidbody;
        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        private float _sliderXZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private Vector3 _prevSliderValue = Vector3.zero;

        private Quaternion _initialRotation;

        private float localSliderTravelDistanceHalf;
        private Vector2 localTwoDimSliderTravelDistanceHalf;

        /// <summary>
        /// Use this to get the slider value on all axes.
        /// </summary>
        /// <returns>Vector3 ofslider values.</returns>
        public Vector2 GetSliderValue()
        {
            return new Vector2(_sliderValue.x, _sliderValue.y);
        }

        private void AutoAssignConnectedButton()
        {
            if (_slideableObject != null)
            {
                _slideableObject.TryGetComponent<PhysicalHandsButton>(out PhysicalHandsButton physicalHandsButton);
                if (_connectedButton == null)
                {
                    _connectedButton = physicalHandsButton;
                }
            }
        }

        #region Unity Methods

        /// <summary>
        /// Initializes the slider when the GameObject is enabled.
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        private void OnDisable()
        {
            if (_connectedButton != null)
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
            }
        }

        private void OnEnable()
        {
            if (_connectedButton != null)
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
            }
        }

        /// <summary>
        /// Update method called once per frame.
        /// </summary>
        private void Update()
        {
            // Calculate the change in position of the slider object from its zero position
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;
            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue(_axisChangeFromZero);

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SendSliderEvent(TwoDimSliderChangeEvent);
            }

            _prevSliderValue = _sliderValue;
        }

        private void FixedUpdate()
        {
            // Account for joints being a little weird when rotating parent rigidbodies.
            // Set the rotation to the initial rotation of the sliding object when it gets too far away. 
            // Note. This will stop the slideable object from being rotated at all at runtime
            if (Quaternion.Angle(_slideableObject.transform.localRotation, _initialRotation) > 0.5f)
            {
                _slideableObject.transform.localRotation = _initialRotation;
            }
        }

        #endregion

        #region Set Up

        private void Initialize()
        {
            MakeLocalSliderScales();

            // Check if the slideable object is assigned
            if (_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider. \n This script has been disabled", this.gameObject);
                this.enabled = false;
                return;
            }

            ConfigureSlideableObject();

            // Set up the slider based on its type

            SetUpTwoDimSlider();

            // Update the zero position of the slider
            UpdateTwoDimensionalSliderZeroPos();

            ConfigureConnectedButton();

            // Freeze or unfreeze slider position based on the flag
            UpdateSliderPosition();
        }

        /// <summary>
        /// Calculates local slider scales based on the slider type and direction.
        /// </summary>
        private void MakeLocalSliderScales()
        {
            localTwoDimSliderTravelDistanceHalf.x = SliderTravelDistance.x / transform.lossyScale.x / 2;
            localTwoDimSliderTravelDistanceHalf.y = SliderTravelDistance.y / transform.lossyScale.z / 2;
        }

        /// <summary>
        /// Configures the slideable object with necessary components and event handlers.
        /// </summary>
        private void ConfigureSlideableObject()
        {
            PhysicalHandsSlideHelper slideHelper;
            if (!_slideableObject.TryGetComponent<PhysicalHandsSlideHelper>(out slideHelper))
            {
                slideHelper = _slideableObject.AddComponent<PhysicalHandsSlideHelper>();
            }

            slideHelper._onHandGrab += OnHandGrab;
            slideHelper._onHandGrabExit += OnHandGrabExit;
            slideHelper._onHandContact += OnHandContact;
            slideHelper._onHandContactExit += OnHandContactExit;

            if (!_slideableObject.TryGetComponent<Rigidbody>(out _slideableObjectRigidbody))
            {
                _slideableObjectRigidbody = _slideableObject.AddComponent<Rigidbody>();
            }

            _slideableObjectRigidbody.useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            _initialRotation = _slideableObject.transform.localRotation;
        }

        /// <summary>
        /// Configures the connected button with event listeners.
        /// </summary>
        private void ConfigureConnectedButton()
        {
            if (_connectedButton == null)
            {
                if (_slideableObject.TryGetComponent<PhysicalHandsButton>(out _connectedButton))
                {
                    _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                    _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
                }
            }
            else
            {
                _connectedButton.OnButtonPressed?.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.RemoveListener(ButtonUnPressed);
                _connectedButton.OnButtonPressed?.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed?.AddListener(ButtonUnPressed);
            }
        }

        /// <summary>
        /// Updates the slider position based on freezing conditions and starting positions.
        /// </summary>
        private void UpdateSliderPosition()
        {
            if (_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            UpdateSliderPos(_twoDimStartPosition);
        }

        /// <summary>
        /// Updates the zero position for a two-dimensional slider based on its direction.
        /// </summary>
        private void UpdateTwoDimensionalSliderZeroPos()
        {
            // Initialize offset variables
            float xOffset = _configurableJoints.ElementAt(0).anchor.x - localTwoDimSliderTravelDistanceHalf.x;
            float zOffset = _configurableJoints.ElementAt(1).anchor.z - localTwoDimSliderTravelDistanceHalf.y;

            // Update zero positions based on calculated offsets and current object position
            _sliderXZeroPos = xOffset + _slideableObject.transform.localPosition.x;
            _sliderZZeroPos = zOffset + _slideableObject.transform.localPosition.z;
        }

        #region Set Up Joints
        /// <summary>
        /// Sets up the configurable joint for the slider.
        /// </summary>
        /// <param name="joint">The configurable joint to set up.</
        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
            if (joint == null)
            {
                return;
            }

            joint.connectedBody = this.GetComponent<Rigidbody>();
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            JointDrive jointDrive = new JointDrive();
            jointDrive.positionDamper = 10;
            jointDrive.maximumForce = 2;
            joint.xDrive = jointDrive;
            joint.yDrive = jointDrive;
            joint.zDrive = jointDrive;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;

            joint.anchor = Vector3.zero;
        }

        /// <summary>
        /// Sets up a two-dimensional slider.
        /// </summary>
        private void SetUpTwoDimSlider()
        {
            if (_slideableObject == null)
            {
                Debug.LogWarning("There is no slideable object. Please add one to use the slider.", this.gameObject);
                return;
            }

            foreach (var joint in _slideableObject.GetComponents<ConfigurableJoint>())
            {
                Destroy(joint);
            }
   

            while (_configurableJoints.Count < 2)
            {
                _configurableJoints.Add(_slideableObject.AddComponent<ConfigurableJoint>());
            }

            foreach (var joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);
            }

            //Set up joint limits for separate travel distances on each axis
            SoftJointLimit linerJointLimit = new SoftJointLimit();
            linerJointLimit.limit = SliderTravelDistance.x / 2;
            _configurableJoints.ElementAt(0).linearLimit = linerJointLimit;
            linerJointLimit.limit = SliderTravelDistance.y / 2;
            _configurableJoints.ElementAt(1).linearLimit = linerJointLimit;

            _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
            _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
            _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
            _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            MakeLocalSliderScales();
            DrawJointRangeGizmo();
        }

        /// <summary>
        /// Draws gizmos representing the range of motion for the joint based on its type and direction.
        /// </summary>
        private void DrawJointRangeGizmo()
        {
            Vector3 jointPosition = _slideableObject.transform.localPosition;
            Matrix4x4 m = _slideableObject.transform.localToWorldMatrix;
            m.SetTRS(this.transform.position, m.rotation, m.lossyScale);
            Gizmos.matrix = m;

            Vector2 TwoDimSliderTravelDistanceHalf = SliderTravelDistance / 2;

            Gizmos.color = Color.red; // X axis
            Gizmos.DrawLine(jointPosition + Vector3.right * (TwoDimSliderTravelDistanceHalf.x / _slideableObject.transform.lossyScale.x), jointPosition - Vector3.right * (TwoDimSliderTravelDistanceHalf.x / _slideableObject.transform.lossyScale.x));
            Gizmos.color = Color.blue; // Z axis
            Gizmos.DrawLine(jointPosition + Vector3.forward * (TwoDimSliderTravelDistanceHalf.y / _slideableObject.transform.lossyScale.z), jointPosition - Vector3.forward * (TwoDimSliderTravelDistanceHalf.y / _slideableObject.transform.lossyScale.z));
            Vector3 extents = new Vector3(SliderTravelDistance.x / _slideableObject.transform.lossyScale.x, 0, SliderTravelDistance.y / _slideableObject.transform.lossyScale.z);

            DrawBoxGizmo(jointPosition, extents, Color.cyan);
        }

        private void DrawBoxGizmo(Vector3 position, Vector3 size, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, size);
        }

        #endregion

        #region Events
        private void ButtonPressed()
        {
            UnFreezeSliderPosition();

            SendSliderEvent(TwoDimSliderButtonPressedEvent);
        }

        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();

            SendSliderEvent(TwoDimSliderButtonUnPressedEvent);
        }

        public void OnHandGrab(ContactHand hand)
        {
            UnFreezeSliderPosition();
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();
        }

        public void OnHandContact(ContactHand hand)
        {
            UnFreezeSliderPosition();
        }

        public void OnHandContactExit(ContactHand hand)
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            SnapSliderOnRelease();
        }

        void SnapSliderOnRelease()
        {
            if (_twoDimNumberOfSegments.x != 0 || _twoDimNumberOfSegments.y != 0)
            {
                SnapToSegment();
            }

            // Calculate the change in position of the slider object from its zero position
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;
            // Calculate the slider value based on the change in position
            _sliderValue = CalculateSliderValue(_axisChangeFromZero);

            // Check if the slider value has changed
            if (_prevSliderValue != _sliderValue)
            {
                // Send slider change event
                SendSliderEvent(TwoDimSliderChangeEvent);
            }

            _prevSliderValue = _sliderValue;
        }

        /// <summary>
        /// Unfreezes the slider position, allowing it to move freely.
        /// </summary>
        private void UnFreezeSliderPosition()
        {
            _slideableObjectRigidbody.constraints = RigidbodyConstraints.None;
            _slideableObjectRigidbody.freezeRotation = true;
            _slideableObjectRigidbody.isKinematic = false;
        }
        /// <summary>
        /// Freezes the slider position, preventing it from moving.
        /// </summary>
        private void FreezeSliderPosition()
        {
            _slideableObjectRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            _slideableObjectRigidbody.isKinematic = true;
        }

        /// <summary>
        /// Standardised event to send any sort of value events. 
        /// If you know which event will be used then only send the relevant event with the other being null.
        /// Invoke by passing the correct type of event.
        /// </summary>
        /// <param name="unityEvent"></param>
        /// <param name="twoDimUnityEvent"></param>
        void SendSliderEvent(UnityEvent<Vector2> twoDimCurEvent)
        {
            twoDimCurEvent?.Invoke(new Vector2(_sliderValue.x, _sliderValue.z));
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the position of a slider object based on the provided value or two-dimensional value.
        /// </summary>
        /// <param name="value">The value representing the position along the slider.</param>
        /// <param name="twoDimValue">The two-dimensional value representing the position along the slider in two dimensions.</param>
        private void UpdateSliderPos(Vector2 twoDimValue)
        {
            // Get the current position of the slider object
            Vector3 slidePos = _slideableObject.transform.localPosition;

            // Determine the type of slider and update its position accordingly
            slidePos.x += Utils.Map(twoDimValue.x, 0, 1, 0, localTwoDimSliderTravelDistanceHalf.x * 2) + _sliderXZeroPos;
            slidePos.z += Utils.Map(twoDimValue.y, 0, 1, 0, localTwoDimSliderTravelDistanceHalf.y * 2) + _sliderZZeroPos;

            // Reset velocity to zero and update the position of the slider object
            _slideableObjectRigidbody.velocity = Vector3.zero;
            _slideableObjectRigidbody.transform.localPosition = slidePos;
        }

        /// <summary>
        /// Calculates the value of slider movement based on the change from zero position.
        /// </summary>
        /// <param name="changeFromZero">Change in position from zero position.</param>
        /// <returns>The calculated slider value.</returns>
        private Vector3 CalculateSliderValue(in Vector3 changeFromZero)
        {

            return new Vector3(
                Utils.Map(changeFromZero.x, 0, localTwoDimSliderTravelDistanceHalf.x * 2, 0, 1),
                0,
                Utils.Map(changeFromZero.z, 0, localTwoDimSliderTravelDistanceHalf.y * 2, 0, 1));
        }

        /// <summary>
        /// Snaps the slider to the nearest segment based on its type and direction.
        /// </summary>
        private void SnapToSegment()
        {
            // Initialize variables
            float closestStep = 0;
            float value = 0;
            Vector2 twoDimValue = Vector2.zero;

            // Snap to segments on X and Z axes
            if (_twoDimNumberOfSegments.x != 0)
            {
                closestStep = GetClosestStep(_sliderValue.x, (int)_twoDimNumberOfSegments.x);
                twoDimValue.x = closestStep;
            }
            if (_twoDimNumberOfSegments.y != 0)
            {
                closestStep = GetClosestStep(_sliderValue.z, (int)_twoDimNumberOfSegments.y);
                twoDimValue.y = closestStep;
            }
            // Update the slider position
            UpdateSliderPos(twoDimValue);
        }

        private float GetClosestStep(float inputNumber, int numberOfSteps)
        {
            if (numberOfSteps == 0)
            {
                return (float)inputNumber;
            }

            // Calculate the step size
            float stepSize = 1.0f / numberOfSteps;

            // Calculate the closest step index
            int closestStepIndex = (int)Math.Round(inputNumber / stepSize);

            // Calculate the closest step value
            float closestStepValue = closestStepIndex * stepSize;

            return closestStepValue;
        }

        #endregion
    }
}