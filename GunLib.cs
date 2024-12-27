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
using UnityEngine.InputSystem;

namespace sunnydees_cool_mod
{
    [BepInPlugin("org.gorillatag.SunnyDee.GunLib", "Sunny Dee's GunLibrary", "1.0.3")]

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

        private static VRRig VRRigAimedAt;
        private static float TimeSinceLastRigCheck;
        private static bool AutoAimAtRigs;
        private static bool GunLibEnabeld;
        private static float lastupdatedgunpos;

        public static bool GunTrigger = false;
        public static bool GunShowen = false;

        private static Color colour = Color.cyan;

        private static bool GunHasBeenLocked = false;
        private static bool NewGunDir = false;

        public static void UpdateGun(bool Aoutoaimatplayers)
        {
            AutoAimAtRigs = Aoutoaimatplayers;
            GunTrigger = false;
            if (ControllerInputPoller.instance.rightGrab)
            {
                GunShowen = true;
                GunTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.3;
                UpdateGunPos(true, AutoAimAtRigs, false);
                MakePointer(GunInfo.point, true, colour, Aoutoaimatplayers);
            }
            else if (ControllerInputPoller.instance.leftGrab)
            {
                GunShowen = true;
                GunTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.3;
                UpdateGunPos(false, AutoAimAtRigs, false);
                MakePointer(GunInfo.point, false, colour, Aoutoaimatplayers);
            }else if (Mouse.current.rightButton.isPressed)
            {
                GunShowen = true;
                GunTrigger = Mouse.current.leftButton.isPressed;
                UpdateGunPos(true, AutoAimAtRigs, true);
                MakePointer(GunInfo.point, true, colour, Aoutoaimatplayers);
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


        private static void UpdateGunPos(bool right, bool AimARigs, bool mouse)
        {

            if (!mouse)
            {

                if (right)
                {
                    Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position, (NewGunDir ?  GorillaTagger.Instance.rightHandTransform.forward : -GorillaTagger.Instance.rightHandTransform.up), out var RayInfo, 512f, NoInvisLayerMask());
                    GunInfo = RayInfo;
                }
                else
                {
                    Physics.Raycast(GorillaTagger.Instance.leftHandTransform.position, (NewGunDir ? GorillaTagger.Instance.leftHandTransform.forward : -GorillaTagger.Instance.leftHandTransform.up), out var RayInfo, 512f, NoInvisLayerMask());
                    GunInfo = RayInfo;
                }
            }
            else
            {
                Camera TPC;
                try
                {
                    TPC = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera").GetComponent<Camera>();
                }
                catch
                {
                    TPC = GameObject.Find("Shoulder Camera").GetComponent<Camera>();
                }

                Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                Physics.Raycast(ray, out var RayInfo, 512f, NoInvisLayerMask());
                GunInfo = RayInfo;
            }


            lastupdatedgunpos = Time.time;
        }

        private static int NoInvisLayerMask()
        {
            return ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics | 1 << GorillaParticle);
        }

        public static void SetGunLibColor(Color colour1)
        {
            colour = colour1;
        }

        public static void SetGunLibDir(bool NewDir)
        {
            NewGunDir = NewDir;
        }

        private static void MakePointer(Vector3 point, bool right, Color colour, bool AimARigs)
        {
            Color scolour = colour;
            if (GunTrigger)
            {
                scolour = Color.green;
            }

            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            pointer.GetComponent<Renderer>().material.color = colour;
            if (AimARigs && RigAimedAt() != null)
            {
                pointer.transform.position = RigAimedAt().transform.position;
            }
            else
            {
                pointer.transform.position = point;
            }

            GameObject.Destroy(pointer.GetComponent<BoxCollider>());
            GameObject.Destroy(pointer.GetComponent<Rigidbody>());
            GameObject.Destroy(pointer.GetComponent<Collider>());

            GameObject gameObject = new GameObject("Line");
            LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startColor = scolour;
            lineRenderer.endColor = colour - new Color(5, 5, 5);
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.020f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            if (right)
            {
                lineRenderer.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
            }
            else
            {
                lineRenderer.SetPosition(0, GorillaTagger.Instance.leftHandTransform.position);
            }
            lineRenderer.SetPosition(1, pointer.transform.position);
            lineRenderer.material.shader = Shader.Find("GUI/Text Shader");
            UnityEngine.Object.Destroy(gameObject, Time.deltaTime);
            UnityEngine.Object.Destroy(pointer, Time.deltaTime);
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

        public static PhotonView PhotonViewAimedAt()
        {
            return GetPhotonViewFromVRRig(RigAimedAt());
        }

        public static NetworkView NetworkedViewAimedAt()
        {
            return GetNetworkViewFromVRRig(RigAimedAt());
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

        private static Player NetPlayerToPlayer(NetPlayer p)
        {
            return p.GetPlayerRef();
        }

        private static NetPlayer GetPlayerFromVRRig(VRRig p)
        {
            return p.Creator;
        }
        private static PhotonView GetPhotonViewFromVRRig(VRRig p)
        {
            return (PhotonView)Traverse.Create(p).Field("photonView").GetValue();
        }
        private static NetworkView GetNetworkViewFromVRRig(VRRig p)
        {
            return (NetworkView)Traverse.Create(p).Field("netView").GetValue();
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
