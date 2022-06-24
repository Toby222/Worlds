# Biome Modding guide

**Biome** mod files are located within the *Biomes* folder. To be valid, mod files must have the **.json** extension and have the following file structure:

### File Structure

```
{
  "biomes": [ -- list of biomes --
    {
      "id":                         -- (required) Unique biome identifier, if more than one biome definition share ids, only the last one loaded will be used
      "name":                       -- (required) String to be used by the game's UI. Please avoid using overlong strings or non-unicode characters
      "color":                      -- (required) HTML color code ("#RRGGBB") used to represent biome on map (transparency values, if added, will be ignored)
      "type":                       -- (required) Indicates if the biome is land-based or sea-based (accepted values: "land", "water")
      "traits":                     -- (optional) List of zero or more additional traits (accepted values: "wood","sea","ice")

      "minAltitude":                -- (optional) Minimum cell altitude at which this biome can be present, a value of '0' refers to sea level (meters)
      "maxAltitude":                -- (optional) Maximum cell altitude at which this biome can be present, a value of '0' refers to sea level (meters)
      "altitudeSaturationSlope":    -- (optional) Slope with which the biome will reach max altitude saturation within the cell (between 0.001 and 1000 inclusive) (see note #7)
      "minRainfall":                -- (optional) Minimum cell rainfall at which this biome can be present (mm per m^2 per year)
      "maxRainfall":                -- (optional) Maximum cell rainfall at which this biome can be present (mm per m^2 per year)
      "minFlowingWater":            -- (optional) Minimum cell non-sea flowing water accumulation at which this biome can be present (mm per m^2)
      "maxFlowingWater":            -- (optional) Maximum cell non-sea flowing water accumulation at which this biome can be present (mm per m^2)
      "waterSaturationSlope":       -- (optional) Slope with which the biome will reach max rainfall or moisture saturation within the cell (between 0.001 and 1000 inclusive) (see note #7)
      "minTemperature":             -- (optional) Minimum cell temperature at which this biome can be present (centigrade, yearly average)
      "maxTemperature":             -- (optional) Maximum cell temperature at which this biome can be present (centigrade, yearly average)
      "temperatureSaturationSlope": -- (optional) Slope with which the biome will reach max temperature saturation within the cell (between 0.001 and 1000 inclusive) (see note #7)

      "survivability":              -- (required) Base probability of a given human group surviving in this biome (between 0 and 1 inclusive)
      "foragingCapacity":           -- (required) Base foraging capacity this cell provides to a human group (between 0 and 1 inclusive)
      "accessibility":              -- (required) Base travel modifier through and within this cell (between 0 and 1 inclusive)
      "arability":                  -- (required) Base arability modifier for this cell (between 0 and 1 inclusive)

      "layerConstraints": [
        {
          "layerId":                -- (required) Id of the layer to use as constraint
          "minValue":               -- (optional) Minimum layer value in cell at which this biome can be present (between -1000000 and 1000000 inclusive) (see note #4 and #6)
          "maxValue":               -- (optional) Maximum layer value in cell at which this biome can be present (between -1000000 and 1000000 inclusive) (see note #5 and #6)
          "saturationSlope":        -- (optional) Slope with which the biome will reach max saturation within the cell (between 0.1 and 1 inclusive) (see note #7)
        }
      ]
    },
    ...                             -- additional biomes --
  ]
}
```

## Notes
1. Remove any trailing commas or the file won't be parsed
2. Optional **min** attribute values (altitude/rainfall/temperature) not defined will be assigned the minimum attribute value of its category
3. Optional **max** attribute values (altitude/rainfall/temperature) not defined will be assigned the maximum attribute value of its category
4. Only set a **minValue** if the biome is not supposed to be present at values less than minValue. Otherwise do not add this constraint property
5. Only set a **maxValue** if the biome is not supposed to be present at values greater than maxValue. Otherwise do not add this constraint property
6. A layer constraint's **minValue** or **maxValue** shouldn't be greater than the specific layer's **maxPossibleValue**
7. **Saturation** represents how close is particular biome from filling a particular cell and it helps calculate the relative strength of a particular biome in regards to others within a single cell. The saturation slope indicates how quickly a particular biome reaches its saturation point within a cell. A saturation slope of **0** indicates that the biome never increases its saturation level above **0**. A  slope of **1** indicates that the biome reaches its maximum saturation at the exact midpoint between the minimum and maximum values of a particular property. A slope greater than **1** indicates the biome reaches its maximum saturation faster. A slope less than **1** indicates the biome won't reach its saturation point. When undefined, the default saturation slope is **1**

--
