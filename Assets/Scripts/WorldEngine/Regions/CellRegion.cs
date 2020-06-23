using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

public class CellRegion : Region
{
    public List<WorldPosition> CellPositions;

    private HashSet<TerrainCell> _cells = new HashSet<TerrainCell>();

    private HashSet<TerrainCell> _innerBorderCells = new HashSet<TerrainCell>();

    private HashSet<TerrainCell> _outerBorderCells = new HashSet<TerrainCell>();

    private TerrainCell _mostCenteredCell = null;

    public CellRegion()
    {

    }

    public CellRegion(TerrainCell originCell, Language language) : base(originCell, language)
    {

    }

    public void Update()
    {
        Manager.AddUpdatedCells(_cells, CellUpdateType.Region, CellUpdateSubType.Membership, IsSelected);
    }

    public void AddCells(IEnumerable<TerrainCell> cells)
    {
        foreach (TerrainCell cell in cells)
        {
            AddCell(cell);
        }
    }

    public bool AddCell(TerrainCell cell)
    {
        if (!_cells.Add(cell))
            return false;

        cell.Region = this;

        return true;
    }

    public override ICollection<TerrainCell> GetCells()
    {
        return _cells;
    }

    public override bool IsInnerBorderCell(TerrainCell cell)
    {
        return _innerBorderCells.Contains(cell);
    }

    public void EvaluateAttributes()
    {
        Dictionary<string, float> biomePresences = new Dictionary<string, float>();

        float waterArea = 0;
        float coastalOuterBorderArea = 0;
        float outerBorderArea = 0;

        MinAltitude = float.MaxValue;
        MaxAltitude = float.MinValue;

        AverageOuterBorderAltitude = 0;

        AverageAltitude = 0;
        AverageRainfall = 0;
        AverageTemperature = 0;
        AverageFlowingWater = 0;

        AverageSurvivability = 0;
        AverageForagingCapacity = 0;
        AverageAccessibility = 0;
        AverageArability = 0;

        AverageFarmlandPercentage = 0;

        TotalArea = 0;

        MostBiomePresence = 0;

        _innerBorderCells.Clear();
        _outerBorderCells.Clear();

        foreach (TerrainCell cell in _cells)
        {
            float cellArea = cell.Area;

            bool isInnerBorder = false;

            bool isNotFullyWater = (cell.WaterBiomePresence < 1);

            foreach (TerrainCell nCell in cell.Neighbors.Values)
            {
                if (nCell.Region != this)
                {
                    isInnerBorder = true;

                    if (_outerBorderCells.Add(nCell))
                    {
                        float nCellArea = nCell.Area;

                        outerBorderArea += nCellArea;
                        AverageOuterBorderAltitude += cell.Altitude * nCellArea;

                        if (isNotFullyWater && (nCell.WaterBiomePresence >= 1))
                        {
                            coastalOuterBorderArea += nCellArea;
                        }
                    }
                }
            }

            if (isInnerBorder)
            {
                _innerBorderCells.Add(cell);
            }

            if (MinAltitude > cell.Altitude)
            {
                MinAltitude = cell.Altitude;
            }

            if (MaxAltitude < cell.Altitude)
            {
                MaxAltitude = cell.Altitude;
            }

            AverageAltitude += cell.Altitude * cellArea;
            AverageRainfall += cell.Rainfall * cellArea;
            AverageTemperature += cell.Temperature * cellArea;
            AverageFlowingWater += cell.FlowingWater * cellArea;

            AverageSurvivability += cell.Survivability * cellArea;
            AverageForagingCapacity += cell.ForagingCapacity * cellArea;
            AverageAccessibility += cell.BaseAccessibility * cellArea;
            AverageArability += cell.BaseArability * cellArea;

            AverageFarmlandPercentage += cell.FarmlandPercentage * cellArea;

            foreach (string biomeId in cell.PresentBiomeIds)
            {
                float presenceArea = cell.GetBiomePresence(biomeId) * cellArea;

                if (biomePresences.ContainsKey(biomeId))
                {
                    biomePresences[biomeId] += presenceArea;
                }
                else
                {
                    biomePresences.Add(biomeId, presenceArea);
                }
            }

            foreach (string biomeId in cell.PresentWaterBiomeIds)
            {
                waterArea += cell.GetBiomePresence(biomeId) * cellArea;
            }

            TotalArea += cellArea;
        }

        AverageAltitude /= TotalArea;
        AverageRainfall /= TotalArea;
        AverageTemperature /= TotalArea;
        AverageFlowingWater /= TotalArea;

        AverageSurvivability /= TotalArea;
        AverageForagingCapacity /= TotalArea;
        AverageAccessibility /= TotalArea;
        AverageArability /= TotalArea;

        AverageFarmlandPercentage /= TotalArea;

        WaterPercentage = waterArea / TotalArea;

        AverageOuterBorderAltitude /= outerBorderArea;

        CoastPercentage = coastalOuterBorderArea / outerBorderArea;

        PresentBiomeIds = new List<string>(biomePresences.Count);
        BiomePresences = new List<float>(biomePresences.Count);

        _biomePresences = new Dictionary<string, float>(biomePresences.Count);

        foreach (KeyValuePair<string, float> pair in biomePresences)
        {
            float presence = pair.Value / TotalArea;

            PresentBiomeIds.Add(pair.Key);
            BiomePresences.Add(presence);

            _biomePresences.Add(pair.Key, presence);

            if (MostBiomePresence < presence)
            {
                MostBiomePresence = presence;
                BiomeWithMostPresence = pair.Key;
            }
        }

        //#if DEBUG
        //        if ((Manager.RegisterDebugEvent != null) && (Manager.TracingData.Priority <= 0))
        //        {
        //            //			if ((originCell.Longitude == Manager.TracingData.Longitude) && (originCell.Latitude == Manager.TracingData.Latitude)) {
        //            string regionId = "Id:" + Id;

        //            SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
        //                "CellRegion::EvaluateAttributes - Region: " + regionId,
        //                "CurrentDate: " + World.CurrentDate +
        //                ", cell count: " + _cells.Count +
        //                ", TotalArea: " + TotalArea +
        //                "");

        //            Manager.RegisterDebugEvent("DebugMessage", debugMessage);
        //            //			}
        //        }
        //#endif

        CalculateMostCenteredCell();

        DefineAttributes();
        DefineElements();
    }

