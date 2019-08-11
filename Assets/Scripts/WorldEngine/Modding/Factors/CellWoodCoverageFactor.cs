﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CellWoodPresenceFactor : Factor
{
    public const string Regex = @"^\s*cell_wood_coverage\s*$";

    public CellWoodPresenceFactor(Match match)
    {
    }

    public override float Calculate(CellGroup group)
    {
        return Calculate(group.Cell);
    }

    public override float Calculate(TerrainCell cell)
    {
        return cell.WoodCoverage;
    }

    public override string ToString()
    {
        return "'Cell Wood Presence' Factor";
    }
}
