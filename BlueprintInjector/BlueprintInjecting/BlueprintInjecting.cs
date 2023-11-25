using BlueprintInjector.Utils;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace BlueprintInjector.BlueprintInjecting
{
    public static class BlueprintInjecting
    {
        public static void InjectBlueprint(this UAsset Asset, string PackageName)
        {
            if (Asset.GetExportWithObjectName("SCS_Node_789") is not null)
                throw new Exception($"Blueprint {Asset.FilePath.GetAssetName()} is already modified (remove pakchunk from Paks directory)");

            string AssetName = PackageName.GetAssetName();

            Asset.AddChildActorImports();
            Asset.AddInjectableBlueprintImports(PackageName);

            FPackageIndex ClassExport = Asset.GetClassExportPackageIndex();
            FPackageIndex SimpleConstructionScript = Asset.GetExportWithClassName("SimpleConstructionScript");
            FPackageIndex SCS_Node = Asset.GetImportWithObjectName("SCS_Node");
            FPackageIndex Default__SCS_Node = Asset.GetImportWithObjectName("Default__SCS_Node");
            FPackageIndex ChildActorComponent = Asset.GetImportWithObjectName("ChildActorComponent");
            FPackageIndex Default__ChildActorComponent = Asset.GetImportWithObjectName("Default__ChildActorComponent");

            /** Add our exports */

            // ASSET_NAME_GEN_VARIABLE_ASSET_NAME_C_CAT
            NormalExport myExport = new NormalExport(Asset, new byte[4]);
            myExport.Data = new List<PropertyData>();
            myExport.ObjectName = new FName(Asset, AssetName + "_GEN_VARIABLE_" + AssetName + "_C_CAT");
            myExport.OuterIndex = new FPackageIndex(Asset.Exports.Count + 2);
            myExport.ClassIndex = FPackageIndex.FromImport(Asset.Imports.Count - 2);
            myExport.SuperIndex = new FPackageIndex(0);
            myExport.TemplateIndex = FPackageIndex.FromImport(Asset.Imports.Count - 1);
            myExport.ObjectFlags = EObjectFlags.RF_Transactional | EObjectFlags.RF_Public | EObjectFlags.RF_ArchetypeObject;
            myExport.SerializationBeforeSerializationDependencies.Add(new FPackageIndex(Asset.Exports.Count + 2));
            myExport.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromImport(Asset.Imports.Count - 2));
            myExport.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromImport(Asset.Imports.Count - 1));
            myExport.CreateBeforeCreateDependencies.Add(new FPackageIndex(Asset.Exports.Count + 2));

            Asset.Exports.Add(myExport);

            // ASSET_NAME_GEN_VARIABLE
            myExport = new NormalExport(Asset, new byte[4]);
            myExport.Data = new List<PropertyData>();
            myExport.ObjectName = new FName(Asset, AssetName + "_GEN_VARIABLE");
            myExport.OuterIndex = ClassExport;
            myExport.ClassIndex = ChildActorComponent;
            myExport.SuperIndex = new FPackageIndex(0);
            myExport.TemplateIndex = Default__ChildActorComponent;
            myExport.ObjectFlags = EObjectFlags.RF_Public | EObjectFlags.RF_Transactional | EObjectFlags.RF_ArchetypeObject;
            myExport.SerializationBeforeSerializationDependencies.Add(ClassExport);
            myExport.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromImport(Asset.Imports.Count - 2));
            myExport.CreateBeforeSerializationDependencies.Add(new FPackageIndex(Asset.Exports.Count));
            myExport.SerializationBeforeCreateDependencies.Add(ChildActorComponent);
            myExport.SerializationBeforeCreateDependencies.Add(Default__ChildActorComponent);
            myExport.CreateBeforeCreateDependencies.Add(ClassExport);

            ObjectPropertyData ChildActorClass = new ObjectPropertyData(new FName(Asset, "ChildActorClass"));
            ChildActorClass.Value = FPackageIndex.FromImport(Asset.Imports.Count - 2);
            myExport.Data.Add(ChildActorClass);

            ObjectPropertyData ChildActorTemplate = new ObjectPropertyData(new FName(Asset, "ChildActorTemplate"));
            ChildActorTemplate.Value = FPackageIndex.FromExport(Asset.Exports.Count - 1);
            myExport.Data.Add(ChildActorTemplate);

            Asset.Exports.Add(myExport);

            // SCS_Node
            myExport = new NormalExport(Asset, new byte[4]);
            myExport.Data = new List<PropertyData>();
            myExport.ObjectName = new FName(Asset, "SCS_Node_789");
            myExport.OuterIndex = SimpleConstructionScript;
            myExport.ClassIndex = SCS_Node;
            myExport.SuperIndex = new FPackageIndex(0);
            myExport.TemplateIndex = Default__SCS_Node;
            myExport.ObjectFlags = EObjectFlags.RF_Transactional;
            myExport.CreateBeforeSerializationDependencies.Add(new FPackageIndex(Asset.Exports.Count));
            myExport.CreateBeforeSerializationDependencies.Add(ChildActorComponent);
            myExport.SerializationBeforeCreateDependencies.Add(SCS_Node);
            myExport.SerializationBeforeCreateDependencies.Add(Default__SCS_Node);
            myExport.CreateBeforeCreateDependencies.Add(SimpleConstructionScript);

            ObjectPropertyData ComponentClass = new ObjectPropertyData(new FName(Asset, "ComponentClass"));
            ComponentClass.Value = ChildActorComponent;
            myExport.Data.Add(ComponentClass);

            ObjectPropertyData ComponentTemplate = new ObjectPropertyData(new FName(Asset, "ComponentTemplate"));
            ComponentTemplate.Value = FPackageIndex.FromExport(Asset.Exports.Count - 1);
            myExport.Data.Add(ComponentTemplate);

            StructPropertyData VariableGuid = new StructPropertyData(new FName(Asset, "VariableGuid"), new FName(Asset, "Guid"));
            GuidPropertyData AssetGuid = new GuidPropertyData(new FName(Asset, "VariableGuid"));
            AssetGuid.Value = new Guid("{485B7F17-43DE-BC5F-6B36-F89C1EA1B53F}");
            VariableGuid.Value.Add(AssetGuid);
            myExport.Data.Add(VariableGuid);

            NamePropertyData InternalVariableName = new NamePropertyData(new FName(Asset, "InternalVariableName"));
            InternalVariableName.Value = new FName(Asset, AssetName);
            myExport.Data.Add(InternalVariableName);


            // Add newly created SCS_Node to SimpleConstructionScript (RootNodes and AllNodes)
            NormalExport SCS = (NormalExport)SimpleConstructionScript.ToExport(Asset);

            if (SCS != null)
            {
                ArrayPropertyData? AllNodes = SCS.FindPropertyByName<ArrayPropertyData>("AllNodes");
                if (AllNodes is not null)
                {
                    ObjectPropertyData NewNode = new ObjectPropertyData(new FName(Asset, "567"));
                    NewNode.Value = FPackageIndex.FromExport(Asset.Exports.Count);

                    List<PropertyData> OrigArray = AllNodes.Value.ToList();
                    OrigArray.Add(NewNode);
                    AllNodes.Value = OrigArray.ToArray();
                }

                ArrayPropertyData? RootNodes = SCS.FindPropertyByName<ArrayPropertyData>("RootNodes");
                if (RootNodes is not null)
                {
                    ObjectPropertyData NewNode = new ObjectPropertyData(new FName(Asset, "567"));
                    NewNode.Value = FPackageIndex.FromExport(Asset.Exports.Count);

                    List<PropertyData> OrigArray = RootNodes.Value.ToList();
                    OrigArray.Add(NewNode);
                    RootNodes.Value = OrigArray.ToArray();
                }
            }

            Asset.Exports.Add(myExport);
        }

        private static void AddChildActorImports(this UAsset Asset)
        {
            if (Asset is null)
                throw new Exception();

            Asset.AddImport(new Import(
                    new FName(Asset, "/Script/CoreUObject"),
                    new FName(Asset, "Package"),
                    new FPackageIndex(0),
                    new FName(Asset, "/Script/Engine"),
                    false
                ));

            Asset.AddImport(new Import(
                    new FName(Asset, "/Script/CoreUObject"),
                    new FName(Asset, "Class"),
                    FPackageIndex.FromImport(Asset.Imports.Count - 1),  /* "/Script/Engine" */
                    new FName(Asset, "ChildActorComponent"),
                    false
                ));

            Asset.AddImport(new Import(
                new FName(Asset, "/Script/Engine"),
                new FName(Asset, "ChildActorComponent"),
                FPackageIndex.FromImport(Asset.Imports.Count - 2),  /* "/Script/Engine" */
                new FName(Asset, "Default__ChildActorComponent"),
                false
            ));
        }

        private static void AddInjectableBlueprintImports(this UAsset Asset, string PackageName)
        {
            if (Asset is null)
                throw new Exception();

            string AssetName = PackageName.GetAssetName();

            Asset.AddImport(new Import(
                    new FName(Asset, "/Script/CoreUObject"),
                    new FName(Asset, "Package"),
                    new FPackageIndex(0),
                    new FName(Asset, PackageName),
                    false
                ));

            Asset.AddImport(new Import(
                new FName(Asset, "/Script/Engine"),
                new FName(Asset, "BlueprintGeneratedClass"),
                FPackageIndex.FromImport(Asset.Imports.Count - 1),
                new FName(Asset, AssetName + "_C"),
                false
            ));

            Asset.AddImport(new Import(
                new FName(Asset, PackageName),
                new FName(Asset, AssetName + "_C"),
                FPackageIndex.FromImport(Asset.Imports.Count - 2),
                new FName(Asset, "Default__" + AssetName + "_C"),
                false
            ));
        }

        private static FPackageIndex GetClassExportPackageIndex(this UAsset Asset)
        {
            if (Asset is null)
                throw new Exception();

            FPackageIndex ClassExport = new FPackageIndex(0);
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                Export export = Asset.Exports[i];

                if (export is ClassExport bgcCat)
                {
                    ClassExport = FPackageIndex.FromExport(i);
                    break;
                }
            }

            return ClassExport;
        }

        private static FPackageIndex GetExportWithClassName(this UAsset Asset, string ClassName)
        {
            if (Asset is null)
                throw new Exception();

            FPackageIndex ExportPackageIndex = new FPackageIndex(0);
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                Export export = Asset.Exports[i];

                if (!(export is NormalExport normalExport))
                    continue;

                if (export.ClassIndex.Index > 0)
                    continue;

                if (export.ClassIndex.ToImport(Asset).ObjectName.ToString() == ClassName)
                {
                    ExportPackageIndex = FPackageIndex.FromExport(i);
                    break;
                }
            }

            return ExportPackageIndex;
        }

        private static FPackageIndex GetImportWithObjectName(this UAsset Asset, string ClassName)
        {
            if (Asset is null)
                throw new Exception();

            FPackageIndex ImportIndex = new FPackageIndex(0);
            for (int i = 0; i < Asset.Imports.Count; i++)
            {
                Import import = Asset.Imports[i];

                if (import.ObjectName.ToString() == ClassName)
                {
                    ImportIndex = FPackageIndex.FromImport(i);
                    break;
                }
            }

            return ImportIndex;
        }
    }
}