    public bool RemoveCell(TerrainCell cell)
    {
        if (!_cells.Remove(cell))
            return false;

        cell.Region = null;
        Manager.AddUpdatedCell(cell, CellUpdateType.Region, CellUpdateSubType.Membership);

        return true;
    }

    public override void Synchronize()
    {
        CellPositions = new List<WorldPosition>(_cells.Count);

        foreach (TerrainCell cell in _cells)
        {
            CellPositions.Add(cell.Position);
        }

        base.Synchronize();
    }

    public override void FinalizeLoad()
    {
        base.FinalizeLoad();

        foreach (WorldPosition position in CellPositions)
        {
            TerrainCell cell = World.GetCell(position);

            if (cell == null)
            {
                throw new System.Exception("Cell missing at position " + position.Longitude + "," + position.Latitude);
            }

            _cells.Add(cell);

            cell.Region = this;
        }

        EvaluateAttributes();
    }

    private void DefineAttributes()
    {
        bool hasAddedAttribute;

        HashSet<RegionAttribute> attributesToSkip = new HashSet<RegionAttribute>();

        // Since there are attributes that have dependencies on other attributes, we might need to test each attribute more than once.
        do
        {
            hasAddedAttribute = false;

            foreach (RegionAttribute r in RegionAttribute.Attributes.Values)
            {
                if (!attributesToSkip.Contains(r) && r.Assignable(this))
                {
                    Info.AddAttribute(r.GetInstanceForRegion(this));
                    hasAddedAttribute = true;

                    attributesToSkip.Add(r); // If the attribute has already been added then we don't need it to test it again
                }
            }
        }
        while (hasAddedAttribute); // Repeat if at least one new attribute was added in the previous loop

        attributesToSkip.Clear();

        // Now validate secondary attributes
        do
        {
            hasAddedAttribute = false;

            foreach (RegionAttribute r in RegionAttribute.SecondaryAttributes.Values)
            {
                if (!attributesToSkip.Contains(r) && r.Assignable(this))
                {
                    Info.AddAttribute(r.GetInstanceForRegion(this));
                    hasAddedAttribute = true;

                    attributesToSkip.Add(r); // If the attribute has already been added then we don't need it to test it again
                }
            }
        }
        while (hasAddedAttribute); // Repeat if at least one new attribute was added in the previous loop
    }

