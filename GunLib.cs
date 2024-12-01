﻿using BepInEx;
using Photon.Pun;
using PlayFab.ExperimentationModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace sunnydees_cool_mod
{
    [BepInPlugin("org.gorillatag.SunnyDee.GunLib", "Sunny Dee's GunLibrary", "1.0.1")]

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

        public static void UpdateGun(bool Aoutoaimatplayers)
        {
            AutoAimAtRigs = Aoutoaimatplayers;
            GunTrigger = false;
            if (ControllerInputPoller.instance.rightGrab)
            {
                UpdateGunPos(true, AutoAimAtRigs);
                MakePointer(GunInfo.point, true, colour, Aoutoaimatplayers);
                GunTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.3;
            }
            else if (ControllerInputPoller.instance.leftGrab)
            {
                UpdateGunPos(false, AutoAimAtRigs);
                MakePointer(GunInfo.point, false, colour, Aoutoaimatplayers);
                GunTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.3;
            }
            else
            {
                GunTrigger = false;
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
            if (AimARigs)
            {
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    rig.transform.localScale = new Vector3(3, 3, 3);
                }
            }

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

            if (AutoAimAtRigs)
            {
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    rig.transform.localScale = new Vector3(1, 1, 1);
                }
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
            lineRenderer.startColor = colour;
            lineRenderer.endColor = colour - new Color(5, 5, 5);
            lineRenderer.startWidth = 0.025f;
            lineRenderer.endWidth = 0.025f;
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

        public static VRRig GetVRRigFromPlayer(Photon.Realtime.Player p)
        {
            return GorillaGameManager.instance.FindPlayerVRRig(p);
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