using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.TreeHandling
{
    public class FireHandler : MonoBehaviour
    {
        public ComputeShader Shader;
        public float StepTime = 0.5f;
        public int TexResolution = 512;

        public Renderer Renderer;
        private RenderTexture[] myRt;

        public Texture InitialState;
        public int WindSpeed = 5;
        public int WindDir = 45;
        public int NaturalFireSpreadSpeed = 45;

        private int _currTex = 0;
        private const int NumTex = 2;
        private bool _shouldUpdate = false;
        private float _lastUpdate = 0.0f;

        private int _initKernel;
        private int _mainKernel;

        private bool _isInitialized;
        private bool _shouldReset;
        private bool _shouldSetPixels;

        List<PixelSetData> _pixelsToSet = new List<PixelSetData>();
        private TerrainTreeData _treeData;

        private ComputeBuffer _burntTreeBuffer;
        private Vector2Int[] _burntTreeArray;

        public event EventHandler<BurntTreesEventArgs> BurntTreesAdded;

        private BurntTreesEventArgs _reusableEventArgs;
        private SortedSet<int> _knownBurnedIndices;

        private struct PixelSetData
        {
            public int X;
            public int Y;
            public Color Color;

            public PixelSetData(int x, int y, Color color)
            {
                X = x;
                Y = y;
                Color = color;
            }
        }

        public void Init(TerrainTreeData treeData)
        {
            _knownBurnedIndices = new SortedSet<int>();
            _treeData = treeData;
            InitialState = treeData.TreeTexture;
        
            // one pixel border on heightmaps
            TexResolution = treeData.Terrain.terrainData.heightmapResolution - TerrainUtils.UnityTextureBorder;

            myRt = new RenderTexture[NumTex];
            for (int i = 0; i < NumTex; i++)
            {
                myRt[i] = new RenderTexture(TexResolution, TexResolution, 24);
                myRt[i].enableRandomWrite = true;
                myRt[i].Create();

                if (i == 0)
                {
                    Graphics.Blit(InitialState, myRt[i]);
                }
            }

            Shader = (ComputeShader)Resources.Load("Fire");
            if (Shader == null)
            {
                Debug.LogError("Could not instantiate the compute shader for FireHandler");
                return;
            }

            //rend = GetComponent<Renderer>();
            if (Renderer != null)
            {
                Renderer.enabled = true;
            }

            // we don't know how many trees can be burnt during the dispatch, so buffer for full texture size is created
            // 2 mb for 512 terrain tile on GPU - seems okay
            _burntTreeBuffer = new ComputeBuffer(TexResolution * TexResolution, sizeof(int) * 2);
        
            // same size in ram - Vector2Int has two int members, rest is function pointers 
            _burntTreeArray = new Vector2Int[TexResolution*TexResolution];

            _initKernel = Shader.FindKernel("CSInit");
            _mainKernel = Shader.FindKernel("CSMain");

            ResetComputeSim();

            _isInitialized = true;
        }

        private void ResetComputeSim()
        {
            int prevTex = _currTex;
            _currTex = (_currTex + 1) % NumTex;

            Graphics.Blit(InitialState, myRt[prevTex]);
            Shader.SetTexture(_initKernel, "Prev", InitialState);
            Shader.SetTexture(_initKernel, "Result", myRt[_currTex]);
            Shader.Dispatch(_initKernel, TexResolution / 8, TexResolution / 8, 1);

            if (Renderer != null)
            {
                Renderer.material.SetTexture("_MainTex", myRt[_currTex]);
            }
        }

        public void ClearInitialTexture()
        {
            var tex = new Texture2D(TexResolution, TexResolution, TextureFormat.ARGB32, false);
            tex.name = $"{_treeData.Terrain.name}-TreeTexture";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;

            tex.ClearTextureToColor();
            tex.Apply();

            InitialState = tex;
        }

        private void ComputeStepFrame()
        {
            int prevTex = _currTex;
            _currTex = (_currTex + 1) % NumTex;

            Shader.SetInt("TexRes", TexResolution);
            Shader.SetBuffer(_mainKernel, "BurntTrees", _burntTreeBuffer);
            Shader.SetTexture(_mainKernel, "Prev", myRt[prevTex]);
            Shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
            Shader.SetInt("WindDirDegrees", WindDir);
            Shader.SetInt("WindSpeed", WindSpeed);
            Shader.SetInt("NaturalFireSpreadSpeed", NaturalFireSpreadSpeed);
            Shader.SetTexture(_mainKernel, "Result", myRt[_currTex]);
            Shader.Dispatch(_mainKernel, TexResolution / 8, TexResolution / 8, 1);

            //Array.Clear(_burntTreeArray, 0, _burntTreeArray.Length);
            _burntTreeBuffer.GetData(_burntTreeArray);

            //_reusableEventArgs.BurntTrees.Clear();
            BurntTreesEventArgs eventArgs = null;
            for (var index = 0; index < _burntTreeArray.Length; index++)
            {
                var item = _burntTreeArray[index];
                // zeroed value is not interesting
                if (item == Vector2Int.zero)
                {
                    continue;
                }

                // non-zero values we don't know yet are, though
                if (!_knownBurnedIndices.Contains(index))
                {
                    _knownBurnedIndices.Add(index);
                    if (eventArgs == null)
                    {
                        eventArgs = new BurntTreesEventArgs();
                    }

                    Vector2 pos = new Vector2((index % TexResolution) / (float)TexResolution, (index / (float)TexResolution) / TexResolution);
                    eventArgs.BurntTrees.Add(pos);
                }
            }

            if (eventArgs != null)
            {
                OnBurntTreesAdded(eventArgs);
            }

            _lastUpdate -= StepTime;
        }

        public void StartSimulation()
        {
            _shouldUpdate = true;
        }

        public void StopSimulation()
        {
            _shouldUpdate = false;
        }

        public void ReInit(TerrainTreeData data, bool restart = false)
        {
            StopSimulation();
            ClearInitialTexture();
            _burntTreeBuffer.Dispose();
            Init(data);
            if (restart)
            {
                StartSimulation();
            }
        }

        public void ClearSimulation()
        {
            StopSimulation();
            ClearInitialTexture();
            ResetComputeSim();
        }

        public void ResetSimulation()
        {
            _shouldReset = true;
        }

        public void MarkTreeOnFire(TreeInstance tree)
        {
            var pos = GetTreeInstanceLocalPosition(tree, _treeData.Terrain);

            _pixelsToSet.Add(new PixelSetData(pos.x, pos.y, Color.red));
            _shouldSetPixels = true;
        }

    

        public void MarkTreeLive(TreeInstance tree)
        {
            var pos = GetTreeInstanceLocalPosition(tree, _treeData.Terrain);
            _pixelsToSet.Add(new PixelSetData(pos.x, pos.y, Color.green));
            _shouldSetPixels = true;
        }

        private Vector2Int GetTreeInstanceLocalPosition(TreeInstance tree, Terrain terrain)
        {
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;

            var pos = tree.position;
            var texX = (int)(pos.x * resolution);
            var texY = (int)(pos.z * resolution);

            return new Vector2Int(texX, texY);
        }

        void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_shouldSetPixels)
            {
                int nextTex = (_currTex + 1) % NumTex;

                var renderTex = myRt[_currTex];
                var tex2D = new Texture2D(TexResolution, TexResolution, TextureFormat.ARGB32, false);
                tex2D.wrapMode = TextureWrapMode.Clamp;
                tex2D.filterMode = FilterMode.Point;

                RenderTexture.active = renderTex;
                tex2D.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0, false);
                tex2D.Apply();
                RenderTexture.active = null;

                foreach (var pixelData in _pixelsToSet)
                {
                    tex2D.SetPixel(pixelData.X, pixelData.Y, pixelData.Color);
                }

                tex2D.Apply();

                Graphics.Blit(tex2D, myRt[_currTex]);
                Graphics.Blit(tex2D, myRt[nextTex]);

                _pixelsToSet.Clear();
                _shouldSetPixels = false;
            }

            // for testing purposes - toggle the sim
            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                _shouldUpdate = !_shouldUpdate;
                _lastUpdate = 0.0f;
            }

            if (_shouldUpdate && _lastUpdate > StepTime)
            {
                ComputeStepFrame();
            }

            if (Renderer != null)
            {
                Renderer.material.SetTexture("_MainTex", myRt[_currTex]);
            }

            _lastUpdate += Time.deltaTime;

            // for testing purposes - reset the sim
            if (Input.GetKeyUp(KeyCode.Alpha1) || _shouldReset)
            {
                ResetComputeSim();
                _shouldReset = false;
            }
        }

        protected virtual void OnBurntTreesAdded(BurntTreesEventArgs e)
        {
            BurntTreesAdded?.Invoke(this, e);
        }

        void OnDestroy()
        {
            _burntTreeBuffer?.Dispose();
        }
    }
}
