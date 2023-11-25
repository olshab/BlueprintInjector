using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using BlueprintInjector.Utils;

namespace BlueprintInjector.Components
{
    public class UChildActorComponent : UActorComponent
    {
        public string? ChildActorClass { get; private set; }
        public Import? ChildActorClassImport { get; private set; }              

        public UChildActorComponent(NormalExport ComponentExport)
            : base(ComponentExport)
        {
            /** ChildActorClass */
            ObjectPropertyData? ChildActorClassObject = FindPropertyByName<ObjectPropertyData>("ChildActorClass");
            if (ChildActorClassObject is null || !ChildActorClassObject.Value.IsImport())
                return;

            ChildActorClassImport = ChildActorClassObject.ToImport(AssociatedAsset).GetOutermostPackage(AssociatedAsset);
            ChildActorClass = ChildActorClassImport.ObjectName.ToString();
        }
    }
}
