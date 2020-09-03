using System;
using Assets.Scripts.Enums;
using Assets.Scripts.TreeHandling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class DemoManager : MonoBehaviour
    {
        private TreeManager _treeManager;

        public InteractionMode Mode;

        public bool InputHandlingLocked = false;
        // Start is called before the first frame update
        void Start()
        {
            _treeManager = GetComponent<TreeManager>();
            if (_treeManager == null)
            {
                _treeManager = gameObject.AddComponent<TreeManager>();
            }

            _treeManager.Init();

            Mode = InteractionMode.PlaceTree;
        }

        public void LockInputHandling()
        {
            InputHandlingLocked = true;
        }

        public void UnlockInputHandling()
        {
            InputHandlingLocked = false;
        }

        public void OnWindDirectionChanged(float value)
        {
            Debug.Log($"Wind dir changed to {value}");

            _treeManager.SetGlobalWindDirection((int)value);
        }

        public void OnWindSpeedChanged(float value)
        {
            Debug.Log($"Wind speed changed to {value}");
            _treeManager.SetGlobalWindSpeed((int)value);
        }

        public void OnNaturalSpreadChanged(float value)
        {
            Debug.Log($"Natural spread speed changed to {value}");
            _treeManager.SetGlobalNaturalSpread((int) value);
        }

        public void OnSimulationStepTimeChanged(float value)
        {
            Debug.Log($"Simulation step time changed to {value}");
            _treeManager.SetGlobalSimulationStepTime(value);
        }

        // Update is called once per frame
        void Update()
        {
            if (InputHandlingLocked)
            {
                return;
            }

            if (Input.GetMouseButtonUp((int)MouseButton.LeftMouse))
            {
                var mousePos = Input.mousePosition;
                //var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                Ray ray = Camera.main.ScreenPointToRay(mousePos);

                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    switch (Mode)
                    {
                        case InteractionMode.None:
                            return;
                        case InteractionMode.PlaceTree:
                        {
                            var terrain = hit.collider.gameObject.GetComponent<Terrain>();
                            if (terrain != null)
                            {
                                _treeManager.PlaceTreeUnderCursor(hit.point, terrain);
                            }
                        
                            break;
                        }
                        case InteractionMode.RemoveTree:
                        {
                            var terrain = hit.collider.gameObject.GetComponent<Terrain>();
                            if (terrain != null)
                            {
                                _treeManager.RemoveTreeUnderCursor(hit.point, terrain);
                            }
                            break;
                        }
                        case InteractionMode.ToggleFire:
                        {
                            _treeManager.ToggleTreeStateUnderCursor(hit.point);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
            _treeManager.GlobalFireSimulationStart();
        }

        public void StopFireSimulation()
        {
            Debug.Log("Stopping fire sim");
            _treeManager.GlobalFireSimulationStop();
        }

        public void ClearTrees()
        {
            Debug.Log("Clearing trees");
            _treeManager.GlobalClearAllTrees();
        }

        public void GenerateTrees()
        {
            _treeManager.GlobalGenerateTrees(10000);
        }

        public void SetRandomTreesOnFire()
        {
            _treeManager.SetRandomTreesOnFire();
        }

        public void ToggleTexture()
        {
            var rend = this.GetComponent<Renderer>();
            if (rend == null)
            {
                return;
            }

            rend.enabled = !rend.enabled;

            if (rend.enabled)
            {
                _treeManager.SetRendererToNextHandler(rend);
            }
        }

        public void SwitchHandlerRendered()
        {
            var rend = this.GetComponent<Renderer>();
            if (rend == null)
            {
                return;
            }
            _treeManager.SetRendererToNextHandler(rend);
        }

        public void QuitDemo()
        {
            _treeManager?.CleanUp();
            Debug.Log("Quitting");
            Application.Quit(0);
        }
    }
}
