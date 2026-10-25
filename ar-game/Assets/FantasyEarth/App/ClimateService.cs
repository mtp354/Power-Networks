using System;
using FantasyEarth.World;
using UnityEngine;

namespace FantasyEarth.App
{
    public sealed class ClimateService : MonoBehaviour
    {
        [SerializeField] private TextAsset koppenGrid;
        private ClimateModel _model;

        private void Awake()
        {
            if (koppenGrid == null) throw new InvalidOperationException("Bundled climate grid is not assigned");
            _model = new ClimateModel(new KoppenGrid(koppenGrid.bytes, KoppenGrid.GlobalWidth, KoppenGrid.GlobalHeight));
        }

        public Observation<KoppenClass> ClimateAt(GeoPoint p) => _model.ClimateAt(p);
    }
}
