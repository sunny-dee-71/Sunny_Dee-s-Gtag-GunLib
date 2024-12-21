using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using PlayFab.ExperimentationModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace sunnydees_cool_mod
{
    [BepInPlugin("org.gorillatag.SunnyDee.GunLib", "Sunny Dee's GunLibrary", "1.0.2")]

    public class GunLib : BaseUnityPlugin
    {
        private static int TransparentFX = LayerMask.NameToLayer("TransparentFX");
        private static int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        private static int Zone = LayerMask.NameToLayer("Zone");
        private static int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
        private static int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
        private static int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
        private static int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

        public static RaycastHit GunInfo;

        public static VRRig VRRigAimedAt;
        public static float TimeSinceLastRigCheck;
        public static bool AutoAimAtRigs;
        public static bool GunLibEnabeld;
        public static float lastupdatedgunpos;

        public static bool GunTrigger = false;
        public static bool GunShowen = false;

        public static Color colour = Color.cyan;

        public static bool GunHasBeenLocked { get; private set; }

        public static void UpdateGun(bool Aoutoaimatplayers)
        {
            AutoAimAtRigs = Aoutoaimatplayers;
            GunTrigger = false;
            if (ControllerInputPoller.instance.rightGrab)
            {
                GunShowen = true;
                GunTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.3;
                UpdateGunPos(true, AutoAimAtRigs);
                MakePointer(GunInfo.point, true, colour, Aoutoaimatplayers);
            }
            else if (ControllerInputPoller.instance.leftGrab)
            {
                GunShowen = true;
                GunTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.3;
                UpdateGunPos(false, AutoAimAtRigs);
                MakePointer(GunInfo.point, false, colour, Aoutoaimatplayers);
            }
            else
            {
                GunShowen = false;
                GunTrigger = false;
                GunHasBeenLocked = false;
            }
        }


        public static bool GunAimedAtPlayer()
        {
            if (RigAimedAt() != null)
            {
                return true;
            }
            return false;
        }


        public static void UpdateGunPos(bool right,bool AimARigs)
        {

            if (right)
            {
                Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position, -GorillaTagger.Instance.rightHandTransform.up, out var RayInfo, 512f, NoInvisLayerMask());
                GunInfo = RayInfo;
            }
            else
            {
                Physics.Raycast(GorillaTagger.Instance.leftHandTransform.position, -GorillaTagger.Instance.leftHandTransform.up, out var RayInfo, 512f, NoInvisLayerMask());
                GunInfo = RayInfo;
            }


            lastupdatedgunpos = Time.time;
        }

        public static int NoInvisLayerMask()
        {
            return ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics | 1 << GorillaParticle);
        }

        public static void SetGunLibColor(Color colour1)
        {
            colour = colour1;
        }

        public static void MakePointer(Vector3 point, bool right, Color colour, bool AimARigs)
        {
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            pointer.GetComponent<Renderer>().material.color = colour;

            Vector3 targetPosition = point;
            if (AimARigs && RigAimedAt() != null)
            {
                targetPosition = RigAimedAt().transform.position;
            }

            pointer.transform.position = targetPosition;

            GameObject.Destroy(pointer.GetComponent<BoxCollider>());
            GameObject.Destroy(pointer.GetComponent<Rigidbody>());
            GameObject.Destroy(pointer.GetComponent<Collider>());

            GameObject gameObject = new GameObject("Line");
            LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startColor = colour;
            lineRenderer.endColor = colour - new Color(5, 5, 5);
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.020f;
            lineRenderer.positionCount = 20;
            lineRenderer.useWorldSpace = true;

            Vector3 handPosition = right ? GorillaTagger.Instance.rightHandTransform.position : GorillaTagger.Instance.leftHandTransform.position;
            Vector3 handForward = right ? GorillaTagger.Instance.rightHandTransform.forward : GorillaTagger.Instance.leftHandTransform.forward;

            Vector3 midPoint1 = handPosition + handForward * 1.0f;
            Vector3 midPoint2;

            if (AimARigs && RigAimedAt() != null)
            {
                Vector3 toTarget = (targetPosition - handPosition).normalized;
                Vector3 perpendicular = Vector3.Cross(toTarget, Vector3.up).normalized * 0.5f;
                midPoint2 = (handPosition + targetPosition) / 2 + perpendicular;
            }
            else
            {
                midPoint2 = (handPosition + targetPosition) / 2 + Vector3.up * 0.5f;
            }

            lineRenderer.SetPosition(0, handPosition);
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, targetPosition);

            for (int i = 1; i < lineRenderer.positionCount - 1; i++)
            {
                float t = i / (float)(lineRenderer.positionCount - 1);
                Vector3 curvePoint = CubicBezierCurve(handPosition, midPoint1, midPoint2, targetPosition, t);
                lineRenderer.SetPosition(i, curvePoint);
            }

            lineRenderer.material.shader = Shader.Find("GUI/Text Shader");
            UnityEngine.Object.Destroy(gameObject, Time.deltaTime);
            UnityEngine.Object.Destroy(pointer, Time.deltaTime);
        }

        private static Vector3 CubicBezierCurve(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * start) + (3 * uu * t * control1) + (3 * u * tt * control2) + (ttt * end);
        }



        public static VRRig RigAimedAt()
        {
            if (GunTrigger && VRRigAimedAt != null)
            {
                GunHasBeenLocked = true;
                return VRRigAimedAt;
            }

            if (GunHasBeenLocked && VRRigAimedAt != null)
            {
                return VRRigAimedAt;
            }

            if (TimeSinceLastRigCheck > 10)
            {
                TimeSinceLastRigCheck = 0;
                float nearestDistance = Mathf.Infinity;
                VRRig nearestBody = VRRigAimedAt;

                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    float distance = Vector3.Distance(GunInfo.point, rig.transform.position);

                    if (distance < nearestDistance && distance < 5f && rig != GorillaTagger.Instance.offlineVRRig)
                    {
                        nearestDistance = distance;
                        nearestBody = rig;
                    }

                }
                if (nearestDistance > 5f)
                {
                    VRRigAimedAt = null;
                }
                else
                {
                    VRRigAimedAt = nearestBody;
                }
                return VRRigAimedAt;
            }
            else
            {
                TimeSinceLastRigCheck += 1;
                return VRRigAimedAt;
            }
        }

        public static Player NonNetPlayerAimedAt()
        {
            return NetPlayerToPlayer(GetPlayerFromVRRig(RigAimedAt()));
        }

        public static NetPlayer NetPlayerAimedAt()
        {
            return GetPlayerFromVRRig(RigAimedAt());
        }

        public static PhotonView PNViewAimedAt()
        {
            return GetPhotonViewFromVRRig(RigAimedAt());
        }


        public static Vector3 GunPos()
        {
            if (AutoAimAtRigs && RigAimedAt() != null)
            {
                return RigAimedAt().transform.position;
            }
            else
            {
                return GunInfo.point;
            }
        }

        public static Player NetPlayerToPlayer(NetPlayer p) //Thanks IIDK
        {
            return p.GetPlayerRef();
        }

        public static NetPlayer GetPlayerFromVRRig(VRRig p)
        {
            return p.Creator;
        }
        public static PhotonView GetPhotonViewFromVRRig(VRRig p)
        {
            return (PhotonView)Traverse.Create(p).Field("photonView").GetValue();
        }


        public static bool GunLibEnabled()
        {
            if (Time.time > lastupdatedgunpos + 1)
            {
                GunLibEnabeld = false;
            }
            else
            {
                GunLibEnabeld = true;
            }
            return GunLibEnabeld;
        }
    }
}
