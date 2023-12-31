﻿using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using BlueprintInjector.Utils;
using CUE4Parse.Utils;

namespace BlueprintInjector.Components
{
    public class UActorSpawner : UActorComponent
    {
        public string? Visualization { get; private set; }
        public List<FActorSpawnerProperties> ActivatedSceneElement { get; private set; }
        public List<FActorSpawnerProperties> DeactivatedSceneElement { get; private set; }

        public UActorSpawner(NormalExport ActorSpawnerExport)
            : base(ActorSpawnerExport)
        {
            SoftObjectPropertyData? VisualizationData =
                ActorSpawnerExport.FindPropertyByName<SoftObjectPropertyData>("Visualization");

            if (VisualizationData is not null)
                Visualization = VisualizationData.Value.AssetPath.AssetName.ToString().SubstringBeforeLast('.');

            ActivatedSceneElement = new List<FActorSpawnerProperties>();
            DeactivatedSceneElement = new List<FActorSpawnerProperties>();

            ArrayPropertyData? ActivatedSceneElementData =
                ActorSpawnerExport.FindPropertyByName<ArrayPropertyData>("ActivatedSceneElement");
            if (ActivatedSceneElementData is not null)
            {
                foreach (StructPropertyData ActivatedSceneElementStruct in ActivatedSceneElementData.Value)
                    ActivatedSceneElement.Add(new FActorSpawnerProperties(ActivatedSceneElementStruct));
            }

            ArrayPropertyData? DeactivatedSceneElementData =
                ActorSpawnerExport.FindPropertyByName<ArrayPropertyData>("DeactivatedSceneElement");
            if (DeactivatedSceneElementData is not null)
            {
                foreach (StructPropertyData DeactivatedSceneElementStruct in DeactivatedSceneElementData.Value)
                    DeactivatedSceneElement.Add(new FActorSpawnerProperties(DeactivatedSceneElementStruct));
            }
        }
    }

    public class UHexSpawner : UActorSpawner
    {
        public UHexSpawner(NormalExport ActorSpawnerExport)
            : base(ActorSpawnerExport)
        { }
    }

    public class FActorSpawnerProperties
    {
        public string SceneElement { get; private set; }
        public float Weight { get; private set; }

        public FActorSpawnerProperties(StructPropertyData PropertiesStruct)
        {
            SoftObjectPropertyData? SceneElementData = PropertiesStruct.FindPropertyByName<SoftObjectPropertyData>("SceneElement");
            FloatPropertyData? WeightData = PropertiesStruct.FindPropertyByName<FloatPropertyData>("Weight");

            if (SceneElementData is null || WeightData is null)
                throw new Exception("Failed to get SceneElement or WeightData");

            SceneElement = SceneElementData.Value.AssetPath.AssetName.ToString().SubstringBeforeLast('.');
            Weight = WeightData.Value;
        }
    }
}
