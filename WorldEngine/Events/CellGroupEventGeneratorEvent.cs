using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public abstract class CellGroupEventGeneratorEvent : CellGroupEvent
{
    [XmlAttribute("GnId")]
    public string GeneratorId;

    [XmlIgnore]
    public ICellGroupEventGenerator Generator;

    [XmlIgnore]
    public string EventSetFlag;

    public CellGroupEventGeneratorEvent()
    {
    }

    public CellGroupEventGeneratorEvent(
        ICellGroupEventGenerator generator, 
        CellGroup group, 
        long triggerDate, 
        long eventTypeId) : 
        base(group, triggerDate, eventTypeId)
    {
        Generator = generator;
        GeneratorId = generator.GetEventGeneratorId();
        EventSetFlag = generator.EventSetFlag;

        group.SetFlag(EventSetFlag);
    }

    public override void FinalizeLoad()
    {
        base.FinalizeLoad();

        Generator = World.GetEventGenerator(GeneratorId) as ICellGroupEventGenerator;

        if (Generator == null)
        {
            throw new System.Exception("CellGroupEventGeneratorEvent: Generator with Id:" + GeneratorId + " not found");
        }
    }

    public override void Cleanup()
    {
        if (Group != null)
        {
            Group.UnsetFlag(EventSetFlag);
        }

        base.Cleanup();
    }
}
