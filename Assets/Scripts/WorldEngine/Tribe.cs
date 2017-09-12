﻿
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class Tribe : Polity {

	public const int MinPopulationForTribeCore = 500;

	public const float TribalExpansionFactor = 2f;

	public const int TribeLeaderAvgTimeSpan = 41;

	public const string TribeType = "Tribe";

	public static string[] TribeNounVariations = new string[] { "tribe", "people", "folk", "community", "[ipn(man)]men", "[ipn(woman)]women", "[ipn(child)]children" };

	public const float BaseCoreInfluence = 0.5f;

	[XmlAttribute("SpltDate")]
	public int TribeSplitEventDate;

	[XmlIgnore]
	public TribeSplitEvent TribeSplitEvent;

	public Tribe () {

	}

	public Tribe (CellGroup coreGroup) : base (TribeType, coreGroup) {

		//// Make sure there's a region to spawn into

		TerrainCell coreCell = coreGroup.Cell;

		Region cellRegion = coreGroup.Cell.Region;

		if (cellRegion == null) {

			cellRegion = Region.TryGenerateRegion (coreCell);

			if (cellRegion != null) {
				cellRegion.GenerateName (this, coreCell);

				World.AddRegion (cellRegion);
			}
		}

		////

		float randomValue = coreGroup.Cell.GetNextLocalRandomFloat (RngOffsets.TRIBE_GENERATE_NEW_TRIBE);
		float coreInfluence = BaseCoreInfluence + randomValue * (1 - BaseCoreInfluence);

		coreGroup.SetPolityInfluence (this, coreInfluence, 0);

		World.AddGroupToUpdate (coreGroup);

		GenerateName ();

//		Debug.Log ("New tribe '" + Name + "' spawned at " + coreGroup.Cell.Position);

		//// Add starting clan

		Clan clan = new Clan (this, coreGroup, 1);

		AddFaction (clan);

		SetDominantFaction (clan);

		//// Add base events

		AddBaseEvents ();
	}

	public Tribe (Clan triggerClan, Polity parentPolity) : base (TribeType, triggerClan.CoreGroup, parentPolity) {

		triggerClan.ChangePolity (this, triggerClan.Prominence);

		SwitchCellInfluences (parentPolity, triggerClan);

		GenerateName ();

		//// Add base events

		AddBaseEvents ();

		////

//		Debug.Log ("New tribe '" + Name + "' from tribe '" + parentPolity.Name + "' with total transfered prominence = " + transferedProminence);
	}

	private void AddBaseEvents () {

		if (TribeSplitEvent.CanBeAssignedTo (this)) {

			TribeSplitEventDate = TribeSplitEvent.CalculateTriggerDate (this);

			TribeSplitEvent = new TribeSplitEvent (this, TribeSplitEventDate);

			World.InsertEventToHappen (TribeSplitEvent);
		}
	}

	private void SwitchCellInfluences (Polity sourcePolity, Clan triggerClan) {

//		float targetPolityProminence = triggerClan.Prominence;
//		float sourcePolityProminence = 1 - targetPolityProminence;
//
//		if (targetPolityProminence <= 0) {
//		
//			throw new System.Exception ("Pulling clan prominence equal or less than zero.");
//		}

		int maxGroupCount = sourcePolity.InfluencedGroups.Count;

		Dictionary<CellGroup, float> groupDistances = new Dictionary<CellGroup, float> (maxGroupCount);

		Queue<CellGroup> sourceGroups = new Queue<CellGroup> (maxGroupCount);

		sourceGroups.Enqueue (CoreGroup);

		int reviewedCells = 0;
		int switchedCells = 0;

		HashSet<Faction> factionsToTransfer = new HashSet<Faction> ();

		while (sourceGroups.Count > 0) {
		
			CellGroup group = sourceGroups.Dequeue ();

			if (groupDistances.ContainsKey (group))
				continue;

			PolityInfluence pi = group.GetPolityInfluence (sourcePolity);

			if (pi == null)
				continue;

			reviewedCells++;

			float targetDistanceToFactionCore = CalculateShortestCoreDistance (group, groupDistances);

			if (targetDistanceToFactionCore >= CellGroup.MaxCoreDistance)
				continue;

			groupDistances.Add (group, targetDistanceToFactionCore);

			float sourceDistanceToFactionCore = pi.CoreDistance;

			float percentInfluence = 1f;

			if (sourceDistanceToFactionCore < CellGroup.MaxCoreDistance) {

				float distanceDelta = targetDistanceToFactionCore - sourceDistanceToFactionCore;

				if (distanceDelta > 0) {
				
					percentInfluence = 0;
				}
//
//				float ditanceToCoresSum = targetDistanceToFactionCore + sourceDistanceToFactionCore;
//
//				if (ditanceToCoresSum <= 0) {
//
//					throw new System.Exception ("Sum of core distances equal or less than zero.");
//				}
//			
//				float distanceFactor = sourceDistanceToFactionCore / ditanceToCoresSum;
//
//				distanceFactor = Mathf.Clamp01((distanceFactor * 3f) - 1f);
//
//				float targetDistanceFactor = distanceFactor;
//				float sourceDistanceFactor = 1 - distanceFactor;
//
//				float targetPolityWeight = targetPolityProminence * targetDistanceFactor;
//				float sourcePolityWeight = sourcePolityProminence * sourceDistanceFactor;
//
//				percentInfluence = targetPolityWeight / (targetPolityWeight + sourcePolityWeight);
			}

			if (percentInfluence > 0.5f) {
			
				switchedCells++;

				foreach (Faction faction in group.GetFactionCores ()) {
				
					factionsToTransfer.Add (faction);
				}
			}

			if (percentInfluence <= 0)
				continue;

			float influenceValue = group.GetPolityInfluenceValue (sourcePolity);
	
			group.SetPolityInfluence (sourcePolity, influenceValue * (1 - percentInfluence));

			group.SetPolityInfluence (this, influenceValue * percentInfluence, targetDistanceToFactionCore);
	
			World.AddGroupToUpdate (group);

			foreach (CellGroup neighborGroup in group.Neighbors.Values) {

				if (groupDistances.ContainsKey (neighborGroup))
					continue;
			
				sourceGroups.Enqueue (neighborGroup);
			}
		}

		float highestProminence = triggerClan.Prominence;

		Clan dominantClan = triggerClan;

		foreach (Faction faction in factionsToTransfer) {

			Clan clan = faction as Clan;

			if (clan != null) {
				if (clan.Prominence > highestProminence) {
					highestProminence = clan.Prominence;
					dominantClan = clan;
				}
			}

			faction.ChangePolity (this, faction.Prominence);
		}

		SetDominantFaction (dominantClan);

//		Debug.Log ("SwitchCellInfluences: source polity cells: " + maxGroupCount + ", reviewed cells: " + reviewedCells + ", switched cells: " + switchedCells);
	}

	private float CalculateShortestCoreDistance (CellGroup group, Dictionary<CellGroup, float> groupDistances) {

		if (groupDistances.Count <= 0)
			return 0;

		float shortestDistance = CellGroup.MaxCoreDistance;

		foreach (KeyValuePair<Direction, CellGroup> pair in group.Neighbors) {

			float distanceToCoreFromNeighbor = float.MaxValue;

			if (!groupDistances.TryGetValue (pair.Value, out distanceToCoreFromNeighbor)) {
			
				continue;
			}

			if (distanceToCoreFromNeighbor >= float.MaxValue)
				continue;

			float neighborDistance = group.Cell.NeighborDistances[pair.Key];

			float totalDistance = distanceToCoreFromNeighbor + neighborDistance;

			if (totalDistance < 0)
				continue;

			if (totalDistance < shortestDistance)
				shortestDistance = totalDistance;
		}

		return shortestDistance;
	}

	protected override void UpdateInternal ()
	{
//		TryRelocateCore ();
	}

	protected override void GenerateName ()
	{
		Region coreRegion = CoreGroup.Cell.Region;

		int rngOffset = RngOffsets.TRIBE_GENERATE_NAME + (int)Id;

		int randomInt = GetNextLocalRandomInt (rngOffset++, TribeNounVariations.Length);

		string tribeNounVariation = TribeNounVariations[randomInt];

		string regionAttributeNounVariation = coreRegion.GetRandomAttributeVariation ((int maxValue) => GetNextLocalRandomInt (rngOffset++, maxValue));

		if (regionAttributeNounVariation != string.Empty) {
			regionAttributeNounVariation = " [nad]" + regionAttributeNounVariation;
		}

		string untranslatedName = "the" + regionAttributeNounVariation + " " + tribeNounVariation;

		Language.NounPhrase namePhrase = Culture.Language.TranslateNounPhrase (untranslatedName, () => GetNextLocalRandomFloat (rngOffset++));

		Name = new Name (namePhrase, untranslatedName, Culture.Language, World);

//		#if DEBUG
//		Debug.Log ("Tribe #" + Id + " name: " + Name);
//		#endif
	}

	public override float CalculateGroupInfluenceExpansionValue (CellGroup sourceGroup, CellGroup targetGroup, float sourceValue)
	{
		if (sourceValue <= 0)
			return 0;

		float sourceGroupTotalPolityInfluenceValue = sourceGroup.TotalPolityInfluenceValue;
		float targetGroupTotalPolityInfluenceValue = targetGroup.TotalPolityInfluenceValue;

		if (sourceGroupTotalPolityInfluenceValue <= 0) {

			throw new System.Exception ("sourceGroup.TotalPolityInfluenceValue equal or less than 0: " + sourceGroupTotalPolityInfluenceValue);
		}

		float influenceFactor = sourceGroupTotalPolityInfluenceValue / (targetGroupTotalPolityInfluenceValue + sourceGroupTotalPolityInfluenceValue);
		influenceFactor = Mathf.Pow (influenceFactor, 4);

		float modifiedForagingCapacity = 0;
		float modifiedSurvivability = 0;

		CalculateAdaptionToCell (targetGroup.Cell, out modifiedForagingCapacity, out modifiedSurvivability);

		float survivabilityFactor = Mathf.Pow (modifiedSurvivability, 2);

		float finalFactor = influenceFactor * survivabilityFactor;

		if (sourceGroup != targetGroup) {

			// There should be a strong bias against polity expansion to reduce activity
			finalFactor *= TribalExpansionFactor;
		}

		return finalFactor;
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		if (TribeSplitEvent.CanBeAssignedTo (this)) {

			TribeSplitEvent = new TribeSplitEvent (this, TribeSplitEventDate);

			World.InsertEventToHappen (TribeSplitEvent);
		}
	}
}

