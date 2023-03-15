using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    public class NamedListAttribute : PropertyAttribute
    {
        public readonly string[] names;
        public NamedListAttribute(string[] names) { this.names = names; }
    }

    [CustomPropertyDrawer(typeof(NamedListAttribute))]
    public class NamedArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            
            try
            {
                int pos = int.Parse(property.propertyPath.Split('[', ']')[1]);
                EditorGUI.PropertyField(rect, property, new GUIContent(((NamedListAttribute)attribute).names[pos]), true);
            }
            catch
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }
        }
    }

    [CreateAssetMenu(fileName = "HandPose", menuName = "ScriptableObjects/HandPose")]
    public class HandPoseScriptableObject : ScriptableObject
    {
        [HideInInspector]
        public bool DetectThumb = true;
        [HideInInspector]
        public bool DetectIndex = true;
        [HideInInspector]
        public bool DetectMiddle = true;
        [HideInInspector]
        public bool DetectRing = true;
        [HideInInspector]
        public bool DetectPinky = true;

        private List<int> fingerIndexesToCheck = new List<int>();

        public List<int> GetFingerIndexesToCheck()
        {
            ApplyFingersToUse();
            return fingerIndexesToCheck;
        }

        [SerializeField]
        private Hand serializedHand;
        public Hand GetSerializedHand()
        {
            return serializedHand;
        }

        [SerializeField]
        private Hand mirroredHand;
        public Hand GetMirroredHand()
        {
            return mirroredHand;
        }

        #region Finger Thresholds

        private static Vector2 defaultRotation = new Vector2(15, 15);

        [HideInInspector]
        public Vector2 globalRotation = new Vector2(15,15);
        public List<Vector2>[] fingerRotationThresholds = new List<Vector2>[5];

        [Header("Finger Rotational Thresholds")]
        [SerializeField]
        [NamedListAttribute(new string[] { "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> ThumbJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> IndexJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> MiddleJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> RingJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation };

        [SerializeField]
        [NamedListAttribute(new string[] { "Proximal", "Intermediate", "Distal" })]
        private List<Vector2> PinkieJointRotation = new List<Vector2>() { defaultRotation, defaultRotation, defaultRotation };

        /// <summary>
        /// The distance a bone must move away from being detected before the pose is no longer enabled.
        /// This means that users cannot hover just on the edge of a detection and cause it to send rapid detections while straying still.
        /// E.g. Detection threshold is 15 degrees, so when the user gets within 15 degrees, detection will occur.
        /// Hysteresis threshold is 5 so the user need to move 20 degrees from the pose before the detection will drop.
        /// </summary>
        [SerializeField, Tooltip("When a joint is within the rotation threshold, how many degrees away from the original threshold " +
            "must the user move to stop the detection of each joint for the pose. This helps to avoid flickering detection when on the boundaries of thresholds")]
        private float _hysteresisThreshold = 5;

        public float GetHysteresisThreshold()
        {
            return _hysteresisThreshold;
        }

        #endregion

        public void SaveHandPose(Hand handToSerialise)
        {
            serializedHand = handToSerialise;
            ApplyThresholds();
        }

        void MirrorHand(ref Hand hand)
        {
            LeapTransform leapTransform = new LeapTransform(Vector3.zero, Quaternion.Euler(Vector3.zero));
            leapTransform.MirrorX();
            hand.Transform(leapTransform);
            hand.IsLeft = !hand.IsLeft;
            return;
        }

        public Vector2 GetBoneRotationthreshold(int fingerNum, int boneNum)
        {
            ApplyThresholds();

            if (fingerRotationThresholds.Count() > 0)
            {
                // if there is no metacarpal, reduce the index
                if(fingerRotationThresholds[fingerNum].Count == 3)
                {
                    boneNum -= 1;
                }

                return fingerRotationThresholds[fingerNum].ElementAt(boneNum);
            }
            else
            {
                return new Vector3 ( 0f, 0f );
            }
        }

        private void ApplyThresholds()
        {
            fingerRotationThresholds[0] = ThumbJointRotation;
            fingerRotationThresholds[1] = IndexJointRotation;
            fingerRotationThresholds[2] = MiddleJointRotation;
            fingerRotationThresholds[3] = RingJointRotation;
            fingerRotationThresholds[4] = PinkieJointRotation;
        }

        private void OnValidate()
        {
            mirroredHand = mirroredHand.CopyFrom(serializedHand);
            MirrorHand(ref mirroredHand);

            ApplyFingersToUse();
            ApplyThresholds();
        }

        private void ApplyFingersToUse()
        {
            fingerIndexesToCheck.Clear();
            if (DetectThumb) { fingerIndexesToCheck.Add(0); }
            if (DetectIndex) { fingerIndexesToCheck.Add(1); }
            if (DetectMiddle) { fingerIndexesToCheck.Add(2); }
            if (DetectRing) { fingerIndexesToCheck.Add(3); }
            if (DetectPinky) { fingerIndexesToCheck.Add(4); }
        }

        public void SetAllBoneThresholds(float threshold)
        {
            Vector2 newRotation = new Vector2(threshold, threshold);

            ThumbJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation };
            IndexJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation };
            MiddleJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation };
            RingJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation };
            PinkieJointRotation = new List<Vector2>() { newRotation, newRotation, newRotation };

            ApplyThresholds();
        }
    }
}