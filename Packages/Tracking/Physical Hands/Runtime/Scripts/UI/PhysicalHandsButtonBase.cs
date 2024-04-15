/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandsButtonBase : MonoBehaviour
    {
        [SerializeField] 
        private GameObject _pressableObject;
        [SerializeField]
        bool _automaticTravelDistance = true;

        private float _buttonTravelDistanceLocal;
        [SerializeField, Min(0.001f)]
        private float _buttonTravelDistance = 0.01f;
        [SerializeField]
        private float _buttonTravelOffset = 0;
        [SerializeField] 
        protected bool _canBePressedByObjects = false;
        [SerializeField] 
        private ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;

        private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.5f;
        private bool _isButtonPressed = false;
        private bool _contactHandPressing = false;
        private bool _leftHandContacting = false;
        private bool _rightHandContacting = false;
        private Rigidbody _pressableObjectRB = null;
        private Rigidbody _rigidbody = null;
        private Vector3 _initialButtonPosition = Vector3.zero;
        private List<GameObject> _objectsContactingButton = new List<GameObject>();

        protected ConfigurableJoint _configurableJoint;
        private PhysicalHandsButtonHelper _buttonHelper;
        [SerializeField]
        private float springValue = 0;
        [SerializeField]
        private float damperValue = 0;
        [SerializeField]
        private float maxForceValue = Mathf.Infinity;
        [SerializeField]
        private float bouncinessValue = 0;
        private enum ButtonPreset
        {
            Standard = 0,
            Soft = 1,
            Bouncy = 2,
            Custom = 3,
        }
        [SerializeField]
        private ButtonPreset _buttonPreset = ButtonPreset.Standard;

        #region Events
        [SerializeField]
        public UnityEvent OnButtonPressed;
        [SerializeField]
        public UnityEvent OnButtonUnPressed;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandContact;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandContactExit;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandHover;
        [SerializeField]
        public UnityEvent<ContactHand> OnHandHoverExit;
        #endregion

        #region public getters
        public bool IsPressed
        {
            get
            {
                return _isButtonPressed;
            }
        }
        #endregion

        public void UpdateInspectorValues()
        {
            if (_automaticTravelDistance && _pressableObject != null)
            {
                float buttonTravelOffset = 0;

                if (TryGetComponent<MeshFilter>(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
                {
                    buttonTravelOffset += meshFilter.sharedMesh.bounds.extents.y;
                }

                if (_pressableObject.TryGetComponent<MeshFilter>(out meshFilter) && meshFilter.sharedMesh != null)
                {
                    buttonTravelOffset += (meshFilter.sharedMesh.bounds.extents.y * _pressableObject.transform.localScale.y);
                }

                _buttonTravelDistance = (_pressableObject.transform.localPosition.y - buttonTravelOffset) * transform.lossyScale.y;
            }

            switch (_buttonPreset)
            {
                // Standard button preset with no spring, damper, or force limits
                case ButtonPreset.Standard:
                    springValue = 10;
                    damperValue = 0;
                    maxForceValue = 5;
                    bouncinessValue = 0f;
                    break;
                // Soft button preset with low spring, damper, and force limits
                case ButtonPreset.Soft:
                    springValue = 1;
                    damperValue = 10;
                    maxForceValue = 1;
                    bouncinessValue = 0f;
                    break;
                // Bouncy button preset with high spring limit and no damper or force limits
                case ButtonPreset.Bouncy:
                    springValue = 1;
                    damperValue = 0;
                    maxForceValue = 5;
                    bouncinessValue = 0.6f;
                    break;
            }
        }

        /// <summary>
        /// Updates the editor values including button travel distance and button presets.
        /// </summary>
        private void UpdateAutomaticValues()
        {
            // Update the button preset values
            UpdateButtonPreset();

            UpdateInspectorValues();

            // Check if automatic travel distance calculation is enabled and a pressable object is assigned
            if (_automaticTravelDistance && _pressableObject != null)
            {
                _buttonTravelOffset = 0;

                if (TryGetComponent<MeshFilter>(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
                {
                    _buttonTravelOffset += meshFilter.sharedMesh.bounds.extents.y;
                }

                if (_pressableObject.TryGetComponent<MeshFilter>(out meshFilter) && meshFilter.sharedMesh != null)
                {
                    _buttonTravelOffset += (meshFilter.sharedMesh.bounds.extents.y * _pressableObject.transform.localScale.y);
                }

                _buttonTravelDistanceLocal = _pressableObject.transform.localPosition.y - _buttonTravelOffset;
                _buttonTravelDistance = _buttonTravelDistanceLocal * transform.lossyScale.y;

                if (_buttonTravelDistance < 0)
                {
                    // Log a warning if the button travel distance is negative
                    Debug.Log("Button Travel distance is negative, please ensure the button moves on the positive y axis");
                }
            }
            else
            {
                // If automatic travel distance calculation is disabled,
                // calculate the button travel offset based on the initial button position and the previously calculated travel distance
                _buttonTravelDistanceLocal = _buttonTravelDistance / transform.lossyScale.y;
                _buttonTravelOffset = _initialButtonPosition.y - _buttonTravelDistanceLocal;
            }
        }

        /// <summary>
        /// Updates the button preset values based on the selected button preset type.
        /// </summary>
        private void UpdateButtonPreset()
        {
            //To allow runtime changing of button mode
            if (_configurableJoint != null)
            {
                // Configure spring parameters
                _configurableJoint.yDrive = new JointDrive
                {
                    positionSpring = springValue,
                    positionDamper = damperValue,
                    maximumForce = maxForceValue
                };

                _configurableJoint.linearLimit = new SoftJointLimit
                {
                    limit = (float)(_buttonTravelDistance / 2),
                    bounciness = bouncinessValue
                };
            }
        }

        protected virtual void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the button.
        /// </summary>
        private void Initialize()
        {
            if (_pressableObject == null)
            {
                Debug.LogError("Pressable object not assigned. Please assign one to use the button.");
                enabled = false;
                return;
            }

            _initialButtonPosition = _pressableObject.transform.localPosition;

            _rigidbody = GetComponent<Rigidbody>();

            UpdateAutomaticValues();

            SetUpPressableObject();
            SetUpSpringJoint();
        }

        /// <summary>
        /// Set up the pressable object.
        /// </summary>
        private void SetUpPressableObject()
        {
            // Ensure the pressable object has a Rigidbody component
            if (!_pressableObject.TryGetComponent<Rigidbody>(out _pressableObjectRB))
            {
                _pressableObjectRB = _pressableObject.AddComponent<Rigidbody>();
                _pressableObjectRB.useGravity = false;
            }

            // Ensure the pressable object has an IgnorePhysicalHands component
            if (!_pressableObject.TryGetComponent<IgnorePhysicalHands>(out IgnorePhysicalHands ignorePhysHands))
            {
                ignorePhysHands = _pressableObject.AddComponent<IgnorePhysicalHands>();
                ignorePhysHands.DisableAllGrabbing = true;
                ignorePhysHands.DisableAllHandCollisions = false;
                ignorePhysHands.DisableCollisionOnChildObjects = false;
            }

            // Ensure the pressable object has a PhysicalHandsButtonHelper component
            if (!_pressableObject.TryGetComponent<PhysicalHandsButtonHelper>(out _buttonHelper))
            {
                _buttonHelper = _pressableObject.AddComponent<PhysicalHandsButtonHelper>();
            }

            // Subscribe to events from the button helper
            _buttonHelper._onHandContact -= OnHandContactPO;
            _buttonHelper._onHandContactExit -= OnHandContactExitPO;
            _buttonHelper._onHandHover -= OnHandHoverPO;
            _buttonHelper._onHandHoverExit -= OnHandHoverExitPO;
            _buttonHelper._onCollisionEnter -= OnCollisionPO;
            _buttonHelper._onCollisionExit -= OnCollisionExitPO;

            _buttonHelper._onHandContact += OnHandContactPO;
            _buttonHelper._onHandContactExit += OnHandContactExitPO;
            _buttonHelper._onHandHover += OnHandHoverPO;
            _buttonHelper._onHandHoverExit += OnHandHoverExitPO;
            _buttonHelper._onCollisionEnter += OnCollisionPO;
            _buttonHelper._onCollisionExit += OnCollisionExitPO;
        }

        /// <summary>
        /// Set up the spring joint for the button.
        /// </summary>
        private void SetUpSpringJoint()
        {
            if (!_pressableObject.TryGetComponent<ConfigurableJoint>(out _configurableJoint))
            {
                _configurableJoint = _pressableObject.AddComponent<ConfigurableJoint>();
            }

            _configurableJoint.targetPosition = new Vector3(0, -_buttonTravelDistanceLocal, 0);

            // Connect the button to the parent object with a spring joint
            _configurableJoint.connectedBody = _rigidbody;

            // Configure spring parameters
            _configurableJoint.yDrive = new JointDrive
            {
                positionSpring = springValue,
                positionDamper = damperValue,
                maximumForce = maxForceValue
            };

            // Lock and limit motion axes
            _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;

            // Adjust anchor position for button travel distance
            _configurableJoint.anchor = Vector3.zero;
            _configurableJoint.autoConfigureConnectedAnchor = false;
            _configurableJoint.connectedAnchor = new Vector3(0, _buttonTravelOffset + (_buttonTravelDistanceLocal / 2) ,0);

            // Set linear limit for button travel
            _configurableJoint.linearLimit = new SoftJointLimit
            {
                limit = (float)(_buttonTravelDistance / 2),
                bounciness = bouncinessValue
            };
        }

        private void FixedUpdate()
        {
            float distance = Mathf.Abs(_pressableObject.transform.localPosition.y - _initialButtonPosition.y);

            // Check if the button should be pressed
            if (!_isButtonPressed && distance >= (_buttonTravelDistanceLocal * 0.95f) &&
                (_contactHandPressing || (_canBePressedByObjects && _objectsContactingButton.Count > 0)))
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            // Check if the button should be released
            if (_isButtonPressed && distance < _buttonTravelDistanceLocal * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
                ButtonUnpressed();
            }
        }

        /// <summary>
        /// Invoke the button pressed event.
        /// </summary>
        protected virtual void ButtonPressed()
        {
            OnButtonPressed?.Invoke();
        }

        /// <summary>
        /// Invoke the button released event.
        /// </summary>
        protected virtual void ButtonUnpressed()
        {
            OnButtonUnPressed?.Invoke();
        }

        /// <summary>
        /// Handle hand contact with the pressable object.
        /// </summary>
        protected virtual void OnHandContactPO(ContactHand hand)
        {
            OnHandContact?.Invoke(hand);

            if (hand != null)
            {
                if (hand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = true;
                }
                else if (hand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = true;
                }
            }

            _contactHandPressing = GetChosenHandInContact();
        }

        /// <summary>
        /// Handles actions when a hand exits contact with the pressable object.
        /// </summary>
        /// <param name="hand">The hand exiting contact.</param>
        protected virtual void OnHandContactExitPO(ContactHand hand)
        {
            // Invoke event for hand contact exit
            OnHandContactExit?.Invoke(hand);

            // Update hand contact flags
            if (hand != null)
            {
                if (hand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = false;
                }
                else if (hand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = false;
                }
            }
            else
            {
                // Reset hand contact flags if hand is null
                _leftHandContacting = false;
                _rightHandContacting = false;
            }

            // Update overall hand contact flag
            _contactHandPressing = GetChosenHandInContact();
        }

        /// <summary>
        /// Handles actions when a hand exits hovering over the pressable object.
        /// </summary>
        /// <param name="hand">The hand exiting hover.</param>
        protected virtual void OnHandHoverExitPO(ContactHand hand)
        {
            // Invoke event for hand hover exit
            OnHandHoverExit?.Invoke(hand);
        }

        /// <summary>
        /// Handles actions when a hand hovers over the pressable object.
        /// </summary>
        /// <param name="hand">The hand hovering.</param>
        protected virtual void OnHandHoverPO(ContactHand hand)
        {
            // Invoke event for hand hover
            OnHandHover?.Invoke(hand);
        }

        protected virtual void OnCollisionPO(Collision collision)
        {
            if (!collision.transform.root.GetComponent<PhysicalHandsManager>())
            {
                _objectsContactingButton.Add(collision.gameObject);
            }
        }

        protected virtual void OnCollisionExitPO(Collision collision)
        {
            _objectsContactingButton.Remove(collision.gameObject);
        }

        /// <summary>
        /// Determines whether any chosen hand is in contact with the pressable object.
        /// </summary>
        /// <returns>True if a chosen hand is in contact; otherwise, false.</returns>
        protected bool GetChosenHandInContact()
        {
            switch (_whichHandCanPressButton)
            {
                case ChiralitySelection.LEFT:
                    return _leftHandContacting;
                case ChiralitySelection.RIGHT:
                    return _rightHandContacting;
                case ChiralitySelection.BOTH:
                    return _rightHandContacting || _leftHandContacting;
                default:
                    return false;
            }
        }
    }
}