public class TribeSplitEventMessage : PolityEventMessage {

	[XmlAttribute]
	public long NewTribeId;

	public TribeSplitEventMessage () {
		
	}

	public TribeSplitEventMessage (Polity polity, Tribe newTribe, long date) : base (polity, WorldEvent.TribeSplitEventId, date) {

		NewTribeId = newTribe.Id;
	}

	protected override string GenerateMessage ()
	{
		Polity newTribe = World.GetPolity (NewTribeId);

		return "A new tribe, " + newTribe.Name.Text + ", has split from tribe " +  Polity.Name.Text;
	}
}

public class TribeSplitEvent : PolityEvent {

	public const int DateSpanFactorConstant = CellGroup.GenerationTime * 2000;

	public const int MuAdministrativeLoadValue = 500000;

//	public const string EventSetFlag = "TribeSplitEvent_Set";

	public const float MinCoreInfluenceValue = 0.3f;

	public const float MinCoreDistance = 1000f;

	public const float MinTargetProminence = 0.40f;
	public const float MaxTargetProminence = 0.60f;

//	private CellGroup _newCoreGroup = null;
	private Clan _triggerClan = null;

	public TribeSplitEvent () {

		DoNotSerialize = true;
	}

	public TribeSplitEvent (Tribe tribe, int triggerDate) : base (tribe, triggerDate, TribeSplitEventId) {

//		tribe.SetFlag (EventSetFlag);

		DoNotSerialize = true;
	}

