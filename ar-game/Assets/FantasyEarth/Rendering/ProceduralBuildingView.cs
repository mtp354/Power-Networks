using FantasyEarth.Fantasy;
using FantasyEarth.World;
using UnityEngine;

namespace FantasyEarth.Rendering
{
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralBuildingView : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private Vector2[] outline;
        [SerializeField] private float heightM = 8;
        [SerializeField] private Biome biome;
        [SerializeField] private uint worldSeed = 1;
        [SerializeField] private Material material;
        private Mesh _mesh;
        private bool _rebuild;

        public void Configure(BuildingFootprint footprint, Biome selectedBiome, Material sharedMaterial, uint seed = 1)
        {
            stableId = footprint.StableId; heightM = (float)footprint.HeightM;
            outline = new Vector2[footprint.Outline.Count];
            for (int i = 0; i < outline.Length; i++) outline[i] = new Vector2((float)footprint.Outline[i].EastM, (float)footprint.Outline[i].NorthM);
            biome = selectedBiome; material = sharedMaterial; worldSeed = seed;
            Rebuild();
        }

        private void OnEnable() => Rebuild();
        private void OnValidate() => _rebuild = true;
        private void Update() { if (_rebuild) Rebuild(); }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();

        [ContextMenu("Rebuild building")]
        public void Rebuild()
        {
            _rebuild = false;
            if (material == null || string.IsNullOrWhiteSpace(stableId) || outline == null || outline.Length < 3)
            { Release(); return; }
            var points = new PlanarPoint[outline.Length];
            for (int i = 0; i < points.Length; i++) points[i] = new PlanarPoint(outline[i].x, outline[i].y);
            var footprint = new BuildingFootprint(stableId, points, heightM);
            Mesh next = BuildingMeshBuilder.Create(footprint, BuildingRecipe.Create(footprint, biome, worldSeed));
            next.hideFlags = HideFlags.DontSave;
            Release();
            _mesh = next;
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void Release()
        {
            if (_mesh == null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == _mesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh);
            _mesh = null;
        }
    }
}
