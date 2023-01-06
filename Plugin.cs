using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItemSpawner
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static ConfigEntry<KeyboardShortcut> openMenuKey;

        private void Awake()
        {
            openMenuKey = Config.Bind("General", "Open Menu Key", new KeyboardShortcut(KeyCode.F3), "Key to open spawn menu");

            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            Logger.LogInfo($"Patching...");
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Patched");
        }

        [HarmonyPatch(typeof(PlayerManager), "Start")]
        [HarmonyPrefix]
        static void Start_Prefix()
        {
            new GameObject("__ItemSpawner__").AddComponent<ItemSpawnerMain>().Init(logger, openMenuKey);
        }
    }


    internal class ItemSpawnerMain : MonoBehaviour
    {
        static ManualLogSource logger;
        static ConfigEntry<KeyboardShortcut> openMenuKey;

        private List<GameObject> ingredients = new List<GameObject>();
        private bool toggleGUI;
        private Vector2 scrollPosition;
        private string searchedItem = "";

        public void Init(ManualLogSource logSource, ConfigEntry<KeyboardShortcut> menuKey)
        {
            logger = logSource;
            openMenuKey = menuKey;
        }

        private void UpdateIngredients()
        {
            this.ingredients.Clear();
            for (int i = 0; i < RecipeManager.Instance.recipesList.Count; i++)
            {
                RecipeManager.RecipeStruct recipeStruct = RecipeManager.Instance.recipesList[i];
                if (recipeStruct == null)
                {
                    logger.LogDebug($"recipesList[{i}] was null");
                    continue;
                }
                for (int j = 0; j < recipeStruct.ingredients.Count; j++)
                {
                    RecipeManager.IngredientStruct ingredientStruct = recipeStruct.ingredients[j];
                    if (ingredientStruct.gameObject == null)
                    {
                        logger.LogDebug($"ingredients[{j}] gameobject null");
                        continue;
                    }
                    if (!this.ingredients.Contains(ingredientStruct.gameObject))
                    {
                        this.ingredients.Add(ingredientStruct.gameObject);
                        logger.LogDebug(Time.time + " | " + ingredientStruct.gameObject.name);
                    }
                }
            }
        }

        private void OnGUI()
        {
            List<GameObject> list = new List<GameObject>();
            try
            {
                if (!this.toggleGUI)
                {
                    return;
                }
                GUI.Box(new Rect(50f, 50f, 350f, (float)(Screen.height - 95)), "");
                this.searchedItem = GUI.TextField(new Rect(75f, 75f, 300f, 30f), this.searchedItem);
                foreach (GameObject gameObject in this.ingredients)
                {
                    if (gameObject.name.ToLower().Contains(this.searchedItem.ToLower()))
                    {
                        list.Add(gameObject);
                    }
                }
            }
            catch (Exception)
            {
            }
            logger.LogDebug(string.Concat(new object[]
            {
                Time.time,
                " | ",
                list.Count,
                " | ",
                this.ingredients.Count
            }));
            this.scrollPosition = GUI.BeginScrollView(new Rect(50f, 125f, 350f, (float)(Screen.height - 175)), this.scrollPosition, new Rect(0f, 0f, 330f, (float)(list.Count * 30 + 100)));
            int num = 0;
            foreach (GameObject gameObject2 in list)
            {
                try
                {
                    if (GUI.Button(new Rect(25f, (float)num, 300f, 25f), gameObject2.name.Replace("_", " ")))
                    {
                        UnityEngine.Object.Instantiate<GameObject>(gameObject2, PlayerManager.Instance.PlayerTransform().position, Quaternion.identity);
                    }
                    num += 35;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }
            }
            GUI.EndScrollView();
        }

        private void Update()
        {
            if (openMenuKey.Value.IsDown())
            {
                this.toggleGUI = !this.toggleGUI;
                if (this.toggleGUI)
                {
                    CursorManager.Instance.ShowCursor();
                }
                else
                {
                    CursorManager.Instance.HideCursor();
                }
                try
                {
                    this.UpdateIngredients();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
    }
}