    private void DefineElements()
    {
        foreach (Element e in Element.Elements.Values)
        {
            if (e.Assignable(this))
            {
                Info.AddElement(e.GetInstanceForRegion(this));
            }
        }
    }

    private void CalculateMostCenteredCell()
    {
        int centerLongitude = 0, centerLatitude = 0;

        foreach (TerrainCell cell in _cells)
        {
            centerLongitude += cell.Longitude;
            centerLatitude += cell.Latitude;
        }

        centerLongitude /= _cells.Count;
        centerLatitude /= _cells.Count;

        TerrainCell closestCell = null;
        int closestDistCenter = int.MaxValue;

        foreach (TerrainCell cell in _cells)
        {
            int distCenter = Mathf.Abs(cell.Longitude - centerLongitude) + Mathf.Abs(cell.Latitude - centerLatitude);

            if ((closestCell == null) || (distCenter < closestDistCenter))
            {
                closestDistCenter = distCenter;
                closestCell = cell;
            }
        }

        _mostCenteredCell = closestCell;
    }

    public override TerrainCell GetMostCenteredCell()
    {
        return _mostCenteredCell;
    }

    // Notes:
    // - find borders
    // - after adding all possible cells to region check areas enclosed by borders
    // --- if area smaller than a percentage of region, add to region
    // ------ if the area contained a region, merge that region into larger region
    // - neighbors are associated to borders
    // - mark regions that are too small to be added to bigger bordering regions
    // - jump over one cell wide areas
    // - do not add to region one cell wide areas unless surrounded by ocean
    // - create regions for areas adjacent to cell groups even if unhabited
    // - a region size should be limited by the discovering polity exploration range
    // - if a polity expands beyond a region border into unexplored ares then the new
    //   areas should be added to the region if they are similar

    public static bool CanAddCellToBiomeRegion(TerrainCell cell, string biomeId)
    {
        if (cell.Region != null)
        {
            return false;
        }

        string cellBiomeId = cell.BiomeWithMostPresence;

        return cellBiomeId == biomeId;
    }

    private struct RegionExplorationParameters
    {
        public string BiomeId;
        public HashSet<TerrainCell> AcceptedCells;
        public HashSet<TerrainCell> ExploredCells;
        public Queue<TerrainCell> CellsToExplore;
        public int RegionSize;
    }

    //private struct Border
    //{
    //    public int Id;

    //    public HashSet<TerrainCell> Cells;

    //    public bool Encloses;

    //    public Stack<TerrainCell> NonEnclosedCells;

    //    public int Size;

    //    public int Top;
    //    public int Bottom;
    //    public int Left;
    //    public int Right;

    //    public Border(TerrainCell startCell)
    //    {
    //        Id = _borderCount++;
    //        Cells = new HashSet<TerrainCell>();
    //        NonEnclosedCells = new Stack<TerrainCell>();
    //        Encloses = false;

    //        Cells.Add(startCell);

