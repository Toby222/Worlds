﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class Route : ISynchronizable {

	public List<WorldPosition> CellPositions = new List<WorldPosition> ();

	[XmlAttribute]
	public float Length = 0;

	[XmlAttribute]
	public bool Consolidated = false;

	[XmlAttribute("MigDir")]
	public int MigrationDirectionInt = -1;

	[XmlIgnore]
	public World World;

	[XmlIgnore]
	public TerrainCell FirstCell;

	[XmlIgnore]
	public TerrainCell LastCell;

	[XmlIgnore]
	public List<TerrainCell> Cells = new List<TerrainCell> ();

	public Direction MigrationDirection {
		get { return (Direction)MigrationDirectionInt; }
	}

	private const float CoastPreferenceIncrement = 400;

	private Direction _traverseDirection;

	private bool _isTraversingSea;
	private float _currentDirectionOffset;

	private float _currentCoastPreference;
	private float _currentEndRoutePreference;

	public Route () {
	
	}

	public Route (TerrainCell startCell) {

		World = startCell.World;

		FirstCell = startCell;
	
		Build ();
	}

	public void Destroy () {

		if (!Consolidated)
			return;
	
		foreach (TerrainCell cell in Cells) {
		
			cell.RemoveCrossingRoute (this);
			Manager.AddUpdatedCell (cell, CellUpdateType.Route);
		}
	}

	public void Reset () {

		if (!Consolidated)
			return;

		foreach (TerrainCell cell in Cells) {

			cell.RemoveCrossingRoute (this);
			Manager.AddUpdatedCell (cell, CellUpdateType.Route);
		}

		CellPositions.Clear ();
		Cells.Clear ();

		Consolidated = false;
	}

	public void Build () {

		_isTraversingSea = false;
		_currentEndRoutePreference = 0;
		_currentDirectionOffset = 0;
		_currentCoastPreference = CoastPreferenceIncrement;
		_currentEndRoutePreference = 0;

		AddCell (FirstCell);
		LastCell = FirstCell;

		TerrainCell nextCell = FirstCell;
		Direction nextDirection;

		int rngOffset = 0;

		while (true) {

			#if DEBUG
			TerrainCell prevCell = nextCell;
			#endif

			nextCell = ChooseNextSeaCell (nextCell, rngOffset++, out nextDirection);

//			#if DEBUG
//			if (Manager.RegisterDebugEvent != null) {
//				if ((FirstCell.Longitude == Manager.TracingData.Longitude) && (FirstCell.Latitude == Manager.TracingData.Latitude)) {
//
//					string cellPos = "Position: Long:" + FirstCell.Longitude + "|Lat:" + FirstCell.Latitude;
//					string prevCellDesc = "Position: Long:" + prevCell.Longitude + "|Lat:" + prevCell.Latitude;
//
//					string nextCellDesc = "Null";
//
//					if (nextCell != null) {
//						nextCellDesc = "Position: Long:" + nextCell.Longitude + "|Lat:" + nextCell.Latitude;
//					}
//
//					SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//						"ChooseNextSeaCell - FirstCell:" + cellPos,
//						"CurrentDate: " + World.CurrentDate + 
//						", prevCell: " + prevCellDesc + 
//						", nextCell: " + nextCellDesc + 
//						"");
//
//					Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//				}
//			}
//			#endif

			if (nextCell == null) {
				LastCell = nextCell;
				break;
			}

			Length += LastCell.NeighborDistances [nextDirection];

			AddCell (nextCell);

			if (nextCell.GetBiomePresence (Biome.Ocean) <= 0)
				break;

			LastCell = nextCell;
		}

		if (nextCell != null) {
			MigrationDirectionInt = (int)LastCell.GetDirection (nextCell);
		}
		
		LastCell = nextCell;
	}

	public void Consolidate () {

		if (Consolidated)
			return;

		foreach (TerrainCell cell in Cells) {

			cell.AddCrossingRoute (this);
			Manager.AddUpdatedCell (cell, CellUpdateType.Route);
		}

		Consolidated = true;
	}

	public void AddCell (TerrainCell cell) {
	
		Cells.Add (cell);
		CellPositions.Add (cell.Position);
	}
		
	public TerrainCell ChooseNextSeaCell (TerrainCell currentCell, int rngOffset, out Direction direction) {

		if (_isTraversingSea)
			return ChooseNextDepthSeaCell (currentCell, rngOffset, out direction);
		else
			return ChooseNextCoastalCell (currentCell, rngOffset, out direction);
	}

	public TerrainCell ChooseNextDepthSeaCell (TerrainCell currentCell, int rngOffset, out Direction direction) {

		Direction newDirection = _traverseDirection;
		float newOffset = _currentDirectionOffset;

		float deviation = 2 * FirstCell.GetNextLocalRandomFloat (RngOffsets.ROUTE_CHOOSE_NEXT_DEPTH_SEA_CELL + rngOffset) - 1;
		deviation = (deviation * deviation + 1f) / 2f;
		deviation = newOffset - deviation;

//		LatitudeDirectionModifier (currentCell, out newDirection, out newOffset);

		if (deviation >= 0.5f) {
			newDirection = (Direction)(((int)_traverseDirection + 1) % 8);
		} else if (deviation < -0.5f) {
			newDirection = (Direction)(((int)_traverseDirection + 6) % 8);
		} else if (deviation < 0) {
			newDirection = (Direction)(((int)_traverseDirection + 7) % 8);
		}

		TerrainCell nextCell = currentCell.GetNeighborCell (newDirection);
		direction = newDirection;

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			if ((FirstCell.Longitude == Manager.TracingData.Longitude) && (FirstCell.Latitude == Manager.TracingData.Latitude)) {
//
//				string cellPos = "Position: Long:" + FirstCell.Longitude + "|Lat:" + FirstCell.Latitude;
//
//				string nextCellDesc = "Null";
//
//				if (nextCell != null) {
//					nextCellDesc = "Position: Long:" + nextCell.Longitude + "|Lat:" + nextCell.Latitude;
//				}
//
//				SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//					"ChooseNextDepthSeaCell - FirstCell:" + cellPos,
//					"CurrentDate: " + World.CurrentDate + 
//					", deviation: " + deviation + 
//					", nextCell: " + nextCellDesc + 
//					"");
//
//				Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//			}
//		}
//		#endif

		if (nextCell == null)
			return null;

		if (Cells.Contains (nextCell))
			return null;

		if (nextCell.IsPartOfCoastline) {
			
			_currentCoastPreference += CoastPreferenceIncrement;
			_currentEndRoutePreference += 0.1f;

			_isTraversingSea = false;
		}

		return nextCell;
	}

	private class CoastalCellValue : CollectionUtility.ElementWeightPair<KeyValuePair<Direction, TerrainCell>> {

		public CoastalCellValue (KeyValuePair<Direction, TerrainCell> pair, float weight) : base (pair, weight) {
			
		}
	}

	public TerrainCell ChooseNextCoastalCell (TerrainCell currentCell, int rngOffset, out Direction direction) {

		float totalWeight = 0;

		List<CoastalCellValue> coastalCellWeights = new List<CoastalCellValue> (currentCell.Neighbors.Count);

//		#if DEBUG
//		string cellWeightsStr = "";
//		#endif

		foreach (KeyValuePair<Direction, TerrainCell> nPair in currentCell.Neighbors) {

			TerrainCell nCell = nPair.Value;

			if (Cells.Contains (nCell))
				continue;

			float oceanPresence = nCell.GetBiomePresence (Biome.Ocean);

			float weight = oceanPresence;

			if (nCell.IsPartOfCoastline)
				weight *= _currentCoastPreference;

			weight += (1f - oceanPresence) * _currentEndRoutePreference;

			coastalCellWeights.Add (new CoastalCellValue(nPair, weight));

//			#if DEBUG
//			cellWeightsStr += "\n\tnCell Direction: " + nPair.Key + " - Position: " + nCell.Position + " - Weight: " + weight;
//			#endif

			totalWeight += weight;
		}

//		#if DEBUG
//		cellWeightsStr += "\n";
//		#endif

		if (coastalCellWeights.Count == 0) {

			direction = Direction.South;
		
			return null;
		}

		if (totalWeight <= 0) {

			direction = Direction.South;
		
			return null;
		}

		KeyValuePair<Direction, TerrainCell> targetPair = 
			CollectionUtility.WeightedSelection (coastalCellWeights.ToArray (), totalWeight, () => FirstCell.GetNextLocalRandomFloat (RngOffsets.ROUTE_CHOOSE_NEXT_COASTAL_CELL + rngOffset));

		TerrainCell targetCell = targetPair.Value;
		direction = targetPair.Key;

		if (targetCell == null) {
			throw new System.Exception ("targetCell is null");
		}

		if (!targetCell.IsPartOfCoastline) {
		
			_isTraversingSea = true;
			_traverseDirection = direction;

			_currentDirectionOffset = FirstCell.GetNextLocalRandomFloat(RngOffsets.ROUTE_CHOOSE_NEXT_COASTAL_CELL_2 + rngOffset);
		}

		_currentEndRoutePreference += 0.1f;

//		#if DEBUG
//		if (Manager.RegisterDebugEvent != null) {
//			if ((FirstCell.Longitude == Manager.TracingData.Longitude) && (FirstCell.Latitude == Manager.TracingData.Latitude)) {
//
//				string cellPos = "Position: Long:" + FirstCell.Longitude + "|Lat:" + FirstCell.Latitude;
//				string currentCellDesc = "Position: Long:" + currentCell.Longitude + "|Lat:" + currentCell.Latitude;
//
//				string targetCellDesc = "Null";
//
//				if (targetCell != null) {
//					targetCellDesc = "Position: Long:" + targetCell.Longitude + "|Lat:" + targetCell.Latitude;
//				}
//
//				SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage(
//					"ChooseNextCoastalCell - FirstCell:" + cellPos,
//					"CurrentDate: " + World.CurrentDate + 
//					", currentCell: " + currentCellDesc + 
//					", targetCell: " + targetCellDesc + 
//					", _currentEndRoutePreference: " + _currentEndRoutePreference + 
////					", nCells: " + cellWeightsStr + 
//					"");
//
//				Manager.RegisterDebugEvent ("DebugMessage", debugMessage);
//			}
//		}
//		#endif

		return targetCell;
	}

	public bool ContainsCell (TerrainCell cell) {
	
		return Cells.Contains (cell);
	}

	public void Synchronize () {

	}

	public void FinalizeLoad () {

		if (!Consolidated) {
			Debug.LogError ("Can't finalize unconsolidated route");
			return;
		}

		TerrainCell currentCell = null;

		bool first = true;

		if (CellPositions.Count == 0) {
		
			Debug.LogError ("CellPositions is empty");
		}
	
		foreach (WorldPosition p in CellPositions) {

			currentCell = World.GetCell (p);

			if (currentCell == null) {
				Debug.LogError ("Unable to find terrain cell at [" + currentCell.Longitude + "," + currentCell.Latitude + "]");
			}

			if (first) {

				FirstCell = currentCell;
				first = false;
			}

			Cells.Add (currentCell);
		}

		foreach (TerrainCell cell in Cells) {
			cell.AddCrossingRoute (this);
		}

		LastCell = currentCell;
	}

	//	private void LatitudeDirectionModifier (TerrainCell currentCell, out Direction newDirection, out float newOffset) {
	//
	//		float modOffset;
	//
	//		switch (_traverseDirection) {
	//		case Direction.West:
	//			modOffset = _currentDirectionOffset / 2f;
	//			break;
	//		case Direction.Northwest:
	//			modOffset = 0.5f + _currentDirectionOffset / 2f;
	//			break;
	//		case Direction.North:
	//			modOffset = 0.5f + (1f - _currentDirectionOffset) / 2f;
	//			break;
	//		case Direction.Northeast:
	//			modOffset = (1f - _currentDirectionOffset) / 2f;
	//			break;
	//		case Direction.East:
	//			modOffset = _currentDirectionOffset / 2f;
	//			break;
	//		case Direction.Southeast:
	//			modOffset = 0.5f + _currentDirectionOffset / 2f;
	//			break;
	//		case Direction.South:
	//			modOffset = 0.5f + (1f - _currentDirectionOffset) / 2f;
	//			break;
	//		case Direction.Southwest:
	//			modOffset = (1f - _currentDirectionOffset) / 2f;
	//			break;
	//		default:
	//			throw new System.Exception ("Unhandled direction: " + _traverseDirection);
	//		}
	//
	//		float latFactor = Mathf.Sin(Mathf.PI * currentCell.Latitude / (float)World.Height);
	//
	//		modOffset *= latFactor;
	//
	//		switch (_traverseDirection) {
	//
	//		case Direction.West:
	//			newOffset = modOffset * 2f;
	//			newDirection = Direction.West;
	//			break;
	//
	//		case Direction.Northwest:
	//			newOffset = modOffset * 2f;
	//
	//			if (newOffset > 1) {
	//				newOffset -= 1f;
	//				newDirection = Direction.Northwest;
	//			} else {
	//				newDirection = Direction.West;
	//			}
	//
	//			break;
	//
	//		case Direction.North:
	//			newOffset = (1f - modOffset) * 2f;
	//
	//			if (newOffset > 1) {
	//				newOffset -= 1f;
	//				newDirection = Direction.Northeast;
	//			} else {
	//				newDirection = Direction.North;
	//			}
	//
	//			break;
	//
	//		case Direction.Northeast:
	//			newOffset = (1f - modOffset) * 2f;
	//			newDirection = Direction.Northeast;
	//			break;
	//
	//		case Direction.East:
	//			newOffset = modOffset * 2f;
	//			newDirection = Direction.East;
	//			break;
	//
	//		case Direction.Southeast:
	//			newOffset = modOffset * 2f;
	//
	//			if (newOffset > 1) {
	//				newOffset -= 1f;
	//				newDirection = Direction.Southeast;
	//			} else {
	//				newDirection = Direction.East;
	//			}
	//
	//			break;
	//
	//		case Direction.South:
	//			newOffset = (1f - modOffset) * 2f;
	//
	//			if (newOffset > 1) {
	//				newOffset -= 1f;
	//				newDirection = Direction.Southwest;
	//			} else {
	//				newDirection = Direction.South;
	//			}
	//
	//			break;
	//
	//		case Direction.Southwest:
	//			newOffset = (1f - modOffset) * 2f;
	//			newDirection = Direction.Southwest;
	//			break;
	//
	//		default:
	//			throw new System.Exception ("Unhandled direction: " + _traverseDirection);
	//		}
	//	}
}
