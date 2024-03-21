using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsUISlider : MonoBehaviour
    {

        [SerializeField]
        GameObject _slideableObject;

        #region Direction Enums

        public enum SliderType
        {
            ONE_DIMENSIONAL,
            TWO_DIMENSIONAL,
        }
        [SerializeField]
        internal SliderType _sliderType = SliderType.ONE_DIMENSIONAL;

        public enum SliderDirection
        {
            X,
            Y,
            Z
        }
        [SerializeField]
        internal SliderDirection _sliderDirection = SliderDirection.X;

        public enum TwoDimSliderDirection
        {
            XY,
            XZ,
            YZ,
        }
        [SerializeField]
        internal TwoDimSliderDirection _twoDimSliderDirection = TwoDimSliderDirection.XY;

        #endregion

        public UnityEvent<int> SliderChangeEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderChangeEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonPressedEvent = new UnityEvent<int, int>();
        public UnityEvent<int> SliderButtonUnPressedEvent = new UnityEvent<int>();
        public UnityEvent<int, int> TwoDimensionalSliderButtonUnPressedEvent = new UnityEvent<int, int>();

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        public float SliderTravelDistance = 0.22f;

        /// <summary>
        /// The travel distance of the slider (from the central point).
        /// i.e. slider center point +/- slider travel distance (or half the full travel of the slider).
        /// </summary>
        [SerializeField]
        public Vector2 TwoDimSliderTravelDistance = new Vector2(0.22f, 0.22f);


        /// <summary>
        /// Number of segments for the slider to use.
        /// 0 = unlimited segments
        /// </summary>
        [SerializeField]
        public int _numberOfSegments = 0;

        /// <summary>
        /// Number of segments for the slider to use.
        /// 0 = unlimited segments
        /// </summary>
        [SerializeField]
        public Vector2 _twoDimNumberOfSegments = Vector2.zero;

        [SerializeField]
        private Vector3 _axisChangeFromZero = Vector3.zero;

        [SerializeField]
        private PhysicalHandsButton _connectedButton;

        [SerializeField]
        private bool _freezeIfNotActive = false;

        [SerializeField]
        private Vector3 _sliderValue = Vector3.zero;

        [SerializeField]
        public float _startPosition = 0;

        [SerializeField]
        public Vector2 _twoDimStartPosition = Vector2.zero;

        private Rigidbody _slideableObjectRigidbody;
        private List<ConfigurableJoint> _configurableJoints = new List<ConfigurableJoint>();

        private float _sliderXZeroPos = 0;
        private float _sliderYZeroPos = 0;
        private float _sliderZZeroPos = 0;

        private bool _sliderReleasedLastFrame = false;
        private Vector3 prevSliderValue = Vector3.zero;


        /// <summary>
        /// Use this to get the slider value on all axis.
        /// </summary>
        /// <returns>Vector3 ofslider values.</returns>
        public Dictionary<char, float> GetSliderValue()
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x } };
                            case SliderDirection.Y:
                                return new Dictionary<char, float>() { { 'y', _sliderValue.y } };
                            case SliderDirection.Z:
                                return new Dictionary<char, float>() { { 'z', _sliderValue.z } };
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x }, { 'y', _sliderValue.y } };
                            case TwoDimSliderDirection.XZ:
                                return new Dictionary<char, float>() { { 'x', _sliderValue.x }, { 'z', _sliderValue.y } };
                            case TwoDimSliderDirection.YZ:
                                return new Dictionary<char, float>() { { 'y', _sliderValue.x }, { 'z', _sliderValue.y } };
                        }
                        break;
                    }
            }
            return null;
        }

        #region Unity Methods

        private void OnEnable()
        {
            if (_slideableObject == null)
            {
                Debug.Log("There is no slideable object. Please add one to use the slider. \n This script has been disabled");
                this.enabled = false;
            }
            PhysicalHandsUISliderHelper slideHelper;
            if (!_slideableObject.TryGetComponent<PhysicalHandsUISliderHelper>(out slideHelper))
            {
                slideHelper = _slideableObject.AddComponent<PhysicalHandsUISliderHelper>();
            }
            
            slideHelper._onHandGrab += OnHandGrab;
            slideHelper._onHandGrabExit += OnHandGrabExit;
            slideHelper._onHandContact += OnHandContact;
            slideHelper._onHandContactExit += OnHandContactExit;

            if (!_slideableObject.TryGetComponent<Rigidbody>(out _slideableObjectRigidbody))
            {
                _slideableObjectRigidbody = _slideableObject.AddComponent<Rigidbody>();
            }

            
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    SetUpSlider();
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    SetUpTwoDimSlider();
                    break;
            }

            UpdateSliderZeroPos();

            if (_connectedButton == null)
            {
                if (_slideableObject.TryGetComponent<PhysicalHandsButton>(out _connectedButton))
                {
                    _connectedButton.OnButtonPressed.AddListener(ButtonPressed);
                    _connectedButton.OnButtonUnPressed.AddListener(ButtonUnPressed);
                }
            }
            else
            {
                _connectedButton.OnButtonPressed.AddListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed.AddListener(ButtonUnPressed);
            }
            if(_freezeIfNotActive == false)
            {
                UnFreezeSliderPosition();
            }
            else
            {
                FreezeSliderPosition();
            }

            UpdateSliderPos(_startPosition, _twoDimStartPosition);
        }



        private void OnDisable()
        {
            if(_connectedButton != null)
            {
                _connectedButton.OnButtonPressed.RemoveListener(ButtonPressed);
                _connectedButton.OnButtonUnPressed.RemoveListener(ButtonUnPressed);
            }
            
        }

        private void Update()
        {
            _axisChangeFromZero.x = _slideableObject.transform.localPosition.x - _sliderXZeroPos;
            _axisChangeFromZero.y = _slideableObject.transform.localPosition.y - _sliderYZeroPos;
            _axisChangeFromZero.z = _slideableObject.transform.localPosition.z - _sliderZZeroPos;

            _sliderValue = calculateSliderValue(_axisChangeFromZero);

            if (prevSliderValue != _sliderValue)
            {
                SendSliderEvent(SliderChangeEvent, TwoDimensionalSliderChangeEvent);
                if (_sliderReleasedLastFrame)
                {
                    switch (_sliderType)
                    {
                        case SliderType.ONE_DIMENSIONAL:
                        {
                            if (_numberOfSegments != 0)
                            {
                                SnapToSegment();
                            }
                            break;
                        }
                        case SliderType.TWO_DIMENSIONAL:
                        {
                            if (_twoDimNumberOfSegments.x != 0 || _twoDimNumberOfSegments.y != 0)
                            {
                                SnapToSegment();
                            }
                            break;
                        }
                    }
                    _sliderReleasedLastFrame = false;
                    prevSliderValue = _sliderValue;
                }

            }
        }

        #endregion

        #region Set Up

        /// <summary>
        /// Updates the zero position of the slider based on its type and direction.
        /// </summary>
        private void UpdateSliderZeroPos()
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            _sliderXZeroPos = (_configurableJoints.First().anchor.x + -SliderTravelDistance) + _slideableObject.transform.localPosition.x;
                            break;
                        case SliderDirection.Y:
                            _sliderYZeroPos = (_configurableJoints.First().anchor.y + -SliderTravelDistance) + _slideableObject.transform.localPosition.y;
                            break;
                        case SliderDirection.Z:
                            _sliderZZeroPos = (_configurableJoints.First().anchor.z + -SliderTravelDistance) + _slideableObject.transform.localPosition.z;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            _sliderXZeroPos = (_configurableJoints.ElementAt(0).anchor.x + -TwoDimSliderTravelDistance.x) + _slideableObject.transform.localPosition.x;
                            _sliderYZeroPos = (_configurableJoints.ElementAt(1).anchor.y + -TwoDimSliderTravelDistance.y) + _slideableObject.transform.localPosition.y;
                            break;
                        case TwoDimSliderDirection.XZ:
                            _sliderXZeroPos = (_configurableJoints.ElementAt(0).anchor.x + -TwoDimSliderTravelDistance.x) + _slideableObject.transform.localPosition.x;
                            _sliderZZeroPos = (_configurableJoints.ElementAt(1).anchor.z + -TwoDimSliderTravelDistance.y) + _slideableObject.transform.localPosition.z;
                            break;
                        case TwoDimSliderDirection.YZ:
                            _sliderYZeroPos = (_configurableJoints.ElementAt(0).anchor.y + -TwoDimSliderTravelDistance.x) + _slideableObject.transform.localPosition.y;
                            _sliderZZeroPos = (_configurableJoints.ElementAt(1).anchor.z + -TwoDimSliderTravelDistance.y) + _slideableObject.transform.localPosition.z;
                            break;
                    }
                    break;
            }
        }

        #region Set Up Joints
        /// <summary>
        /// Sets up the configurable joint for the slider.
        /// </summary>
        /// <param name="joint">The configurable joint to set up.</
        private void SetUpConfigurableJoint(ConfigurableJoint joint)
        {
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

            joint.anchor = Vector3.zero;
        }

        /// <summary>
        /// Sets up a one-dimensional slider.
        /// </summary>
        private void SetUpSlider()
        {
            ConfigurableJoint configurableJoint;
            _slideableObject.TryGetComponent<ConfigurableJoint>(out configurableJoint);

            if (_configurableJoints.Count < 1 && configurableJoint == null)
            {
                _configurableJoints.Add(_slideableObject.AddComponent<ConfigurableJoint>());
            }
            else
            {
                _configurableJoints.Add(configurableJoint);
            }

            foreach (ConfigurableJoint joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);

                SoftJointLimit linerJointLimit = new SoftJointLimit();
                linerJointLimit.limit = SliderTravelDistance + 0.01f;
                joint.linearLimit = linerJointLimit;

                switch (_sliderDirection)
                {
                    case SliderDirection.X:
                        joint.xMotion = ConfigurableJointMotion.Limited;
                        break;
                    case SliderDirection.Y:
                        joint.yMotion = ConfigurableJointMotion.Limited;
                        break;
                    case SliderDirection.Z:
                        joint.zMotion = ConfigurableJointMotion.Limited;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets up a two-dimensional slider.
        /// </summary>
        private void SetUpTwoDimSlider()
        {
            _configurableJoints = GetComponents<ConfigurableJoint>().ToList<ConfigurableJoint>();
            for (int i = 0; i < 2; i++)
            {
                if (_configurableJoints.Count != 2)
                {
                    _configurableJoints.Add(gameObject.AddComponent<ConfigurableJoint>());
                }
            }

            foreach (ConfigurableJoint joint in _configurableJoints)
            {
                SetUpConfigurableJoint(joint);


            }

            //Set up joint limits for separate travel distances on each axis
            SoftJointLimit linerJointLimit = new SoftJointLimit();
            linerJointLimit.limit = TwoDimSliderTravelDistance.x + 0.01f;
            _configurableJoints.ElementAt(0).linearLimit = linerJointLimit;
            linerJointLimit.limit = TwoDimSliderTravelDistance.y + 0.01f;
            _configurableJoints.ElementAt(1).linearLimit = linerJointLimit;


            switch (_twoDimSliderDirection)
            {
                case TwoDimSliderDirection.XY:
                    {
                        _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).yMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).yMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                case TwoDimSliderDirection.XZ:
                    {
                        _configurableJoints.ElementAt(0).xMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).xMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                case TwoDimSliderDirection.YZ:
                    {
                        _configurableJoints.ElementAt(0).yMotion = ConfigurableJointMotion.Limited;
                        _configurableJoints.ElementAt(1).yMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(0).zMotion = ConfigurableJointMotion.Free;
                        _configurableJoints.ElementAt(1).zMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
            }
        }
        #endregion

        #endregion

        #region Events
        private void ButtonPressed()
        {
            UnFreezeSliderPosition();

            SendSliderEvent(SliderButtonPressedEvent, TwoDimensionalSliderButtonPressedEvent);
        }
        private void ButtonUnPressed()
        {
            if (_freezeIfNotActive)
            {
                FreezeSliderPosition();
            }

            _sliderReleasedLastFrame = true;

            SendSliderEvent(SliderButtonUnPressedEvent, TwoDimensionalSliderButtonUnPressedEvent);
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

            _sliderReleasedLastFrame = true;
        }

        public void OnHandContact(ContactHand hand)
        {
            
        }

        public void OnHandContactExit(ContactHand hand)
        {
            _sliderReleasedLastFrame = true;
        }


        /// <summary>
        /// Unfreezes the slider position, allowing it to move freely.
        /// </summary>
        private void UnFreezeSliderPosition()
        {
            _slideableObjectRigidbody.constraints = RigidbodyConstraints.None;
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
        void SendSliderEvent(UnityEvent<int> unityEvent, UnityEvent<int, int> twoDimUnityEvent)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                unityEvent.Invoke((int)_sliderValue.x);
                                break;
                            case SliderDirection.Y:
                                unityEvent.Invoke((int)_sliderValue.y);
                                break;
                            case SliderDirection.Z:
                                unityEvent.Invoke((int)_sliderValue.z);
                                break;
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                twoDimUnityEvent.Invoke((int)_sliderValue.x, (int)_sliderValue.y);
                                break;
                            case TwoDimSliderDirection.XZ:
                                twoDimUnityEvent.Invoke((int)_sliderValue.x, (int)_sliderValue.z);
                                break;
                            case TwoDimSliderDirection.YZ:
                                twoDimUnityEvent.Invoke((int)_sliderValue.y, (int)_sliderValue.z);
                                break;
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Update

        private void UpdateSliderPos(float value, Vector2 twoDimValue)
        {
            Vector3 slidePos = _slideableObject.transform.localPosition;

            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            slidePos.x = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderXZeroPos;
                            break;
                        case SliderDirection.Y:
                            slidePos.y = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderYZeroPos;
                            break;
                        case SliderDirection.Z:
                            slidePos.z = Utils.Map(value, 0, 1, 0, SliderTravelDistance * 2) + _sliderZZeroPos;
                            break;
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            slidePos.x += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderXZeroPos;
                            slidePos.y += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderYZeroPos;
                            break;
                        case TwoDimSliderDirection.XZ:
                            slidePos.x += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderXZeroPos;
                            slidePos.z += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderZZeroPos;
                            break;
                        case TwoDimSliderDirection.YZ:
                            slidePos.y += Utils.Map(twoDimValue.x, 0, 1, 0, TwoDimSliderTravelDistance.x * 2) + _sliderYZeroPos;
                            slidePos.z += Utils.Map(twoDimValue.y, 0, 1, 0, TwoDimSliderTravelDistance.y * 2) + _sliderZZeroPos;
                            break;
                    }
                    break;
            }

            _slideableObjectRigidbody.velocity = Vector3.zero;
            _slideableObjectRigidbody.transform.localPosition = slidePos;
            
        }



        /// <summary>
        /// Calculates the value of slider movement based on the change from zero position.
        /// </summary>
        /// <param name="changeFromZero">Change in position from zero position.</param>
        /// <returns>The calculated slider value.</returns>
        private Vector3 calculateSliderValue(in Vector3 changeFromZero)
        {
            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    switch (_sliderDirection)
                    {
                        case SliderDirection.X:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, SliderTravelDistance * 2, 0, 1),
                                0,
                                0);
                        case SliderDirection.Y:
                            return new Vector3(
                                0,
                                Utils.Map(changeFromZero.y, 0, SliderTravelDistance * 2, 0, 1),
                                0);
                        case SliderDirection.Z:
                            return new Vector3(
                                0,
                                0,
                                Utils.Map(changeFromZero.z, 0, SliderTravelDistance * 2, 0, 1));
                    }
                    break;
                case SliderType.TWO_DIMENSIONAL:
                    switch (_twoDimSliderDirection)
                    {
                        case TwoDimSliderDirection.XY:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, TwoDimSliderTravelDistance.x * 2, 0, 1), 
                                Utils.Map(changeFromZero.y, 0, TwoDimSliderTravelDistance.y * 2, 0, 1), 
                                0);
                        case TwoDimSliderDirection.XZ:
                            return new Vector3(
                                Utils.Map(changeFromZero.x, 0, TwoDimSliderTravelDistance.x * 2, 0, 1),
                                0,
                                Utils.Map(changeFromZero.z, 0, TwoDimSliderTravelDistance.y * 2, 0, 1));
                        case TwoDimSliderDirection.YZ:
                            return new Vector3(
                                0,
                                Utils.Map(changeFromZero.y, 0, TwoDimSliderTravelDistance.x * 2, 0, 1),
                                Utils.Map(changeFromZero.z, 0, TwoDimSliderTravelDistance.y * 2, 0, 1));
                    }
                    break;
            }
            return Vector3.negativeInfinity;
        }

        void SnapToSegment()
        {
            float closestStep = 0;
            float value = 0;
            Vector2 twoDimValue = Vector2.zero;

            switch (_sliderType)
            {
                case SliderType.ONE_DIMENSIONAL:
                    {
                        switch (_sliderDirection)
                        {
                            case SliderDirection.X:
                                closestStep = GetClosestStep(_sliderValue.x, _numberOfSegments);
                                value = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                                break;
                            case SliderDirection.Y:
                                closestStep = GetClosestStep(_sliderValue.y, _numberOfSegments);
                                value = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                                break;
                            case SliderDirection.Z:
                                closestStep = GetClosestStep(_sliderValue.z, _numberOfSegments);
                                value = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                                break;
                        }
                        break;
                    }
                case SliderType.TWO_DIMENSIONAL:
                    {
                        switch (_twoDimSliderDirection)
                        {
                            case TwoDimSliderDirection.XY:
                                if (_twoDimNumberOfSegments.x != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.x, (int)_twoDimNumberOfSegments.x);
                                    twoDimValue.x = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }
                                if (_twoDimNumberOfSegments.y != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.y, (int)_twoDimNumberOfSegments.y);
                                    twoDimValue.y = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }
                                break;
                            case TwoDimSliderDirection.XZ:
                                if (_twoDimNumberOfSegments.x != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.x, (int)_twoDimNumberOfSegments.x);
                                    twoDimValue.x = _sliderXZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }
                                if (_twoDimNumberOfSegments.y != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.z, (int)_twoDimNumberOfSegments.y);
                                    twoDimValue.y = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }
                                break;
                            case TwoDimSliderDirection.YZ:
                                if (_twoDimNumberOfSegments.x != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.y, (int)_twoDimNumberOfSegments.x);
                                    twoDimValue.x = _sliderYZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }
                                if (_twoDimNumberOfSegments.y != 0)
                                {
                                    closestStep = GetClosestStep(_sliderValue.z, (int)_twoDimNumberOfSegments.y);
                                    twoDimValue.y = _sliderZZeroPos + (closestStep * (SliderTravelDistance * 2));
                                }

                                break;
                        }
                        break;
                    }
            }

            UpdateSliderPos(closestStep, twoDimValue);
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

            // Ensure the index stays within bounds
            //closestStepIndex = Math.Max(0, Math.Min(closestStepIndex, numberOfSteps - 1));

            // Calculate the closest step value
            float closestStepValue = closestStepIndex * stepSize;

            return closestStepValue;
        }

        #endregion


    }
}
