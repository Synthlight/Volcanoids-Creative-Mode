using System;
using System.Collections.Generic;
using System.Linq;
using Base_Mod;
using JetBrains.Annotations;

namespace Creative_Mode;

[UsedImplicitly]
public class EnableDevMode : BaseGameMod {
    public override void OnInitData() {
        var categoryList = (from recipe in RuntimeAssetDatabase.Get<Recipe>()
                            where recipe.Categories != null && recipe.Categories.Length > 0
                            from category in recipe.Categories
                            where !string.IsNullOrEmpty(category.name)
                            orderby category.name
                            select category).Distinct()
                                            .ToList();

        // Production
        var newProdCategoryList = (from category in categoryList
                                   where category.name == "CraftingTier1"
                                         || category.name == "ProductionTier1"
                                         || category.name == "CraftingTierSubmarine"
                                         || category.name == "ProductionTierSubmarine"
                                   select category).ToArray();

        // Research
        var newResearchCategoryList = (from category in categoryList
                                       where category.name == "ResearchTier1"
                                             || category.name == "CraftingResearchTier1"
                                       select category).ToArray();

        // Refinery
        var newRefineryCategoryList = (from category in categoryList
                                       where category.name == "RefinementTier1"
                                             || category.name == "CraftingRefineryTier1"
                                       select category).ToArray();

        // Scrap
        var newScrapCategoryList = new[] {categoryList.GetCategory("ScrapTier1")};

        foreach (var recipe in RuntimeAssetDatabase.Get<Recipe>()) {
            // Hide worktable variations of recipes.
            if (recipe.name.Contains("WorktableRecipe") && recipe.name != "WorktableRecipe"
                // And the x2/x5 variants.
                || recipe.name.EndsWith("Recipe2")) {
                recipe.Categories = Array.Empty<RecipeCategory>();
                continue;
            }

            recipe.Inputs           = Array.Empty<InventoryItem>();
            recipe.RequiredUpgrades = Array.Empty<ItemDefinition>();
            recipe.ProductionTime   = 1f;

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

        base.OnInitData();
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