    //        Size = 0;

    //        Top = startCell.Latitude;
    //        Bottom = startCell.Latitude;
    //        Left = startCell.Longitude;
    //        Right = startCell.Longitude;

    //        NonEnclosedCells.Push(startCell);
    //    }

    //    public void AddCell(TerrainCell cell)
    //    {
    //        Cells.Add(cell);


    //    }
    //}

    //private static int _borderCount = 0;
    //private static Dictionary<int, Border> _borders;

    //private static Border CreateBorder(TerrainCell startCell)
    //{
    //    Border b = new Border(startCell);

    //    _borders.Add(b.Id, b);

    //    return b;
    //}

    private static void ExploreBorder(
        TerrainCell startCell,
        RegionExplorationParameters p)
    {
        //Border border = CreateBorder(startCell);

        HashSet<TerrainCell> borderExploredCells = new HashSet<TerrainCell>();

        Queue<TerrainCell> borderCellsToExplore = new Queue<TerrainCell>();

        HashSet<TerrainCell> inBorderCells = new HashSet<TerrainCell>();
        HashSet<TerrainCell> outBorderCells = new HashSet<TerrainCell>();

        borderCellsToExplore.Enqueue(startCell);
        p.ExploredCells.Add(startCell);
        borderExploredCells.Add(startCell);

        while (borderCellsToExplore.Count > 0)
        {
            TerrainCell cell = borderCellsToExplore.Dequeue();

            inBorderCells.Clear();
            outBorderCells.Clear();

            // first separate neighbor cells that are inside and outside border
            foreach (TerrainCell nc in cell.Neighbors.Values)
            {
                if (borderExploredCells.Contains(nc))
                {
                    continue;
                }

                if (CanAddCellToBiomeRegion(nc, p.BiomeId))
                {
                    inBorderCells.Add(nc);
                }
                else
                {
                    outBorderCells.Add(nc);
                }

                p.ExploredCells.Add(nc);
                borderExploredCells.Add(nc);
            }

            // now find which neighbor cells are exactly in the border
            foreach (TerrainCell cellIn in inBorderCells)
            {
                bool isBorder = false;

                // find if any of the neighbor to the neighbor is an cell outside
                foreach (TerrainCell nc in cellIn.Neighbors.Values)
                {
                    if (outBorderCells.Contains(nc))
                    {
                        isBorder = true;
                        break;
                    }
                }

                if (isBorder)
                {
                    borderCellsToExplore.Enqueue(cellIn);
                }
                else
                {
                    p.CellsToExplore.Enqueue(cellIn);
                }
            }

            //border.Cells.Add(cell);
            p.AcceptedCells.Add(cell);
            p.RegionSize++;
        }
    }

    public static Region TryGenerateBiomeRegion(
        TerrainCell startCell,
        Language language,
        string biomeId)
    {
        RegionExplorationParameters p = new RegionExplorationParameters()
        {
            BiomeId = biomeId,
            ExploredCells = new HashSet<TerrainCell>(),
            AcceptedCells = new HashSet<TerrainCell>(),
            CellsToExplore = new Queue<TerrainCell>(),
            RegionSize = 0,
        };

        p.CellsToExplore.Enqueue(startCell);
        p.ExploredCells.Add(startCell);

        //_borderCount = 0;
        //_borders = new Dictionary<int, Border>();

        while (p.CellsToExplore.Count > 0)
        {
            TerrainCell cell = p.CellsToExplore.Dequeue();

            bool foundRejected = false;

            foreach (TerrainCell nCell in cell.Neighbors.Values)
            {
                if (p.ExploredCells.Contains(nCell))
                {
                    continue;
                }

                if (CanAddCellToBiomeRegion(nCell, biomeId))
                {
                    p.CellsToExplore.Enqueue(nCell);
                }
                else
                {
                    foundRejected = true;
                }

                p.ExploredCells.Add(nCell);
            }

            if (foundRejected)
            {
                ExploreBorder(cell, p);
            }
            else
            {
                p.AcceptedCells.Add(cell);
                p.RegionSize++;
            }
        }

        CellRegion region = new CellRegion(startCell, language);

        region.AddCells(p.AcceptedCells);

        region.EvaluateAttributes();

        region.Update();

        return region;
    }

