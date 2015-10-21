﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class CellGroup : HumanGroup {

	public const int GenerationSpan = 20;

	public const float NaturalDeathRate = 0.03f; // more or less 0.5/half-life (22.87 years for paleolitic life expectancy of 33 years)
	public const float NaturalBirthRate = 0.105f; // Should cancel out death rate in perfect circumstances (hunter-gathererers in grasslands)
	public const float MinChangeRate = -1.0f; // Should cancel out death rate in perfect circumstances (hunter-gathererers in grasslands)

	public const float NaturalGrowthRate = NaturalBirthRate - NaturalDeathRate;
	
	public const float PopulationConstant = 10;

	public const float TravelTimeFactor = 1;
	
	[XmlAttribute]
	public int Population;
	
	[XmlAttribute]
	public int Id;
	
	[XmlAttribute]
	public bool StillPresent = true;
	
	[XmlAttribute]
	public int LastUpdateDate;
	
	[XmlAttribute]
	public int NextUpdateDate;
	
	[XmlAttribute]
	public int OptimalPopulation;

	public Culture Culture;
	
	[XmlIgnore]
	public TerrainCell Cell;

	public CellGroup () {
	}
	
	public CellGroup (MigratingGroup migratingGroup, int splitPopulation, Culture splitCulture) : this(migratingGroup.World, migratingGroup.TargetCell, splitPopulation, splitCulture) {
	}

	public CellGroup (World world, TerrainCell cell, int initialPopulation, Culture baseCulture) : base(world) {

		Population = initialPopulation;
		
		LastUpdateDate = World.CurrentDate;

		Cell = cell;

		Cell.Groups.Add (this);

		Id = World.GenerateCellGroupId();

		Culture = new Culture (baseCulture);

		InitializeBiomeSurvivalSkills ();
		
		NextUpdateDate = CalculateNextUpdateDate();
		
		World.InsertEventToHappen (new UpdateCellGroupEvent (World, NextUpdateDate, this));
		
		World.UpdateMostPopulousGroup (this);
		
		OptimalPopulation = CalculateOptimalPopulation (Cell);
	}

	public void InitializeBiomeSurvivalSkills () {

		foreach (string biome in Cell.PresentBiomeNames) {
		
			string skillId = BiomeSurvivalSkill.GenerateId (biome);

			if (Culture.GetSkill (skillId) == null) {
				
				Culture.Skills.Add (new BiomeSurvivalSkill (biome, 0.0f));
			}
		}
	}

	public void MergeGroup (MigratingGroup group, int splitPopulation, Culture splitCulture) {
		
		UpdateInternal ();

		int newPopulation = Population + splitPopulation;
		
		if (newPopulation <= 0) {
			throw new System.Exception ("Population after migration merge shouldn't be 0 or less.");
		}

		float percentage = splitPopulation / (float)newPopulation;
	
		Population = newPopulation;

		Culture.MergeCulture (splitCulture, percentage);
	}
	
	public int SplitGroup (MigratingGroup group) {
		
		UpdateInternal ();

		int splitPopulation = (int)Mathf.Floor(Population * group.PercentPopulation);
		
		Population -= splitPopulation;

		return splitPopulation;
	}

	public void Update () {

		UpdateInternal ();
	}

	public void SetupForNextUpdate () {
		
		if (Population <= 0) {
			World.AddGroupToRemove (this);
			return;
		}
		
		World.UpdateMostPopulousGroup (this);
		
		OptimalPopulation = CalculateOptimalPopulation (Cell);
		
		ConsiderMigration();
		
		//		if (IsTagged) {
		//		
		//			bool debug = true;
		//		}
		
		NextUpdateDate = CalculateNextUpdateDate();
		
		World.InsertEventToHappen (new UpdateCellGroupEvent (World, NextUpdateDate, this));
	}

	public void ConsiderMigration () {

		float percentToMigrate = 0.25f * Cell.GetNextLocalRandomFloat ();

		float score = Cell.GetNextLocalRandomFloat ();

		List<TerrainCell> possibleTargetCells = new List<TerrainCell> (Cell.Neighbors);
		possibleTargetCells.Add (Cell);

		float noMigrationPreference = 3f;

		TerrainCell targetCell = MathUtility.WeightedSelection (score, possibleTargetCells, (c) => {

			float areaFactor = Cell.Area / TerrainCell.MaxArea;
			float altitudeFactor = 1 - (c.Altitude / World.MaxPossibleAltitude);

			float stressFactor = 1 - c.CalculatePopulationStress();
			stressFactor *= stressFactor;
			stressFactor *= stressFactor;

			float cSurvivability = 0;
			float cForagingCapacity = 0;

			CalculateAdaptionToCell (c, out cForagingCapacity, out cSurvivability);

			float adaptionFactor = cSurvivability * cForagingCapacity; 

			float cellValue = adaptionFactor * altitudeFactor * areaFactor * stressFactor;

			if (c == Cell) {
				cellValue *= noMigrationPreference;
				cellValue += noMigrationPreference;
			}

			return cellValue;
		});

		if (targetCell == Cell)
			return;

		if (targetCell == null)
			return;
		
		float cellSurvivability = 0;
		float cellForagingCapacity = 0;
		
		CalculateAdaptionToCell (targetCell, out cellForagingCapacity, out cellSurvivability);

		float travelFactor = cellSurvivability * cellSurvivability * cellSurvivability;

		if (cellSurvivability <= 0)
			return;

		int travelTime = (int)Mathf.Ceil(TravelTimeFactor / travelFactor);
		
		int nextDate = World.CurrentDate + travelTime;

		MigratingGroup migratingGroup = new MigratingGroup (World, percentToMigrate, this, targetCell);
		
		World.InsertEventToHappen (new MigrateGroupEvent (World, nextDate, travelTime, migratingGroup));
	}

	public void Destroy () {

		Cell.Groups.Remove (this);
		World.RemoveGroup (this);

		StillPresent = false;
	}

	private void UpdateInternal () {
		
		int timeSpan = World.CurrentDate - LastUpdateDate;

		if (timeSpan <= 0)
			return;

		UpdatePopulation (timeSpan);
		UpdateCulture (timeSpan);
		
		LastUpdateDate = World.CurrentDate;
		
		World.AddUpdatedGroup (this);
	}
	
	private void UpdatePopulation (int timeSpan) {
		
		Population = PopulationAfterTime (timeSpan);
	}
	
	private void UpdateCulture (int timeSpan) {
		
		Culture.Update (this, timeSpan);
	}

	public int CalculateOptimalPopulation (TerrainCell cell) {

		int optimalPopulation = 0;

		float modifiedForagingCapacity = 0;
		float modifiedSurvivability = 0;

		CalculateAdaptionToCell (cell, out modifiedForagingCapacity, out modifiedSurvivability);

		float populationCapacityFactor = PopulationConstant * cell.Area * modifiedForagingCapacity * modifiedSurvivability;

		optimalPopulation = (int)Mathf.Floor (populationCapacityFactor);

		return optimalPopulation;
	}

	public void CalculateAdaptionToCell (TerrainCell cell, out float foragingCapacity, out float survivability) {

		float modifiedForagingCapacity = 0;
		float modifiedSurvivability = 0;
		
		foreach (CulturalSkill skill in Culture.Skills) {
			
			float skillValue = skill.Value;
			
			if (skill is BiomeSurvivalSkill) {
				
				BiomeSurvivalSkill biomeSurvivalSkill = skill as BiomeSurvivalSkill;
				
				string biomeName = biomeSurvivalSkill.Biome;
				
				float biomePresence = cell.GetBiomePresence(biomeName);
				
				if (biomePresence > 0)
				{
					Biome biome = Biome.Biomes[biomeName];
					
					modifiedForagingCapacity += biome.ForagingCapacity * skillValue * biomePresence;
					modifiedSurvivability += (biome.Survivability + skillValue * (1 - biome.Survivability)) * biomePresence;
				}
			}
		}

		foragingCapacity = modifiedForagingCapacity;
		survivability = modifiedSurvivability;
	}

	public int CalculateNextUpdateDate () {

		float skillLevelFactor = (1 + 99 * Culture.SkillAdaptationLevel (this)) / 100f;

		float populationFactor = 1 + Mathf.Abs (OptimalPopulation - Population);

		float mixFactor = skillLevelFactor * (2000 + OptimalPopulation) / populationFactor;

		int finalFactor = (int)Mathf.Max(mixFactor, 1);

		return World.CurrentDate + GenerationSpan * finalFactor;
	}

	public int PopulationAfterTime (int time) { // in years

//		if (Cell.IsObserved) {
//		
//			bool debug = true;
//		}

		int population = Population;
		
		if (population == OptimalPopulation)
			return population;
		
		float timeFactor = NaturalGrowthRate * time / (float)GenerationSpan;

		if (population < OptimalPopulation) {
			
			float geometricTimeFactor = Mathf.Pow(2, timeFactor);
			float populationFactor = 1 - Population/(float)OptimalPopulation;

			population = (int)Mathf.Floor(OptimalPopulation * (1 - Mathf.Pow(populationFactor, geometricTimeFactor)));

			return population;
		}

		if (population > OptimalPopulation) {

			population = (int)Mathf.Floor(OptimalPopulation + (Population - OptimalPopulation) * Mathf.Exp (-timeFactor));
			
			return population;
		}

		return 0;
	}
}
