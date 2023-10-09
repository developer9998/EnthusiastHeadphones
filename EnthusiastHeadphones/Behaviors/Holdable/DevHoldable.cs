using GorillaLocomotion;
using UnityEngine;

namespace EnthusiastHeadphones.Behaviors.Holdable
{
    public class DevHoldable : HoldableObject
    {
        public bool
            InHand = false,
            InLeftHand = false,
            PickUp = true,
            DidSwap = false,
            SwappedLeft = true;

        public float
            Distance = 0.13f,
            ThrowForce = 1.75f;

        public virtual void OnGrab(bool isLeft)
        {

        }

        public virtual void OnDrop(bool isLeft)
        {
            if (isLeft)
            {
                InLeftHand = true;
                InHand = false;
                transform.SetParent(null);

                EquipmentInteractor.instance.leftHandHeldEquipment = null;
            }
            else
            {
                InLeftHand = false;
                InHand = false;
                transform.SetParent(null);

                EquipmentInteractor.instance.rightHandHeldEquipment = null;
            }
        }

        public void Update()
        {
            bool leftGrip = ControllerInputPoller.instance.leftControllerGripFloat > Constants.GrabThreshold;
            bool rightGrip = ControllerInputPoller.instance.rightControllerGripFloat > Constants.GrabThreshold;

            DidSwap = (DidSwap && (!SwappedLeft ? !leftGrip : !rightGrip)) ? false : DidSwap;

            bool pickLeft = PickUp && leftGrip && Vector3.Distance(Player.Instance.leftControllerTransform.position, transform.position) < (Distance * Player.Instance.scale) && !InHand && EquipmentInteractor.instance.leftHandHeldEquipment == null && !DidSwap;
            bool swapLeft = InHand && leftGrip && rightGrip && !DidSwap && Vector3.Distance(Player.Instance.leftControllerTransform.position, transform.position) < (Distance * Player.Instance.scale) && !SwappedLeft && EquipmentInteractor.instance.leftHandHeldEquipment == null;
            if (pickLeft || swapLeft)
            {
                DidSwap = swapLeft;
                SwappedLeft = true;
                InLeftHand = true;
                InHand = true;

                transform.SetParent(GorillaTagger.Instance.offlineVRRig.leftHandTransform.parent);
                GorillaTagger.Instance.StartVibration(true, 0.05f, 0.05f);
                EquipmentInteractor.instance.leftHandHeldEquipment = this;
                if (DidSwap) EquipmentInteractor.instance.rightHandHeldEquipment = null;

                OnGrab(true);
            }
            else if (!leftGrip && InHand && InLeftHand || !PickUp && InHand)
            {
                OnDrop(true);
                return;
            }

            bool pickRight = PickUp && rightGrip && Vector3.Distance(Player.Instance.rightControllerTransform.position, transform.position) < (Distance * Player.Instance.scale) && !InHand && EquipmentInteractor.instance.rightHandHeldEquipment == null && !DidSwap;
            bool swapRight = InHand && leftGrip && rightGrip && !DidSwap && Vector3.Distance(Player.Instance.rightControllerTransform.position, transform.position) < (Distance * Player.Instance.scale) && SwappedLeft && EquipmentInteractor.instance.rightHandHeldEquipment == null;
            if (pickRight || swapRight)
            {
                DidSwap = swapRight;
                SwappedLeft = false;

                InLeftHand = false;
                InHand = true;
                transform.SetParent(GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent);

                GorillaTagger.Instance.StartVibration(false, 0.05f, 0.05f);
                EquipmentInteractor.instance.rightHandHeldEquipment = this;
                if (DidSwap) EquipmentInteractor.instance.leftHandHeldEquipment = null;

                OnGrab(false);
            }
            else if (!rightGrip && InHand && !InLeftHand || !PickUp && InHand)
            {
                OnDrop(false);
                return;
            }
        }
    }
}