    // older versions of TryGenerateBiomeRegion

    public static Region TryGenerateBiomeRegion_reduced(TerrainCell startCell, Language establishmentLanguage, string biomeId)
    {
        int regionSize = 1;

        HashSet<CellRegion> borderingRegions = new HashSet<CellRegion>();

        HashSet<TerrainCell> acceptedCells = new HashSet<TerrainCell>();
        HashSet<TerrainCell> rejectedCells = new HashSet<TerrainCell>();
        HashSet<TerrainCell> exploredCells = new HashSet<TerrainCell>();

        acceptedCells.Add(startCell);
        exploredCells.Add(startCell);

        Queue<TerrainCell> cellsToExplore = new Queue<TerrainCell>();

        foreach (TerrainCell cell in startCell.Neighbors.Values)
        {
            cellsToExplore.Enqueue(cell);
            exploredCells.Add(cell);
        }

        int borderCells = 0;

        while (cellsToExplore.Count > 0)
        {
            TerrainCell cell = cellsToExplore.Dequeue();

            bool accepted = false;

            string cellBiomeId = cell.BiomeWithMostPresence;

            if (cell.Region != null) // if cell belongs to another region, reject, but add region to neighbors
            {
                borderingRegions.Add(cell.Region as CellRegion);
            }
            else if (cellBiomeId == biomeId) // if cell has target biome, accept
            {
                accepted = true;
            }

            if (accepted)
            {
                acceptedCells.Add(cell);
                regionSize++;

                foreach (TerrainCell nCell in cell.Neighbors.Values)
                {
                    if (rejectedCells.Contains(nCell))
                    {
                        // give another chance;
                        rejectedCells.Remove(nCell);
                        borderCells--;
                    }
                    else if (exploredCells.Contains(nCell))
                    {
                        continue;
                    }

                    cellsToExplore.Enqueue(nCell);
                    exploredCells.Add(nCell);
                }
            }

            if (!accepted)
            {
                rejectedCells.Add(cell);
                borderCells++;
            }
        }

        CellRegion region = null;

        int minRegionSize = 20;

        if ((regionSize <= minRegionSize) && (borderingRegions.Count > 0))
        {
            _rngOffset = RngOffsets.REGION_SELECT_BORDER_REGION_TO_REPLACE_WITH;
            _startCell = startCell;

            region = borderingRegions.RandomSelect(GetRandomInt);

            region.ResetInfo();
        }
        else
        {
            region = new CellRegion(startCell, establishmentLanguage);
        }

        region.AddCells(acceptedCells);

        region.EvaluateAttributes();

        region.Update();

        return region;
    }

