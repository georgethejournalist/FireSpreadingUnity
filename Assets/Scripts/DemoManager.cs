using System;
using Assets.Scripts.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class DemoManager : MonoBehaviour
    {
        private FireManager _fireManager;
        private TreeManager _treeManager;

        public InteractionMode Mode;

        // Start is called before the first frame update
        void Start()
        {
            _treeManager = GetComponent<TreeManager>();
            _fireManager = GetComponent<FireManager>();
            if (_treeManager == null)
            {
                _treeManager = gameObject.AddComponent<TreeManager>();
            }

            if (_fireManager == null)
            {
                _fireManager = gameObject.AddComponent<FireManager>();
            }

            _fireManager.Init(_treeManager);

            // TODO change after testing
            Mode = InteractionMode.RemoveTree;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonUp((int)MouseButton.LeftMouse))
            {
                var mousePos = Input.mousePosition;
                //var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                Ray ray = Camera.main.ScreenPointToRay(mousePos);

                //switch (Mode)
                //{

                //}

                // Bit shift the index of the layer (8) to get a bit mask
                //int layerMask = 1 << 8;

                // This would cast rays only against colliders in layer 8.
                // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
                //layerMask = ~layerMask;

                RaycastHit hit;
                // Does the ray intersect any objects excluding the player layer
                if (Physics.Raycast(ray, out hit, Mathf.Infinity/*, layerMask*/))
                {
                    Debug.DrawRay(ray.origin, ray.direction * 500, Color.yellow);
                    Debug.Log("Did Hit");

                    switch (Mode)
                    {
                        case InteractionMode.None:
                            return;
                        case InteractionMode.PlaceTree:
                            break;
                        case InteractionMode.RemoveTree:
                            //_treeManager.FindTreeInstanceIndexAtPosition(hit.point);
                            _treeManager.RemoveTreeUnderCursor(hit.point);
                            break;
                        case InteractionMode.ToggleFire:
                            _treeManager.ToggleTreeStateUnderCursor(hit.point);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * 500, Color.yellow);
                    Debug.Log("Did not Hit");
                }
            }
        }

        public void SetMode(int mode)
        {
            Mode = (InteractionMode)mode;

            Debug.Log($"Set interaction mode to {Mode}");
        }

        public void StartFireSimulation()
        {
            Debug.Log("Starting fire sim");
            _fireManager.StartSimulation();
        }

        public void StopFireSimulation()
        {
            Debug.Log("Stopping fire sim");
            _fireManager.StopSimulation();
        }

        public void QuitDemo()
        {
            Debug.Log("Quitting");
            Application.Quit(0);
        }
    }
}
