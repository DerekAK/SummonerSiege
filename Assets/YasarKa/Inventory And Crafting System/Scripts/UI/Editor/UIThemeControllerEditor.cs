using UnityEngine;
using UnityEditor;

namespace InventoryAndCrafting
{
    [CustomEditor(typeof(UIThemeController))]
    public class UIThemeControllerEditor : Editor
    {
        private bool showInventoryPanelThemes = true;
        private bool showEquipmentPanelThemes = true;
        private bool showCraftingPanelThemes = true;
        private bool showOtherPanelThemes = true;
        private bool showSlotThemes = true;
        private bool showHeaderThemes = true;
        private bool showButtonThemes = true;
        private bool showPanelList = false;
        private bool showSlotList = false;
        private bool showHeaderList = false;
        private bool showButtonList = false;

        private SerializedProperty selectedInventoryPanelThemeProp;
        private SerializedProperty inventoryPanelThemesProp;
        private SerializedProperty inventoryPanelColorsProp;
        private SerializedProperty selectedEquipmentPanelThemeProp;
        private SerializedProperty equipmentPanelThemesProp;
        private SerializedProperty equipmentPanelColorsProp;
        private SerializedProperty selectedCraftingPanelThemeProp;
        private SerializedProperty craftingPanelThemesProp;
        private SerializedProperty craftingPanelColorsProp;
        private SerializedProperty selectedOtherPanelThemeProp;
        private SerializedProperty otherPanelThemesProp;
        private SerializedProperty otherPanelColorsProp;
        private SerializedProperty selectedSlotThemeProp;
        private SerializedProperty slotThemesProp;
        private SerializedProperty slotColorsProp;
        private SerializedProperty selectedHeaderThemeProp;
        private SerializedProperty headerThemesProp;
        private SerializedProperty headerColorsProp;
        private SerializedProperty selectedButtonThemeProp;
        private SerializedProperty buttonThemesProp;
        private SerializedProperty buttonColorsProp;
        private SerializedProperty inventoryPanelsProp;
        private SerializedProperty equipmentPanelsProp;
        private SerializedProperty craftingPanelsProp;
        private SerializedProperty otherPanelsProp;
        private SerializedProperty allSlotsProp;
        private SerializedProperty allHeadersProp;
        private SerializedProperty allButtonsProp;

        private void OnEnable()
        {
            selectedInventoryPanelThemeProp = serializedObject.FindProperty("selectedInventoryPanelTheme");
            inventoryPanelThemesProp = serializedObject.FindProperty("inventoryPanelThemes");
            inventoryPanelColorsProp = serializedObject.FindProperty("_inventoryPanelColors");
            selectedEquipmentPanelThemeProp = serializedObject.FindProperty("selectedEquipmentPanelTheme");
            equipmentPanelThemesProp = serializedObject.FindProperty("equipmentPanelThemes");
            equipmentPanelColorsProp = serializedObject.FindProperty("_equipmentPanelColors");
            selectedCraftingPanelThemeProp = serializedObject.FindProperty("selectedCraftingPanelTheme");
            craftingPanelThemesProp = serializedObject.FindProperty("craftingPanelThemes");
            craftingPanelColorsProp = serializedObject.FindProperty("_craftingPanelColors");
            selectedOtherPanelThemeProp = serializedObject.FindProperty("selectedOtherPanelTheme");
            otherPanelThemesProp = serializedObject.FindProperty("otherPanelThemes");
            otherPanelColorsProp = serializedObject.FindProperty("_otherPanelColors");
            selectedSlotThemeProp = serializedObject.FindProperty("selectedSlotTheme");
            slotThemesProp = serializedObject.FindProperty("slotThemes");
            slotColorsProp = serializedObject.FindProperty("_slotColors");
            selectedHeaderThemeProp = serializedObject.FindProperty("selectedHeaderTheme");
            headerThemesProp = serializedObject.FindProperty("headerThemes");
            headerColorsProp = serializedObject.FindProperty("_headerColors");
            selectedButtonThemeProp = serializedObject.FindProperty("selectedButtonTheme");
            buttonThemesProp = serializedObject.FindProperty("buttonThemes");
            buttonColorsProp = serializedObject.FindProperty("_buttonColors");
            inventoryPanelsProp = serializedObject.FindProperty("inventoryPanels");
            equipmentPanelsProp = serializedObject.FindProperty("equipmentPanels");
            craftingPanelsProp = serializedObject.FindProperty("craftingPanels");
            otherPanelsProp = serializedObject.FindProperty("otherPanels");
            allSlotsProp = serializedObject.FindProperty("allSlots");
            allHeadersProp = serializedObject.FindProperty("allHeaders");
            allButtonsProp = serializedObject.FindProperty("allButtons");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIThemeController themeController = (UIThemeController)target;

            DrawThemeSection("Inventory Panel Themes", ref showInventoryPanelThemes, ref showPanelList,
                selectedInventoryPanelThemeProp, inventoryPanelThemesProp, inventoryPanelColorsProp, 
                themeController.inventoryPanelThemes, themeController.SetInventoryPanelTheme);

            EditorGUILayout.Space(10);

            DrawThemeSection("Equipment Panel Themes", ref showEquipmentPanelThemes, ref showPanelList,
                selectedEquipmentPanelThemeProp, equipmentPanelThemesProp, equipmentPanelColorsProp,
                themeController.equipmentPanelThemes, themeController.SetEquipmentPanelTheme);

            EditorGUILayout.Space(10);

            DrawThemeSection("Crafting Panel Themes", ref showCraftingPanelThemes, ref showPanelList,
                selectedCraftingPanelThemeProp, craftingPanelThemesProp, craftingPanelColorsProp,
                themeController.craftingPanelThemes, themeController.SetCraftingPanelTheme);

            EditorGUILayout.Space(10);

            DrawThemeSection("Other Panel Themes", ref showOtherPanelThemes, ref showPanelList,
                selectedOtherPanelThemeProp, otherPanelThemesProp, otherPanelColorsProp,
                themeController.otherPanelThemes, themeController.SetOtherPanelTheme);

            EditorGUILayout.Space(20);

            DrawThemeSection("Slot Themes", ref showSlotThemes, ref showSlotList,
                selectedSlotThemeProp, slotThemesProp, slotColorsProp, themeController.slotThemes, themeController.SetSlotTheme);

            EditorGUILayout.Space(20);

            DrawThemeSection("Header Themes", ref showHeaderThemes, ref showHeaderList,
                selectedHeaderThemeProp, headerThemesProp, headerColorsProp, themeController.headerThemes, themeController.SetHeaderTheme);

            EditorGUILayout.Space(20);

            DrawThemeSection("Button Themes", ref showButtonThemes, ref showButtonList,
                selectedButtonThemeProp, buttonThemesProp, buttonColorsProp, themeController.buttonThemes, themeController.SetButtonTheme);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
        
            EditorGUILayout.LabelField("Panel References", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(inventoryPanelsProp);
            EditorGUILayout.PropertyField(equipmentPanelsProp);
            EditorGUILayout.PropertyField(craftingPanelsProp);
            EditorGUILayout.PropertyField(otherPanelsProp);
            EditorGUI.indentLevel--;
        
            EditorGUILayout.PropertyField(allSlotsProp);
            EditorGUILayout.PropertyField(allHeadersProp);
            EditorGUILayout.PropertyField(allButtonsProp);
            EditorGUI.indentLevel--;

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                if (Application.isPlaying)
                {
                    themeController.UpdateAllThemes();
                }
            }
        }

