using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts
{
    public class DemoManager : MonoBehaviour
    {
        private FireManager _fireManager;
        private TreeManager _treeManager;

        private InteractionMode _mode;

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
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void SetMode(int mode)
        {
            _mode = (InteractionMode)mode;

            Debug.Log($"Set interaction mode to {_mode}");
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
