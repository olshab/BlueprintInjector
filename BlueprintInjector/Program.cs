using BlueprintInjector.BlueprintGeneratedClass;
using BlueprintInjector.Components;
using BlueprintInjector.BlueprintInjecting;
using BlueprintInjector.Utils;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace BlueprintInjector
{
    public static class Settings
    {
        public static string AssetsDirectory = @"C:\Users\Oleg\Desktop\Injecting";
        public static string GameDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Dead by Daylight\DeadByDaylight\Content\Paks";

        /** Path to assets that are gonna be injected into original blueprints */
        public static string ProjectCookedContent = @"C:\Users\Oleg\Desktop\OldTiles\Saved\Cooked\WindowsNoEditor\OldTiles\Content\MergedTiles";
        public static string PakchunkDirectory = @"C:\Users\Oleg\Desktop\UnrealPak Oct2023\pakchunk830-WindowsNoEditor";

        public static bool bStripOutChildActors = true;
        public static bool bShouldInjectBlueprints = true;

        public static bool bMoveModifiedAndDuplicatedBlueprintsToPakchunk = true;
        public static bool bMoveMergedBlueprintsToPakchunk = true;
    }

    class Program
    {
        public static DefaultFileProvider? Provider = null;

        static HashSet<string> CopiedBlueprints = new HashSet<string>();
        static HashSet<string> SkipCopyingForBlueprints = new HashSet<string>();

        static void Main(string[] Args)
        {
            Provider = new DefaultFileProvider(
                Settings.GameDirectory,
                SearchOption.TopDirectoryOnly,
                true,
                new VersionContainer(EGame.GAME_UE4_27)
            );
            Provider.Initialize();
            Provider.Mount();

            /** Get a list of blueprints in which we are gonna inject */
            if (!File.Exists($"{Settings.AssetsDirectory}\\OriginalBlueprints.txt"))
            {
                Console.WriteLine("[ERROR] File with the list of original blueprints doesn't exist");
                return;
            }

            string[] FileContents = File.ReadAllLines($"{Settings.AssetsDirectory}\\OriginalBlueprints.txt");
            List<string> OriginalBlueprintsPackagePaths = new List<string>();

            foreach (string PackagePath in FileContents)
                if (PackagePath.StartsWith("DeadByDaylight"))
                    OriginalBlueprintsPackagePaths.Add(PackagePath);

            /** Delete all directories (previously extracted assets) */
            foreach (string Dir in Directory.EnumerateDirectories(Settings.AssetsDirectory))
                Directory.Delete(Dir, true);

            /** Scan cooked project files for assets that are gonna be injected into original BPs */
            // Pairs AssetName : FilePath
            Dictionary<string, string> ProjectAssets = new Dictionary<string, string>();

            if (!Directory.Exists(Settings.ProjectCookedContent))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to find project cooked content");
                Console.ForegroundColor = ConsoleColor.Gray;

                return;
            }

            string[] ProjectAssetsFilePaths = Directory.GetFiles(Settings.ProjectCookedContent, "*.uasset", SearchOption.AllDirectories);
            foreach (string ProjectAssetFilePath in ProjectAssetsFilePaths)
            {
                ProjectAssets.Add(ProjectAssetFilePath.GetAssetName(), ProjectAssetFilePath);
            }

            foreach (string PackagePath in OriginalBlueprintsPackagePaths)
            {
                string AssetName = PackagePath.SubstringAfterLast('/');

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Exporting blueprint {AssetName}...");
                Console.ForegroundColor = ConsoleColor.Gray;

                string ExportDirectory = Settings.AssetsDirectory + "\\" + PackagePath.SubstringBeforeLast('/').Replace('/', '\\');
                string? ExtractedBlueprintFilePath = Provider.ExtractAsset(PackagePath, ExportDirectory);

                if (ExtractedBlueprintFilePath is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Failed to export {AssetName}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    continue;
                }

                UAsset Asset = new UAsset(ExtractedBlueprintFilePath, EngineVersion.VER_UE4_27);
                UBlueprintGeneratedClass BPGC = new UBlueprintGeneratedClass(Asset.GetClassExport());

                /** Get all UActorComponent templates so we can hide them */
                List<UActorComponent> ActorComponents = BPGC.GetAllComponents();
                List<UChildActorComponent> ChildActorComponents = BPGC.GetComponentsOfClass<UChildActorComponent>();

                string[] ComponentClassesWhitelist = {
                    "ActorSpawner",
                };
                HideActorComponents(ActorComponents, ComponentClassesWhitelist);
                HideChildActors(ChildActorComponents);

                FileSystemUtils.DeleteEmptyDirs(Settings.AssetsDirectory);

                /** Inject blueprint if it exists in cooked content */
                if (ProjectAssets.ContainsKey(AssetName))
                {
                    if (Settings.bStripOutChildActors)
                        StripOutChildActors(ProjectAssets[AssetName]);

                    Asset.InjectBlueprint(ProjectAssets[AssetName].GetPackageNameFromFilePath());
                }

                else if (AssetName == "BP_TL_Bd_16x16_FenceCornerB_2")
                {
                    // HARDCODE
                    if (Settings.bStripOutChildActors)
                        StripOutChildActors(ProjectAssets["BP_TL_Bd_16x16_FenceCornerB22"]);

                    Asset.InjectBlueprint(ProjectAssets["BP_TL_Bd_16x16_FenceCornerB22"].GetPackageNameFromFilePath());
                }

                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[Warning] Could not find {AssetName} in cooked content");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    // We still need to inject something so during cleanup we can recognize this blueprint as modified
                    Asset.InjectBlueprint("/Game/Empty");
                }

                if (Settings.bShouldInjectBlueprints)
                    Asset.Write(ExtractedBlueprintFilePath);
            }

            if (Settings.bMoveModifiedAndDuplicatedBlueprintsToPakchunk)
            {
                /** Clean up existing duplicated blueprints */
                string[] AllAssets = Directory.GetFiles(Settings.PakchunkDirectory, "*.uasset", SearchOption.AllDirectories);

                foreach (string AssetFile in AllAssets)
                    if (AssetFile.SubstringBeforeLast('\\').SubstringAfterLast('\\') == "COPY")
                        try
                        {
                            Directory.Delete(AssetFile.SubstringBeforeLast('\\'), true);
                        }
                        catch { }

                /** Clean up existing modified blueprints (into which another blueprint has already been injected) */
                AllAssets = Directory.GetFiles(Settings.PakchunkDirectory, "*.uasset", SearchOption.AllDirectories);

                foreach (string AssetFile in AllAssets)
                {
                    UAsset Asset = new UAsset(AssetFile, EngineVersion.VER_UE4_27);
                    if (Asset.GetExportWithObjectName("SCS_Node_789") is not null)
                    {
                        foreach (string AssetPackage in AssetFile.GetAllPackages())
                            File.Delete(AssetPackage);
                    }
                }

                FileSystemUtils.DeleteEmptyDirs(Settings.PakchunkDirectory);

                foreach (string dir in Directory.EnumerateDirectories(Settings.AssetsDirectory))
                    FileSystemUtils.CopyDirectory(dir, Settings.PakchunkDirectory + '\\' + dir.SubstringAfterWithLast('\\'));
            }

            if (Settings.bMoveMergedBlueprintsToPakchunk)
            {
                if (Directory.Exists(Settings.PakchunkDirectory + @"\DeadByDaylight\Content\MergedTiles"))
                    Directory.Delete(Settings.PakchunkDirectory + @"\DeadByDaylight\Content\MergedTiles", true);

                FileSystemUtils.CopyDirectory(Settings.ProjectCookedContent, Settings.PakchunkDirectory + @"\DeadByDaylight\Content\MergedTiles");
            }
        }        
    
        static void HideActorComponents(List<UActorComponent> ActorComponents, string[] ComponentClassesWhitelist)
        {
            foreach (UActorComponent ActorComponent in ActorComponents)
            {
                UAsset AssociatedAsset = ActorComponent.GetAssociatedAsset();

                if (ComponentClassesWhitelist.Contains(ActorComponent.ClassName))
                    continue;

                BoolPropertyData bVisible = new BoolPropertyData(new FName(AssociatedAsset, "bVisible"));
                bVisible.Value = false;
                ActorComponent.Properties.Add(bVisible);

                BoolPropertyData bHiddenInGame = new BoolPropertyData(new FName(AssociatedAsset, "bHiddenInGame"));
                bHiddenInGame.Value = true;
                ActorComponent.Properties.Add(bHiddenInGame);

                // if "BoolProperty" hasn't existed in NameMap yet, UAssetAPI would throw an exception
                AssociatedAsset.AddNameReference(new FString("BoolProperty"));
            }
        }
    
        static void HideChildActors(List<UChildActorComponent> ChildActorComponents)
        {
            if (Provider is null)
                throw new Exception();

            foreach (UChildActorComponent ChildActorComponent in ChildActorComponents)
            {
                if (ChildActorComponent.ChildActorClass is null)
                    continue;

                UAsset AssociatedAsset = ChildActorComponent.GetAssociatedAsset();

                Import? BlueprintPackage = ChildActorComponent.ChildActorClassImport;
                if (BlueprintPackage is null)
                    continue;

                // If we have already exported BP and changed package name for it
                if (ChildActorComponent.ChildActorClass.Contains("/COPY/"))
                    continue;

                // Convert "/Game/..." into "DeadByDaylight/Content/..."
                string FModelPackagePath = ChildActorComponent.ChildActorClass.GetPackagePath();
                string AssetName = FModelPackagePath.GetAssetName();

                if (CopiedBlueprints.Contains(FModelPackagePath))
                {
                    BlueprintPackage.ObjectName = new FName(AssociatedAsset, ChildActorComponent.ChildActorClass.SubstringBeforeLast('/') + "/COPY/" + AssetName);
                    continue;
                }

                if (SkipCopyingForBlueprints.Contains(FModelPackagePath))
                    continue;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Copying {AssetName} used as ChildActorClass...");
                Console.ForegroundColor = ConsoleColor.Gray;
                
                string ExportDirectory = Settings.AssetsDirectory + "\\" + FModelPackagePath.SubstringBeforeLast('/').Replace('/', '\\') + "\\COPY";
                string? ExtractedBlueprintFilePath = Provider.ExtractAsset(FModelPackagePath, ExportDirectory);
                
                if (ExtractedBlueprintFilePath is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Failed to export {AssetName}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    continue;
                }

                CopiedBlueprints.Add(FModelPackagePath);

                UAsset ChildActorBlueprint = new UAsset(ExtractedBlueprintFilePath, EngineVersion.VER_UE4_27);

                /** Now check if we even need this blueprint, i.e. if it has any components */
                UBlueprintGeneratedClass BPGC = new UBlueprintGeneratedClass(ChildActorBlueprint.GetClassExport());

                if (!BPGC.HasAnyComponents())
                {
                    SkipCopyingForBlueprints.Add(FModelPackagePath);

                    foreach (string AssetPackage in ExtractedBlueprintFilePath.GetAllPackages())
                        File.Delete(AssetPackage);

                    continue;
                }

                /** Get all UActorComponent templates so we can hide them */
                List<UActorComponent> ActorComponentsInBlueprint = BPGC.GetAllComponents();

                string[] ComponentClassesWhitelist = {
                    "ActorSpawner",
                };
                HideActorComponents(ActorComponentsInBlueprint, ComponentClassesWhitelist);

                ChildActorBlueprint.Write(ExtractedBlueprintFilePath);
                BlueprintPackage.ObjectName = new FName(AssociatedAsset, ChildActorComponent.ChildActorClass.SubstringBeforeLast('/') + "/COPY/" + AssetName);
            }
        }
    
        static void StripOutChildActors(string AssetFilePath)
        {
            if (!File.Exists(AssetFilePath))
                return;

            UAsset BlueprintToCleanup = new UAsset(AssetFilePath, EngineVersion.VER_UE4_27);

            /** Get all UActorComponent templates so we can hide them */
            UBlueprintGeneratedClass BPGC = new UBlueprintGeneratedClass(BlueprintToCleanup.GetClassExport());

            List<UChildActorComponent> ChildActorComponents = BPGC.GetComponentsOfClass<UChildActorComponent>();
            foreach (UActorComponent ChildActorComponent in ChildActorComponents)
            {
                string[] Tags = {
                    "ActorSpawner",
                    "NewTiles",
                };

                if (ChildActorComponent.HasAnyTags(Tags))
                {
                    ObjectPropertyData? ChildActorClass = ChildActorComponent.FindPropertyByName<ObjectPropertyData>("ChildActorClass");

                    if (ChildActorClass is null)
                    {
                        /** If this child actor is inherited from parent BP */
                        ChildActorClass = new ObjectPropertyData(new FName(BlueprintToCleanup, "ChildActorClass"));
                        ChildActorComponent.Properties.Add(ChildActorClass);
                    }

                    ChildActorClass.Value = new FPackageIndex(0);
                }
            }

            BlueprintToCleanup.Write(AssetFilePath);
        }
    }
}