    public static Region TryGenerateBiomeRegion_original(TerrainCell startCell, Language establishmentLanguage, string biomeId)
    {
        int regionSize = 1;

        HashSet<CellRegion> borderingRegions = new HashSet<CellRegion>();

        // round the base altitude to a multiple of AltitudeRoundnessTarget
        float baseAltitude = AltitudeRoundnessTarget * Mathf.Round(startCell.Altitude / AltitudeRoundnessTarget);

        HashSet<TerrainCell> acceptedCells = new HashSet<TerrainCell>();
        HashSet<TerrainCell> rejectedCells = new HashSet<TerrainCell>();
        HashSet<TerrainCell> exploredCells = new HashSet<TerrainCell>();

        acceptedCells.Add(startCell);
        exploredCells.Add(startCell);

        Queue<TerrainCell> cellsToExplore = new Queue<TerrainCell>();

        foreach (TerrainCell cell in startCell.Neighbors.Values)
        {
            cellsToExplore.Enqueue(cell);
            exploredCells.Add(cell);
        }

        int borderCells = 0;

        while (cellsToExplore.Count > 0)
        {
            int toExploreCount = cellsToExplore.Count;

            float closedness = 1 - (toExploreCount / (float)(toExploreCount + borderCells));

            TerrainCell cell = cellsToExplore.Dequeue();

            float closednessFactor = 1;
            float maxClosednessFactor = MaxClosedness / 2f;

            if (MaxClosedness < 1)
            {
                closednessFactor =
                    ((1 + maxClosednessFactor) *
                    (1 - closedness) / (1 - MaxClosedness)) -
                    maxClosednessFactor;
            }

            float maxAltitudeDifference = BaseMaxAltitudeDifference * closednessFactor;

            bool accepted = false;

            string cellBiomeId = cell.BiomeWithMostPresence;

            if (cell.Region != null) // if cell belongs to another region, reject, but add region to neighbors
            {
                borderingRegions.Add(cell.Region as CellRegion);
            }
            else if (cellBiomeId == biomeId) // if cell has target biome, accept
            {
                accepted = true;
            }
            else // if cell is surrounded by a majority of cells with target biome, accept
            {
                int nSurroundCount = 0;
                int minNSurroundCount = 3;

                foreach (TerrainCell nCell in cell.Neighbors.Values)
                {
                    if ((nCell.BiomeWithMostPresence == biomeId) || acceptedCells.Contains(nCell))
                    {
                        nSurroundCount++;
                    }
                    else
                    {
                        nSurroundCount = 0;
                    }
                }

                foreach (TerrainCell nCell in cell.Neighbors.Values)
                {
                    if ((nCell.BiomeWithMostPresence == biomeId) || acceptedCells.Contains(nCell))
                    {
                        nSurroundCount++;
                    }
                    else
                    {
                        nSurroundCount = 0;
                    }
                }

                int secondRepeatCount = 1;
                foreach (TerrainCell nCell in cell.Neighbors.Values)
                {
                    // repeat until minNSurroundCount
                    if (secondRepeatCount >= minNSurroundCount)
                        break;

                    if (nCell.BiomeWithMostPresence == biomeId)
                    {
                        nSurroundCount++;
                    }
                    else
                    {
                        nSurroundCount = 0;
                    }

                    secondRepeatCount++;
                }

                if (nSurroundCount >= minNSurroundCount)
                {
                    accepted = true;
                }
            }

            if (accepted)
            {
                if (Mathf.Abs(cell.Altitude - baseAltitude) < maxAltitudeDifference)
                {
                    acceptedCells.Add(cell);
                    regionSize++;

                    foreach (TerrainCell nCell in cell.Neighbors.Values)
                    {
                        if (rejectedCells.Contains(nCell))
                        {
                            // give another chance;
                            rejectedCells.Remove(nCell);
                            borderCells--;
                        }
                        else if (exploredCells.Contains(nCell))
                        {
                            continue;
                        }

                        cellsToExplore.Enqueue(nCell);
                        exploredCells.Add(nCell);
                    }
                }
                else
                {
                    accepted = false;
                }
            }

            if (!accepted)
            {
                rejectedCells.Add(cell);
                borderCells++;
            }
        }

        CellRegion region = null;

        int minRegionSize = 20;

        if ((regionSize <= minRegionSize) && (borderingRegions.Count > 0))
        {
            _rngOffset = RngOffsets.REGION_SELECT_BORDER_REGION_TO_REPLACE_WITH;
            _startCell = startCell;

            region = borderingRegions.RandomSelect(GetRandomInt);

            region.ResetInfo();
        }
        else
        {
            region = new CellRegion(startCell, establishmentLanguage);
        }

        region.AddCells(acceptedCells);

        region.EvaluateAttributes();

        region.Update();

        return region;
    }
}
