using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI;

namespace BlueprintInjector.Utils
{
    public static class UAssetAPIHelpers
    {
        public static T? FindPropertyByName<T>(this NormalExport ExportToSearchIn, string PropertyName) where T : PropertyData
        {
            foreach (PropertyData data in ExportToSearchIn.Data)
                if (data.Name.ToString() == PropertyName)
                    return (T)data;

            return null;
        }

        public static T FindPropertyByNameChecked<T>(this NormalExport ExportToSearchIn, string PropertyName) where T : PropertyData
        {
            foreach (PropertyData data in ExportToSearchIn.Data)
                if (data.Name.ToString() == PropertyName)
                    return (T)data;

            throw new Exception($"Failed to find property {PropertyName} in export {ExportToSearchIn.ObjectName.ToString()}");
        }

        public static T? FindPropertyByName<T>(this StructPropertyData StructToSearchIn, string PropertyName) where T : PropertyData
        {
            foreach (PropertyData data in StructToSearchIn.Value)
                if (data.Name.ToString() == PropertyName)
                    return (T)data;

            return null;
        }

        public static T FindPropertyByNameChecked<T>(this StructPropertyData StructToSearchIn, string PropertyName) where T : PropertyData
        {
            foreach (PropertyData data in StructToSearchIn.Value)
                if (data.Name.ToString() == PropertyName)
                    return (T)data;

            throw new Exception($"Failed to find property {PropertyName} in export {StructToSearchIn.Name.ToString()}");
        }

        public static NormalExport? FindExportByClassName(this UAsset Asset, string ClassName)
        {
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                Export export = Asset.Exports[i];

                if (!(export is NormalExport))
                    continue;

                if (!export.ClassIndex.IsImport())
                    continue;

                Import Class = export.ClassIndex.ToImport(Asset);
                if (Class.ObjectName.ToString() == ClassName)
                    return (NormalExport)export;
            }

            return null;
        }

        public static Import GetOutermostPackage(this Import InImport, UAsset Asset)
        {
            Import CurrentImport = InImport;

            while (!CurrentImport.OuterIndex.IsNull())
                CurrentImport = CurrentImport.OuterIndex.ToImport(Asset);

            return CurrentImport;
        }

        public static Export? GetExportWithObjectName(this UAsset Asset, string ObjectName)
        {
            if (Asset is null)
                throw new Exception();

            foreach (Export export in Asset.Exports)
                if (export.ObjectName.ToString() == ObjectName)
                    return export;

            return null;
        }
    }
}
