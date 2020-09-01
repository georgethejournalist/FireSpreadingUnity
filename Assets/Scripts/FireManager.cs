using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
    public class FireManager : MonoBehaviour
    {
        public bool IsInitialized = false;
        public ComputeShader _shader;

        private int _kernel;
        private TreeManager _treeManager;
        private List<RenderTexture> _textures = new List<RenderTexture>();

        

        // Start is called before the first frame update
        void Start()
        {
            

            //_kernel = _shader.FindKernel("FireSpreading");
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