	private static float CalculateTribeAdministrativeLoadFactor (Tribe tribe) {

		float socialOrganizationValue = 0;

		CulturalKnowledge socialOrganizationKnowledge = tribe.Culture.GetKnowledge (SocialOrganizationKnowledge.SocialOrganizationKnowledgeId);

		if (socialOrganizationKnowledge != null)
			socialOrganizationValue = socialOrganizationKnowledge.Value;

		if (socialOrganizationValue <= 0) {

			return float.MaxValue;
		}

		float administrativeLoad = tribe.TotalAdministrativeCost;

		return administrativeLoad / socialOrganizationValue;
	}

	public static int CalculateTriggerDate (Tribe tribe) {

		float randomFactor = tribe.GetNextLocalRandomFloat (RngOffsets.TRIBE_SPLITTING_EVENT_CALCULATE_TRIGGER_DATE);
		randomFactor = Mathf.Pow (randomFactor, 2);

		float dateSpan = (1 - randomFactor) * DateSpanFactorConstant;

		int targetDate = (int)(tribe.World.CurrentDate + dateSpan);

		if (targetDate <= tribe.World.CurrentDate)
			targetDate = int.MinValue;

		return targetDate;
	}

	public static bool CanBeAssignedTo (Tribe tribe) {

//		if (tribe.IsFlagSet (EventSetFlag))
//			return false;

		return true;
	}

