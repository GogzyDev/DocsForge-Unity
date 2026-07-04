using Unity.Scripting.LifecycleManagement;
using UnityEditor.PackageManager;

namespace DocsForge.Settings
{
    internal static partial class DocsForgeBootstrap
    {
        private const string k_PackageName = "com.gogzydev.docsforge";

        [OnCodeInitializing]
        private static void Initialize()
        {
            Events.registeringPackages += OnRegisteringPackages;

            // TODO: subscribe to Events.registeredPackages for import-time initialization
            // and first-run onboarding (e.g. "Getting Started" window on first install).
        }

        private static void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            foreach (var package in args.removed)
            {
                if (package.name != k_PackageName)
                    continue;

                Cleanup();
                return;
            }
        }

        private static void Cleanup()
        {
            DocsForgeProjectSettings.DeleteAsset();
            DocsForgePreferences.DeleteAsset();
        }
    }
}
