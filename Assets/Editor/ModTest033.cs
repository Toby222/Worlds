﻿using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.IO;

public class ModTest033
{
    [Test]
    public void LoadBiomeModTest()
    {
        Manager.UpdateMainThreadReference();

        Debug.Log("loading biome mod file...");

        Biome.ResetBiomes();
        Biome.LoadBiomesFile033(Path.Combine("Mods", "Base", "Biomes", "biomes.json"));

        foreach (Biome biome in Biome.Biomes.Values)
        {
            Debug.Log("generated biome: " + biome.Name);
        }
    }

    [Test]
    public void LoadLayersModTest()
    {
        Manager.UpdateMainThreadReference();

        Debug.Log("loading layer mod file...");

        Layer.ResetLayers();
        Layer.LoadLayersFile033(Path.Combine("Mods", "WeirdBiomesMod", "Layers", "weirdLayers.json"));

        foreach (Layer layer in Layer.Layers.Values)
        {
            Debug.Log("generated layer: " + layer.Name);
        }
    }

    [Test]
    public void LoadRegionAttributeModTest()
    {
        Manager.UpdateMainThreadReference();

        Debug.Log("loading region attribute mod file...");

        Adjective.ResetAdjectives();
        RegionAttribute.ResetAttributes();
        RegionAttribute.LoadRegionAttributesFile033(Path.Combine("Mods", "Base", "RegionAttributes", "region_attributes.json"));

        foreach (RegionAttribute regionAttribute in RegionAttribute.Attributes.Values)
        {
            Debug.Log("generated region attribute: " + regionAttribute.Name);
        }
    }

    [Test]
    public void LoadElementModTest()
    {
        Manager.UpdateMainThreadReference();

        Debug.Log("loading element mod file...");

        Adjective.ResetAdjectives();
        Element.ResetElements();
        Element.LoadElementsFile033(Path.Combine("Mods", "Base", "Elements", "elements.json"));

        foreach (Element element in Element.Elements.Values)
        {
            Debug.Log("generated element: " + element.SingularName);
        }
    }

    [Test]
    public void LoadDiscoveryModTest()
    {
        Manager.UpdateMainThreadReference();
        World.ResetStaticModData();

        Debug.Log("loading discovery mod file...");

        Discovery033.ResetDiscoveries();
        Discovery033.LoadDiscoveriesFile033(Path.Combine("Mods", "Base", "Discoveries", "discoveries.json"));

        foreach (Discovery033 discovery in Discovery033.Discoveries.Values)
        {
            Debug.Log("generated discovery: " + discovery.Name);
        }
    }

    [Test]
    public void ConditionParseTest()
    {
        int condCounter = 1;

        string input = "[ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3";

        Condition condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3)";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "(([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3))";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([NOT]cell_biome_type_presence:water)";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "(([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([NOT]cell_biome_type_presence:water))";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] (([ANY_N_CELL]cell_biome_type_presence:water,0.10) [OR] cell_biome_type_presence:water)";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.10) [OR] ([NOT]cell_biome_type_presence:water)";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.10) [OR] ([NOT]cell_biome_type_presence:water) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.30)";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "(([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.10) [OR] ([NOT]cell_biome_type_presence:water) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.30))";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

        input = "[NOT] (([ANY_N_GROUP]group_has_knowledge:agriculture_knowledge,3) [OR] ([NOT]cell_biome_type_presence:water) [OR] ([ANY_N_CELL]cell_biome_type_presence:water,0.30))";

        condition = Condition.BuildCondition(input);

        Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());
    }

    [Test]
    public void FactorParseTest()
    {
        int factCounter = 1;

        Factor factor = Factor.BuildFactor("[INV]([SQ]cell_biome_type_presence:water)");

        Debug.Log("Test factor " + (factCounter++) + ": " + factor.ToString());

        factor = Factor.BuildFactor("[SQ]([INV]cell_biome_type_presence:water)");

        Debug.Log("Test factor " + (factCounter++) + ": " + factor.ToString());

        factor = Factor.BuildFactor("[SQ]([INV](cell_biome_type_presence:water))");

        Debug.Log("Test factor " + (factCounter++) + ": " + factor.ToString());
    }

    // TODO: This test breaks the test runner for some reason. Investigate
    //[Test]
    //public void ConditionTypeTest()
    //{
    //    Biome.ResetBiomes();
    //    Biome.LoadBiomesFile(Path.Combine("Mods", "Base", "Biomes", "biomes.json"));

    //    Layer.ResetLayers();
    //    Layer.LoadLayersFile(Path.Combine("Mods", "WeirdBiomesMod", "Layers", "weirdLayers.json"));

    //    int condCounter = 1;

    //    string input = "cell_biome_presence:desert,0.3";

    //    Condition condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_biome_most_present:grassland";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "group_population:10000";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_layer_value:mycosystem,20";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_altitude:-1000";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_rainfall:100";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_temperature:-15";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_foraging_capacity:0.5";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());

    //    input = "cell_survivability:0.6";

    //    condition = Condition.BuildCondition(input);

    //    Debug.Log("Test condition " + (condCounter++) + ": " + condition.ToString());
    //}
}