        private void DrawThemeSection(string title, ref bool foldout, ref bool showList,
            SerializedProperty selectedThemeProp, SerializedProperty themesProp, SerializedProperty colorsProp,
            Sprite[] themes, System.Action<Sprite> setThemeAction)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
            foldout = EditorGUILayout.Foldout(foldout, title, true);
            if (foldout)
            {
                EditorGUI.indentLevel++;

                // Theme selection
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(selectedThemeProp);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    if (setThemeAction != null)
                    {
                        setThemeAction.Invoke(selectedThemeProp.objectReferenceValue as Sprite);
                        EditorUtility.SetDirty(target);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    }
                }

                // Color settings
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(colorsProp);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying)
                    {
                        (target as UIThemeController).UpdateAllThemes();
                    }
                    EditorUtility.SetDirty(target);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }

                // Theme gallery
                showList = EditorGUILayout.Foldout(showList, "Available Themes", true);
                if (showList && themes != null)
                {
                    EditorGUI.indentLevel++;
                    float thumbnailSize = 64f;
                    int columns = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 50) / (thumbnailSize + 10));
                    int currentColumn = 0;

                    EditorGUILayout.BeginHorizontal();
                    for (int i = 0; i < themes.Length; i++)
                    {
                        if (currentColumn >= columns)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currentColumn = 0;
                        }

                        if (themes[i] != null)
                        {
                            EditorGUILayout.BeginVertical(GUILayout.Width(thumbnailSize));
                        
                            // Theme preview button
                            EditorGUI.BeginChangeCheck();
                            if (GUILayout.Button(themes[i].texture, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize)))
                            {
                                selectedThemeProp.objectReferenceValue = themes[i];
                                serializedObject.ApplyModifiedProperties();
                                if (setThemeAction != null)
                                {
                                    setThemeAction.Invoke(themes[i]);
                                    EditorUtility.SetDirty(target);
                                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                                }
                            }
                        
                            // Remove theme button
                            if (GUILayout.Button("X", GUILayout.Width(thumbnailSize)))
                            {
                                UIThemeController controller = (UIThemeController)target;
                                controller.RemoveTheme(themesProp.name, themes[i]);
                                serializedObject.Update();
                                EditorUtility.SetDirty(target);
                            }
                        
                            EditorGUILayout.EndVertical();
                        }
                        currentColumn++;
                    }
                    EditorGUILayout.EndHorizontal();
                
                    // Add new theme button
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    Sprite newTheme = (Sprite)EditorGUILayout.ObjectField("New Theme", null, typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck() && newTheme != null)
                    {
                        UIThemeController controller = (UIThemeController)target;
                        controller.AddTheme(themesProp.name, newTheme);
                        serializedObject.Update();
                        EditorUtility.SetDirty(target);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }
}
