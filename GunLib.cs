using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace sunnydees_cool_mod
{
    [BepInPlugin("org.gorillatag.SunnyDee.GunLib", "Sunny Dee's GunLibrary", "1.0.3")]

    public class GunLib : BaseUnityPlugin
    {
        public static GunLib Instance { get; private set; }
        
        private static int mask =>
            LayerMask.GetMask("Default", "Gorilla Object", "NoMirror", "Gorilla Tag Collider");

        private static RaycastHit _gunInfo;

        private static VRRig _vrRigAimedAt;
        private static float _timeSinceLastRigCheck;
        private static float _lastGunPosUpdateTime;
        
        private static Color _defaultColor = Color.white;
        private static Color _triggerColor = Color.green;
        
        private static bool _autoAimAtRigs;
        private static bool _gunLibEnabled;
        private static bool _gunHasBeenLocked;
        
        public static bool gunTrigger;

        private GameObject _pointer;
        private LineRenderer _line;
        private Renderer _pointerRenderer;

        public Camera tpc;

        private void Start()
        {
            Instance = this;
            GorillaTagger.OnPlayerSpawned(PlayerSpawned);
        }

        private void PlayerSpawned()
        {
            _pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pointer.transform.localScale = Vector3.one * 0.125f;
            
            _pointerRenderer = _pointer.GetComponent<Renderer>();
            _pointerRenderer.material.color = Color.white;
            _pointerRenderer.material.shader = Shader.Find("GorillaTag/UberShader");
            Destroy(_pointer.GetComponent<Collider>());
            Destroy(_pointer.GetComponent<Rigidbody>());
            Destroy(_pointer.GetComponent<Collider>());
            
            _line = new GameObject("GunLib Line").AddComponent<LineRenderer>();
            _line.startWidth = 0.007f;
            _line.endWidth = 0.007f;
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.material.shader = Shader.Find("GUI/Text Shader");
            _line.gameObject.SetActive(false);

            tpc = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>();
        }

        public void Gun(Action<RaycastHit> trigger, bool aimAtPlayers)
        {
            UpdateGun(aimAtPlayers);
            if (gunTrigger)
            {
                trigger(_gunInfo);
            }
        }

        private void UpdateGun(bool aimAtPlayers)
        {
            _autoAimAtRigs = aimAtPlayers;
            gunTrigger = false;
            
            if (ControllerInputPoller.instance.rightGrab && XRSettings.isDeviceActive)
            {
                gunTrigger = ControllerInputPoller.instance.rightControllerIndexFloat > 0.3;
                UpdateGunPos(true, _autoAimAtRigs, false);
                Cast(_gunInfo.point, true, aimAtPlayers);
            }
            else if (ControllerInputPoller.instance.leftGrab && XRSettings.isDeviceActive)
            {
                gunTrigger = ControllerInputPoller.instance.leftControllerIndexFloat > 0.3;
                UpdateGunPos(false, _autoAimAtRigs, false);
                Cast(_gunInfo.point, false, aimAtPlayers);
            }
            else if (Keyboard.current.fKey.isPressed && !XRSettings.isDeviceActive)
            {
                gunTrigger = Mouse.current.leftButton.isPressed;
                UpdateGunPos(true, _autoAimAtRigs, true);
                Cast(_gunInfo.point, true, aimAtPlayers);
            }
            else
            {
                gunTrigger = false;
                _gunHasBeenLocked = false;
                _pointer.transform.position = Vector3.zero;
                _line.gameObject.SetActive(false);
            }
        }

        public static bool GunAimedAtPlayer()
        {
            if (RigAimedAt() != null && !RigAimedAt().isOfflineVRRig)
            {
                return true;
            }
            return false;
        }

        private void UpdateGunPos(bool right, bool aimAtPlayers, bool mouse)
        {
            if (!mouse)
            {
                Vector3 startPos;
                Vector3 dir;
                
                if (right)
                {
                    startPos = GorillaTagger.Instance.rightHandTransform.position;
                    dir = -GorillaTagger.Instance.rightHandTransform.up;
                }
                else
                {
                    startPos = GorillaTagger.Instance.leftHandTransform.position;
                    dir = -GorillaTagger.Instance.leftHandTransform.up;
                }
                    
                Physics.Raycast(startPos, dir, out var hit, 512f, mask);
                
                _gunInfo = hit;
            }
            else
            {
                var ray = tpc.ScreenPointToRay(Mouse.current.position.value);
                if (Physics.Raycast(ray, out var hit, 512f, mask))
                {
                    _gunInfo = hit;
                }
            }
            
            _lastGunPosUpdateTime = Time.time;
        }

        public static void SetPointerColor(Color defaultColor, Color triggeredColor)
        {
            _defaultColor = defaultColor;
            _triggerColor = triggeredColor;
        }

        private void Cast(Vector3 point, bool right, bool aimAtPlayers)
        {
            var colorFinal = gunTrigger ? _triggerColor : _defaultColor;

            var pos = point;
            _pointer.transform.position = aimAtPlayers && RigAimedAt() != null ? RigAimedAt().transform.position : pos;
            _pointerRenderer.material.color = colorFinal;
            
            if (!_line.gameObject.activeInHierarchy)
                _line.gameObject.SetActive(true);
            
            _line.startColor = colorFinal;
            _line.endColor = colorFinal;

            _line.SetPosition(0,
                right
                    ? GorillaTagger.Instance.rightHandTransform.position
                    : GorillaTagger.Instance.leftHandTransform.position);
            _line.SetPosition(1, _pointer.transform.position);
        }


        public static VRRig RigAimedAt()
        {
            if (gunTrigger && _vrRigAimedAt != null)
            {
                _gunHasBeenLocked = true;
                return _vrRigAimedAt;
            }

            if (_gunHasBeenLocked && _vrRigAimedAt != null)
            {
                return _vrRigAimedAt;
            }

            if (_timeSinceLastRigCheck > 10)
            {
                _timeSinceLastRigCheck = 0;
                float nearestDistance = Mathf.Infinity;
                VRRig nearestBody = _vrRigAimedAt;

                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    float distance = Vector3.Distance(_gunInfo.point, rig.transform.position);

                    if (distance < nearestDistance && distance < 5f && rig != GorillaTagger.Instance.offlineVRRig)
                    {
                        nearestDistance = distance;
                        nearestBody = rig;
                    }

                }
                if (nearestDistance > 5f)
                {
                    _vrRigAimedAt = null;
                }
                else
                {
                    _vrRigAimedAt = nearestBody;
                }
                return _vrRigAimedAt;
            }
            else
            {
                _timeSinceLastRigCheck += 1;
                return _vrRigAimedAt;
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
            if (_autoAimAtRigs && RigAimedAt() != null)
            {
                return RigAimedAt().transform.position;
            }
            else
            {
                return _gunInfo.point;
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
            if (Time.time > _lastGunPosUpdateTime + 1)
            {
                _gunLibEnabled = false;
            }
            else
            {
                _gunLibEnabled = true;
            }
            return _gunLibEnabled;
        }
    }
}
