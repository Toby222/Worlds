using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class CulturalDiscoveryInfo {
	
	[XmlAttribute]
	public string Id;
	
	[XmlAttribute]
	public string Name;

	public CulturalDiscoveryInfo () {
	}
	
	public CulturalDiscoveryInfo (string id, string name) {
		
		Id = id;
		
		Name = name;
	}
	
	public CulturalDiscoveryInfo (CulturalDiscoveryInfo baseDiscovery) {
		
		Id = baseDiscovery.Id;
		
		Name = baseDiscovery.Name;
	}
}

public abstract class CulturalDiscovery : CulturalDiscoveryInfo {
	
	public CulturalDiscovery (string id, string name) : base (id, name) {

	}
	
	public CulturalDiscovery GenerateCopy () {
		
		System.Type discoveryType = this.GetType ();
		
		System.Reflection.ConstructorInfo cInfo = discoveryType.GetConstructor (new System.Type[] {});
		
		return cInfo.Invoke (new object[] {}) as CulturalDiscovery;
	}

	public abstract bool CanBeHeld (CellGroup group);

	public virtual void LossConsequences (CellGroup group) {
		
	}
}

public class BoatMakingDiscovery : CulturalDiscovery {

	public const string BoatMakingDiscoveryId = "BoatMakingDiscovery";
	public const string BoatMakingDiscoveryName = "Boat Making";
	
	public BoatMakingDiscovery () : base (BoatMakingDiscoveryId, BoatMakingDiscoveryName) {

	}

	public override bool CanBeHeld (CellGroup group)
	{
		CulturalKnowledge knowledge = group.Culture.GetKnowledge (ShipbuildingKnowledge.ShipbuildingKnowledgeId);

		if (knowledge == null)
			return false;

		return true;
	}
}

public class SailingDiscovery : CulturalDiscovery {
	
	public const string SailingDiscoveryId = "SailingDiscovery";
	public const string SailingDiscoveryName = "Sailing";
	
	public SailingDiscovery () : base (SailingDiscoveryId, SailingDiscoveryName) {
		
	}
	
	public override bool CanBeHeld (CellGroup group)
	{
		CulturalKnowledge knowledge = group.Culture.GetKnowledge (ShipbuildingKnowledge.ShipbuildingKnowledgeId);
		
		if (knowledge == null)
			return false;
		
		if (knowledge.Value < ShipbuildingKnowledge.MinKnowledgeValueForSailing)
			return false;
		
		return true;
	}
}

public class TribalismDiscovery : CulturalDiscovery {

	public const string TribalismDiscoveryId = "TribalismDiscovery";
	public const string TribalismDiscoveryName = "Tribalism";

	public TribalismDiscovery () : base (TribalismDiscoveryId, TribalismDiscoveryName) {

	}

	public override bool CanBeHeld (CellGroup group)
	{
		CulturalKnowledge knowledge = group.Culture.GetKnowledge (SocialOrganizationKnowledge.SocialOrganizationKnowledgeId);

		if (knowledge == null)
			return false;

		if (knowledge.Value < SocialOrganizationKnowledge.MinKnowledgeValueForTribalism)
			return false;

		return true;
	}

	public override void LossConsequences (CellGroup group)
	{
		group.LostTribalism ();
		
		base.LossConsequences (group);
	}
}

public class PlantCultivationDiscovery : CulturalDiscovery {

	public const string PlantCultivationDiscoveryId = "PlantCultivationDiscovery";
	public const string PlantCultivationDiscoveryName = "Plant Cultivation";

	public PlantCultivationDiscovery () : base (PlantCultivationDiscoveryId, PlantCultivationDiscoveryName) {

	}

	public override bool CanBeHeld (CellGroup group)
	{
		CulturalKnowledge knowledge = group.Culture.GetKnowledge (AgricultureKnowledge.AgricultureKnowledgeId);

		if (knowledge == null)
			return false;

		return true;
	}
}
