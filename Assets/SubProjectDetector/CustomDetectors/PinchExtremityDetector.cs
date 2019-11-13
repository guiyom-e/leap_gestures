using UnityEngine;

namespace Leap.Unity
{

    /// <summary>
    /// A basic utility class to aid in creating pinch based actions with thumb and a finger extremity.
    /// Once linked with a HandModelBase, it can
    /// be used to detect pinch gestures that the hand makes.
    /// Inspired from PinchEditor.
    /// </summary>
    public class PinchExtremityDetector : PinchDetector
    {

      private float GetPinchDistance(Hand hand)
        {
            var indexTipPosition = hand.GetMiddle().Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
            var thumbTipPosition = hand.GetThumb().TipPosition.ToVector3();
            return Vector3.Distance(indexTipPosition, thumbTipPosition);
        }

    protected override void ensureUpToDate() {
      if (Time.frameCount == _lastUpdateFrame) {
        return;
      }
      _lastUpdateFrame = Time.frameCount;

      _didChange = false;

      Hand hand = _handModel.GetLeapHand();

      if (hand == null || !_handModel.IsTracked) {
        changeState(false);
        return;
      }

      _distance = GetPinchDistance(hand);
      _rotation = hand.Basis.CalculateRotation();
      _position = ((hand.Fingers[0].TipPosition + hand.Fingers[1].TipPosition) * .5f).ToVector3();

      if (IsActive) {
        if (_distance > DeactivateDistance) {
          changeState(false);
          //return;
        }
      } else {
        if (_distance < ActivateDistance) {
          changeState(true);
        }
      }

      if (IsActive) {
        _lastPosition = _position;
        _lastRotation = _rotation;
        _lastDistance = _distance;
        _lastDirection = _direction;
        _lastNormal = _normal;
      }
      if (ControlsTransform) {
        transform.position = _position;
        transform.rotation = _rotation;
      }
    }
    }
}
