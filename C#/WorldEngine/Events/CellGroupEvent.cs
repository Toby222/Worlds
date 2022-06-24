﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public abstract class CellGroupEvent : WorldEvent
{
    public Identifier GroupId;

    [XmlIgnore]
    public CellGroup Group;

    public CellGroupEvent()
    {

    }

    public CellGroupEvent(
        CellGroup group,
        long triggerDate,
        long eventTypeId,
        long? id = null,
        long originalSpawnDate = -1) :
        base(
        group.World, 
        triggerDate, 
        (id == null) ? GenerateUniqueIdentifier(group, triggerDate, eventTypeId) : id.Value, 
        eventTypeId, 
        originalSpawnDate)
    {
        Group = group;
        GroupId = Group.Id;

#if DEBUG
        GenerateDebugMessage(false);
#endif
    }

    public override WorldEventSnapshot GetSnapshot()
    {
        return new CellGroupEventSnapshot(this);
    }

    public static long GenerateUniqueIdentifier(CellGroup group, long triggerDate, long eventTypeId)
    {
        if (triggerDate > World.MaxSupportedDate)
        {
            Debug.LogWarning("CellGroupEvent.GenerateUniqueIdentifier - 'triggerDate' is greater than " + World.MaxSupportedDate + " (triggerDate = " + triggerDate + ")");
        }

        long id = (triggerDate * 1000000000L) + (group.Longitude * 1000000L) + (group.Latitude * 1000L) + eventTypeId;

        return id;
    }

#if DEBUG
    protected void GenerateDebugMessage(bool isReset)
    {
        if ((Manager.RegisterDebugEvent != null) && (Manager.TracingData.Priority <= 0))
        {
            //			if (Group.Id == Manager.TracingData.GroupId) {
            string groupId = "Id: " + Group + " Pos: " + Group.Position;

            SaveLoadTest.DebugMessage debugMessage = new SaveLoadTest.DebugMessage("CellGroupEvent - Group:" + groupId + ", Type: " + this.GetType(),
                "SpawnDate: " + SpawnDate +
                ", TriggerDate: " + TriggerDate +
                //				", isReset: " + isReset + 
                "", SpawnDate);

            Manager.RegisterDebugEvent("DebugMessage", debugMessage);
            //			}
        }
    }
#endif

    public override bool IsStillValid()
    {
        if (!base.IsStillValid())
        {
            return false;
        }

        if (Group == null)
            return false;

        return Group.StillPresent;
    }

    public override void FinalizeLoad()
    {
        base.FinalizeLoad();

        Group = World.GetGroup(GroupId);

        if (Group == null)
        {
            throw new System.Exception("CellGroupEvent: Group with Id:" + GroupId + " not found");
        }
    }

    public override void Reset(long newTriggerDate)
    {
        Reset(newTriggerDate, GenerateUniqueIdentifier(Group, newTriggerDate, TypeId));

#if DEBUG
        GenerateDebugMessage(true);
#endif
    }
}
