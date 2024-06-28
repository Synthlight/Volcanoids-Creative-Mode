using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Base_Mod;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Creative_Mode;

[UsedImplicitly]
public class EnableDevMode : BaseGameMod {
    protected override bool UseHarmony => true;

    private static readonly GUID CRAFTING_TABLE_PROD         = GUID.Parse("f61e601e773ff884497d265728ceacaa");
    private static readonly GUID CRAFTING_TABLE_REF          = GUID.Parse("655ac6f1bdb43da4f9ee18c6285b41ff");
    private static readonly GUID CRAFTING_TABLE_SCI          = GUID.Parse("493fb6e4e66a02a4a8c512e0b94b5414");
    private static readonly GUID CRAFTING_STATION_PROD       = GUID.Parse("7c32d187420152f4da3a79d465cbe87a");
    private static readonly GUID CRAFTING_STATION_REF        = GUID.Parse("3b35b8f4f39847945b9881e25bb01f5a");
    private static readonly GUID CRAFTING_STATION_RES        = GUID.Parse("a7764724dfb030a47a531f7c5e87ff9e");
    private static readonly GUID CRAFTING_STATION_SCRAP      = GUID.Parse("a28879ce9c7caab40a38781abb6af9ae");
    private static readonly GUID CRAFTING_STATION_WIDE_PROD  = GUID.Parse("a985f4262370f4049a3943415f8fe308");
    private static readonly GUID CRAFTING_STATION_WIDE_REF   = GUID.Parse("2db0a28025acc3f439e680c86bcaa9fb");
    private static readonly GUID CRAFTING_STATION_WIDE_RES   = GUID.Parse("5bfc62bce5a139347ae57a7c2515bae1");
    private static readonly GUID CRAFTING_STATION_WIDE_SCRAP = GUID.Parse("b3a64a40be848f842b29510b14e80182");
    private static readonly GUID CRAFTING_HUB_PROD           = GUID.Parse("ca0964a43824b38468eed492d2385ec4");
    private static readonly GUID CRAFTING_HUB_REF            = GUID.Parse("d4446b96f5a46494e8bed91cc40c06b7");
    private static readonly GUID CRAFTING_HUB_RES            = GUID.Parse("00175574f3d8b8c41b2da96cd19cfc40");
    private static readonly GUID CRAFTING_HUB_SCRAP          = GUID.Parse("b4fbc6dbf6156184383895f886d838bd");

    private static readonly List<GUID> CRAFTERS = [
        CRAFTING_TABLE_PROD,
        CRAFTING_TABLE_REF,
        CRAFTING_TABLE_SCI,
        CRAFTING_STATION_PROD,
        CRAFTING_STATION_REF,
        CRAFTING_STATION_RES,
        CRAFTING_STATION_SCRAP,
        CRAFTING_STATION_WIDE_PROD,
        CRAFTING_STATION_WIDE_REF,
        CRAFTING_STATION_WIDE_RES,
        CRAFTING_STATION_WIDE_SCRAP,
        CRAFTING_HUB_PROD,
        CRAFTING_HUB_REF,
        CRAFTING_HUB_RES,
        CRAFTING_HUB_SCRAP
    ];

    public override void OnInitData() {
        var categoryList = (from recipe in RuntimeAssetDatabase.Get<Recipe>()
                            where recipe.Categories is {Length: > 0}
                            from category in recipe.Categories
                            where !string.IsNullOrEmpty(category.name)
                            orderby category.name
                            select category).Distinct()
                                            .ToList();

        // Production
        var newProdCategoryList = (from category in categoryList
                                   where category.name is "CraftingTier1"
                                       or "ProductionTier1"
                                       or "CraftingTierSubmarine"
                                       or "ProductionTierSubmarine"
                                   select category).ToArray();

        // Research
        var newResearchCategoryList = (from category in categoryList
                                       where category.name is "ResearchTier1"
                                           or "CraftingResearchTier1"
                                       select category).ToArray();

        // Refinery
        var newRefineryCategoryList = (from category in categoryList
                                       where category.name is "RefinementTier1"
                                           or "CraftingRefineryTier1"
                                       select category).ToArray();

        // Scrap
        var newScrapCategoryList = new[] {categoryList.GetCategory("ScrapTier1")};

        foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()) {
            // Do first so even if it's one that should be excluded, it's still 'free'.
            recipe.Inputs           = [];
            recipe.RequiredUpgrades = [];
            recipe.ProductionTime   = 1f;

            // Hide worktable variations of recipes.
            if (recipe.name.Contains("WorktableRecipe") && recipe.name != "WorktableRecipe"
                // And the x2/x5 variants.
                || recipe.name.EndsWith("Recipe2")) {
                recipe.Categories = [];
                continue;
            }

            if (recipe.Categories.Contains("CraftingTier") || recipe.Categories.Contains("ProductionTier")) {
                recipe.Categories = newProdCategoryList;
            }

            if (recipe.Categories.Contains("CraftingResearch") || recipe.Categories.Contains("ResearchTier")) {
                recipe.Categories = newResearchCategoryList;
            }

            if (recipe.Categories.Contains("CraftingRefinery") || recipe.Categories.Contains("RefinementTier")) {
                recipe.Categories = newRefineryCategoryList;
            }

            if (recipe.Categories.Contains("ScrapTier")) {
                recipe.Categories = newScrapCategoryList;
            }
        }

        foreach (var recipe in RuntimeAssetDatabase.Get<RecipeUnlockGroup>()) {
            recipe.Items   = [];
            recipe.Recipes = [];
        }

        foreach (var prefab in from def in RuntimeAssetDatabase.Get<ItemDefinition>()
                               where CRAFTERS.Contains(def.AssetId)
                               where def.Prefabs.Length > 0
                               select def.Prefabs[0]) {
            if (prefab.TryGetComponent<FactoryStation>(out var factoryStation)) {
                factoryStation.TimeEfficiency = 1;
            }
            if (prefab.TryGetComponent<Worktable>(out var worktable)) {
                worktable.TimeEfficiency = 10; // Because they are slower for whatever reason and 1f doesn't fix it.
            }
            if (prefab.TryGetComponent<Upgradable>(out var upgradable)) {
                upgradable.m_cost             = [];
                upgradable.m_requiredUpgrades = [];
                upgradable.m_upgradeDuration  = 1f;
            }
        }

        base.OnInitData();
    }
}

// Workaround for: https://discord.com/channels/444244464903651348/589065025138851850/1255977550707163156
[HarmonyPatch]
[UsedImplicitly]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class FixIndexOutOfRangeException {
    [HarmonyTargetMethod]
    [UsedImplicitly]
    public static MethodBase TargetMethod() {
        return typeof(ProducerSync).GetProperty(nameof(ProducerSync.FirstInputIcon), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod();
    }

    [UsedImplicitly]
    [HarmonyPostfix]
    public static bool Prefix(ref Sprite __result) {
        __result = null;
        return false;
    }
}

public static class Extensions {
    public static bool Contains(this RecipeCategory[] categories, string name, bool partialMatch = true) {
        if (categories == null || categories.Length == 0) return false;
        foreach (var category in categories) {
            switch (partialMatch) {
                case true when category.name.Contains(name):
                case false when category.name == name:
                    return true;
            }
        }
        return false;
    }

    public static RecipeCategory GetCategory(this IEnumerable<RecipeCategory> categories, string name) {
        return categories.FirstOrDefault(category => category.name == name);
    }
}