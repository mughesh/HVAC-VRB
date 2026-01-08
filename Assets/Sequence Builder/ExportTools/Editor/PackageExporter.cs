// PackageExporter.cs
// Automates the process of creating DLL distribution packages
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRTrainingKit.ExportTools
{
    public class PackageExporter : EditorWindow
    {
        private string version = "1.0";
        private bool confirmExport = false;
        private Vector2 scrollPos;

        [MenuItem("Sequence Builder/Package Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageExporter>("Package Exporter");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("VR Training Kit - Package Exporter", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Creates distribution package with DLL protection", EditorStyles.miniLabel);

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "⚠️ IMPORTANT: This will temporarily modify your project structure!\n\n" +
                "Make sure you have:\n" +
                "• Committed changes to Git, OR\n" +
                "• Created a backup of your project\n\n" +
                "The script will guide you through restoring your project after export.",
                MessageType.Warning
            );

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

            version = EditorGUILayout.TextField("Version Number:", version);

            GUILayout.Space(10);
            confirmExport = EditorGUILayout.Toggle("I have backed up my project", confirmExport);

            GUILayout.Space(20);

            // Step-by-step guide
            EditorGUILayout.LabelField("Export Process:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Step 1: Click 'Prepare for Export' below\n" +
                "Step 2: Use Unity's Assets > Export Package...\n" +
                "Step 3: Click 'Restore Project' to undo changes",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Buttons
            GUI.enabled = confirmExport;
            if (GUILayout.Button("Step 1: Prepare for Export", GUILayout.Height(40)))
            {
                PrepareForExport();
            }
            GUI.enabled = true;

            GUILayout.Space(5);

            if (GUILayout.Button("Step 2: Open Unity Export Dialog", GUILayout.Height(40)))
            {
                OpenExportDialog();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Step 3: Restore Project (IMPORTANT!)", GUILayout.Height(40)))
            {
                RestoreProject();
            }

            GUILayout.Space(20);

            // Status Display
            EditorGUILayout.LabelField("Current Project Status:", EditorStyles.boldLabel);

            bool pluginsExist = Directory.Exists("Assets/SequenceBuilder/Plugins");
            bool coreExists = Directory.Exists("Assets/SequenceBuilder/Scripts/Core");

            if (pluginsExist && !coreExists)
            {
                EditorGUILayout.HelpBox("✅ Project is in EXPORT mode\n" +
                    "DLLs are in Plugins/, source folders removed.\n" +
                    "Ready to export package!", MessageType.Info);
            }
            else if (!pluginsExist && coreExists)
            {
                EditorGUILayout.HelpBox("✅ Project is in DEVELOPMENT mode\n" +
                    "Source code is active, no DLLs in Plugins/.\n" +
                    "Ready for development!", MessageType.Info);
            }
            else if (pluginsExist && coreExists)
            {
                EditorGUILayout.HelpBox("⚠️ Mixed state detected!\n" +
                    "Both DLLs and source code present.\n" +
                    "Click 'Restore Project' to fix.", MessageType.Warning);
            }

            GUILayout.Space(10);

            // Documentation link
            if (GUILayout.Button("📖 View Full Export Guide"))
            {
                var guidePath = "Assets/SequenceBuilder/Documentation/DLL_EXPORT_STEPS.md";
                var fullPath = Path.GetFullPath(guidePath);
                Application.OpenURL("file://" + fullPath);
            }

            EditorGUILayout.EndScrollView();
        }

        private void PrepareForExport()
        {
            if (!EditorUtility.DisplayDialog(
                "Prepare for Export",
                "This will:\n\n" +
                "1. Create Plugins/ folder\n" +
                "2. Copy 5 DLL files from Library/ (including Profiles.dll)\n" +
                "3. Delete Core, SequenceSystem, StepHandlers, Editor source folders\n" +
                "4. Keep Profiles source code + .asmdef (for reference)\n\n" +
                "Users will have Profiles DLL + source code for learning!\n\n" +
                "Continue?",
                "Yes, Prepare",
                "Cancel"))
            {
                return;
            }

            try
            {
                // Step 1: Create Plugins folder
                string pluginsPath = "Assets/SequenceBuilder/Plugins";
                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                    Debug.Log("✅ Created Plugins folder");
                }

                // Step 2: Copy DLLs (including Profiles!)
                string libPath = "Library/ScriptAssemblies/";
                string[] dllsToCopy = new string[]
                {
                    "VRTrainingKit.Profiles.dll",
                    "VRTrainingKit.Runtime.dll",
                    "VRTrainingKit.Sequences.dll",
                    "VRTrainingKit.StepHandlers.dll",
                    "VRTrainingKit.Editor.dll"
                };

                foreach (string dll in dllsToCopy)
                {
                    string sourcePath = Path.Combine(libPath, dll);
                    string destPath = Path.Combine(pluginsPath, dll);

                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, true);
                        Debug.Log($"✅ Copied {dll}");
                    }
                    else
                    {
                        Debug.LogError($"❌ DLL not found: {sourcePath}");
                    }
                }

                // Step 3: Delete source folders
                string[] foldersToDelete = new string[]
                {
                    "Assets/VRTrainingKit/Scripts/Core",
                    "Assets/VRTrainingKit/Scripts/SequenceSystem",
                    "Assets/VRTrainingKit/Scripts/StepHandlers",
                    "Assets/VRTrainingKit/Scripts/Editor"
                };

                foreach (string folder in foldersToDelete)
                {
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                        File.Delete(folder + ".meta");
                        Debug.Log($"✅ Deleted {folder}");
                    }
                }

                // Step 4: Keep Profiles .asmdef (users need it to reference the assembly)
                // We're keeping both the DLL and source code
                // Users can read the source and extend it in their own assemblies
                Debug.Log("✅ Keeping Profiles source code and .asmdef for reference");

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Success!",
                    "Project prepared for export!\n\n" +
                    "Next steps:\n" +
                    "1. Click 'Step 2: Open Unity Export Dialog'\n" +
                    "2. Select SequenceBuilder folder\n" +
                    "3. Save as: VRTrainingKit_v" + version + "_DLL.unitypackage\n" +
                    "4. Click 'Step 3: Restore Project' when done!",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", "Export preparation failed:\n" + e.Message, "OK");
                Debug.LogError(e);
            }
        }

        private void OpenExportDialog()
        {
            // Select the SequenceBuilder folder
            var vrtkFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/SequenceBuilder");
            if (vrtkFolder != null)
            {
                Selection.activeObject = vrtkFolder;
            }

            EditorUtility.DisplayDialog(
                "Export Package",
                "The SequenceBuilder folder is now selected.\n\n" +
                "Steps:\n" +
                "1. Go to: Assets > Export Package...\n" +
                "2. Make sure VRTrainingKit is selected\n" +
                "3. Check 'Include dependencies'\n" +
                "4. Save as: VRTrainingKit_v" + version + "_DLL.unitypackage\n\n" +
                "⚠️ After exporting, click 'Step 3: Restore Project'!",
                "I Understand"
            );

            // Automatically open the export dialog
            EditorApplication.ExecuteMenuItem("Assets/Export Package...");
        }

        private void RestoreProject()
        {
            if (!EditorUtility.DisplayDialog(
                "Restore Project",
                "⚠️ WARNING: This assumes you have your source code in Git!\n\n" +
                "This will:\n" +
                "1. Delete the Plugins/ folder\n" +
                "2. Run 'git checkout .' to restore deleted files\n\n" +
                "If you're NOT using Git, you'll need to manually restore from backup!\n\n" +
                "Continue?",
                "Yes, Restore (Git)",
                "Cancel"))
            {
                return;
            }

            try
            {
                // Delete Plugins folder
                string pluginsPath = "Assets/SequenceBuilder/Plugins";
                if (Directory.Exists(pluginsPath))
                {
                    Directory.Delete(pluginsPath, true);
                    File.Delete(pluginsPath + ".meta");
                    Debug.Log("✅ Deleted Plugins folder");
                }

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Manual Step Required",
                    "Plugins folder deleted.\n\n" +
                    "Now run this command in your terminal:\n\n" +
                    "git checkout .\n\n" +
                    "This will restore all deleted source folders.",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", "Restore failed:\n" + e.Message, "OK");
                Debug.LogError(e);
            }
        }
    }
}