	public override bool CanTrigger () {

		#if DEBUG
		if (Polity.Territory.IsSelected) {
			bool debug = true;
		}
		#endif

		if (!base.CanTrigger ())
			return false;

		if (new List<Clan> (Polity.GetFactions<Clan> ()).Count <= 1)
			return false;

		float administrativeLoadFactor = CalculateTribeAdministrativeLoadFactor (Polity as Tribe);
		administrativeLoadFactor = Mathf.Pow (administrativeLoadFactor, 2);

		if (administrativeLoadFactor < 0)
			return true;

		// Add clan selection mechanism

		float splitValue = administrativeLoadFactor / (administrativeLoadFactor + MuAdministrativeLoadValue);

		int rngOffset = RngOffsets.EVENT_CAN_TRIGGER + (int)Id;

		float triggerValue = Polity.GetNextLocalRandomFloat (rngOffset++);

		if (triggerValue > splitValue)
			return false;

//		_newCoreGroup = Polity.GetRandomGroup (rngOffset++, GetPolityGroupWeight, true);
//
//		if (_newCoreGroup == null)
//			return false;

		_triggerClan = Polity.GetRandomFaction (rngOffset++, GetFactionWeight, true) as Clan;

		if (_triggerClan == null)
			return false;

		return true;
	}

//	public float GetPolityGroupWeight (CellGroup group) {
//
//		if (group == Polity.CoreGroup)
//			return 0;
//
//		PolityInfluence pi = group.GetPolityInfluence (Polity);
//
//		if (group.HighestPolityInfluence != pi)
//			return 0;
//
//		float value = Mathf.Max(pi.Value - MinCoreInfluenceValue, 0);
//
//		float coreDistance = Mathf.Max(pi.CoreDistance - MinCoreDistance, 0);
//
//		float weight = coreDistance * value;
//
//		if (weight < 0)
//			return float.MaxValue;
//
//		return weight;
//	}

	public float GetFactionWeight (Faction faction) {

		if (!(faction is Clan))
			return 0;

		if (faction.IsDominant)
			return 0;

		CellGroup factionCoreGroup = faction.CoreGroup;

		if (factionCoreGroup == Polity.CoreGroup)
			return 0;

		PolityInfluence pi = factionCoreGroup.GetPolityInfluence (Polity);

		if (factionCoreGroup.HighestPolityInfluence != pi)
			return 0;

		float prominence = faction.Prominence;

		float coreDistance = Mathf.Max(pi.CoreDistance - MinCoreDistance, 0);

		float weight = prominence * coreDistance;

		if (weight < 0)
			return float.MaxValue;

		return weight;
	}

	public override void Trigger () {

//		int rngOffset = RngOffsets.EVENT_TRIGGER + (int)Id;

//		float targetProminence = MinTargetProminence + ((MaxTargetProminence - MinTargetProminence) * Polity.GetNextLocalRandomFloat (rngOffset++));
//
//		float accProminence = 0;
//
//		List<Clan> factions = new List<Clan> (Polity.GetFactions<Clan>());
//
//		List<Clan> clansToTransfer = new List<Clan> (factions.Count);
//
//		factions.Sort ((a, b) => {
//			float randVal = Polity.GetNextLocalRandomFloat (rngOffset++);
//
//			if (randVal > 0.5f) return 1;
//			if (randVal < 0.5f) return -1;
//			return 0;
//		});
//
//		foreach (Clan clan in factions) {
//
//			if (clan.IsDominant)
//				continue;
//
//			clansToTransfer.Add (clan);
//
//			accProminence += clan.Prominence;
//
//			if (accProminence > targetProminence)
//				break;
//		}

//		Tribe newTribe = new Tribe (_newCoreGroup, Polity, clansToTransfer);
		Tribe newTribe = new Tribe (_triggerClan, Polity);

		World.AddPolity (newTribe);
		World.AddPolityToUpdate (newTribe);
		World.AddPolityToUpdate (Polity);

		Polity.AddEventMessage (new TribeSplitEventMessage (Polity, newTribe, TriggerDate));
	}

	protected override void DestroyInternal () {

		base.DestroyInternal ();

//		if (Polity != null) {
//			Polity.UnsetFlag (EventSetFlag);
//		}

		if ((Polity != null) && (Polity.StillPresent)) {

			Tribe tribe = Polity as Tribe;

			if (CanBeAssignedTo (tribe)) {

				tribe.TribeSplitEvent = this;

				tribe.TribeSplitEventDate = CalculateTriggerDate (tribe);

				Reset (tribe.TribeSplitEventDate);

				World.InsertEventToHappen (this);
			}
		}
	}

	public override void FinalizeLoad () {

		base.FinalizeLoad ();

		Tribe tribe = Polity as Tribe;

		tribe.TribeSplitEvent = this;
	}
}
