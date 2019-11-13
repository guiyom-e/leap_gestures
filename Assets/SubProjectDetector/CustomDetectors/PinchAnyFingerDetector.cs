using UnityEngine;
using Leap.Unity.Attributes;
using UnityEngine.Serialization;
using Leap.Unity;
using Leap;
using System.Collections.Generic;

public class PinchAnyFingerDetector : AbstractHoldDetector
{

    public enum FingerUsed {Used, NotUsed};


    [Tooltip("The distance at which to enter the pinching state")]
    [Header("Distance Settings")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_activatePinchDist")]
    public float ActivateDistance = .03f; //meters
    [Tooltip("The distance at which to leave the pinching state")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_deactivatePinchDist")]
    public float DeactivateDistance = .04f; //meters

    [Header("Finger States")]
    [Tooltip("Required state of the thumb.")]
    private FingerUsed Thumb = FingerUsed.Used;

    [Tooltip("Required state of the index finger.")]
    public FingerUsed Index = FingerUsed.Used;
    /** The required middle finger state. */
    [Tooltip("Required state of the middle finger.")]
    public FingerUsed Middle = FingerUsed.NotUsed;
    /** The required ring finger state. */
    [Tooltip("Required state of the ring finger.")]
    public FingerUsed Ring = FingerUsed.NotUsed;
    /** The required pinky finger state. */
    [Tooltip("Required state of the little finger.")]
    public FingerUsed Pinky = FingerUsed.NotUsed;

    public bool IsPinching { get { return this.IsHolding; } }
    public bool DidStartPinch { get { return this.DidStartHold; } }
    public bool DidEndPinch { get { return this.DidRelease; } }

    protected bool _isPinching = false;

    protected float _lastPinchTime = 0.0f;
    protected float _lastUnpinchTime = 0.0f;

    protected Vector3 _pinchPos;
    protected Quaternion _pinchRotation;

    private List<Finger> fingersUsed = new List<Finger>();

    protected virtual void OnValidate()
    {
        ActivateDistance = Mathf.Max(0, ActivateDistance);
        DeactivateDistance = Mathf.Max(0, DeactivateDistance);

        //Activate value cannot be less than deactivate value
        if (DeactivateDistance < ActivateDistance)
        {
            DeactivateDistance = ActivateDistance;
        }
        checkFingersSelected();
    }

    private void checkFingersSelected()
    {
        Hand hand = _handModel.GetLeapHand();

        if (fingersUsed == null || fingersUsed.ToArray().Length > 0)
        {
            fingersUsed = new List<Finger>();
        }
        if (hand != null)
        {
            if (Index.Equals(FingerUsed.Used)) { fingersUsed.Add(hand.Fingers[(int)Finger.FingerType.TYPE_INDEX]); };
            if (Middle.Equals(FingerUsed.Used)) { fingersUsed.Add(hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE]); }
            if (Ring.Equals(FingerUsed.Used)) { fingersUsed.Add(hand.Fingers[(int)Finger.FingerType.TYPE_RING]); }
            if (Pinky.Equals(FingerUsed.Used)) { fingersUsed.Add(hand.Fingers[(int)Finger.FingerType.TYPE_PINKY]); }
        }
    }

    private float GetPinchDistance(Hand hand)
    {
        checkFingersSelected();
        List<float> fingerDistanceToThumb = new List<float>();
        var thumbTipPosition = hand.GetThumb().TipPosition.ToVector3();

        foreach(Finger finger in fingersUsed)
        {
            fingerDistanceToThumb.Add(Vector3.Distance(finger.TipPosition.ToVector3(), thumbTipPosition));
        };
        return Mathf.Max(fingerDistanceToThumb.ToArray());
    }

    protected override void ensureUpToDate()
    {
        if (Time.frameCount == _lastUpdateFrame)
        {
            return;
        }
        _lastUpdateFrame = Time.frameCount;

        _didChange = false;

        Hand hand = _handModel.GetLeapHand();

        if (hand == null || !_handModel.IsTracked)
        {
            changeState(false);
            return;
        }

        _distance = GetPinchDistance(hand);
        _rotation = hand.Basis.CalculateRotation();
        _position = getCenter();

        if (IsActive)
        {
            if (_distance > DeactivateDistance)
            {
                changeState(false);
                //return;
            }
        }
        else
        {
            if (_distance < ActivateDistance)
            {
                changeState(true);
            }
        }

        if (IsActive)
        {
            _lastPosition = _position;
            _lastRotation = _rotation;
            _lastDistance = _distance;
            _lastDirection = _direction;
            _lastNormal = _normal;
        }
        if (ControlsTransform)
        {
            transform.position = _position;
            transform.rotation = _rotation;
        }
    }

    public Vector3 getCenter()
    {
        Hand hand = _handModel.GetLeapHand();
        Vector sum = hand.GetThumb().Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;
        foreach (Finger finger in fingersUsed)
        {
            sum += finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;
        }
        return (sum / (fingersUsed.ToArray() == null ? 1 : (1 + fingersUsed.ToArray().Length))).ToVector3();
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        checkFingersSelected();
        if (ShowGizmos && _handModel != null && _handModel.IsTracked)
        {
            Color centerColor = Color.clear;
            Vector3 centerPosition = Vector3.zero;
            Quaternion circleRotation = Quaternion.identity;
            if (IsHolding)
            {
                centerColor = Color.green;
                centerPosition = Position;
                circleRotation = Rotation;
            }
            else
            {
                Hand hand = _handModel.GetLeapHand();
                if (hand != null)
                {
                    centerColor = Color.red;
                    centerPosition = getCenter();
                    circleRotation = hand.Basis.CalculateRotation();
                }
            }
            Vector3 axis;
            float angle;
            circleRotation.ToAngleAxis(out angle, out axis);
            Utils.DrawCircle(centerPosition, axis, ActivateDistance / 2, centerColor);
            Utils.DrawCircle(centerPosition, axis, DeactivateDistance / 2, Color.blue);
        }
    }
#endif
}