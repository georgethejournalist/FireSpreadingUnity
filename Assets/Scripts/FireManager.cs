using UnityEngine;

namespace Assets.Scripts
{
    public class FireManager : MonoBehaviour
    {
        public bool IsInitialized = false;

        private TreeManager _treeManager;

        // Start is called before the first frame update
        void Start()
        {
            var terrains = FindObjectsOfType<Terrain>();
            if (terrains.Length == 0)
            {
                Debug.Log("No terrain tiles found, fire manager not initialized");
                return;
            }
        }

        public void Init(TreeManager treeManager)
        {
            _treeManager = treeManager;
            IsInitialized = true;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        bool GetSimulationState()
        {
            return true;
        }

        public void StartSimulation()
        {

        }

        public void StopSimulation()
        {

        }

        public void ToggleSimulation()
        {

        }
    }
}
