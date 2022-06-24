﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class PolityFormationEventMessage : CellEventMessage
{
    [XmlAttribute]
    public bool First = false;

    #region PolityId
    [XmlAttribute("PId")]
    public string PolityIdStr
    {
        get { return PolityId; }
        set { PolityId = value; }
    }
    [XmlIgnore]
    public Identifier PolityId;
    #endregion

    public PolityFormationEventMessage()
    {

    }

    public PolityFormationEventMessage(Polity polity, long date) :
        base(polity.CoreGroup.Cell, WorldEvent.PolityFormationEventId, date)
    {
        PolityId = polity.Id;
    }

    protected override string GenerateMessage()
    {
        PolityInfo polityInfo = World.GetPolityInfo(PolityId);

        if (First)
        {
            return "The first polity, " + polityInfo.Name.BoldText + ", formed at " + Position;
        }
        else
        {
            return "A new polity, " + polityInfo.Name.BoldText + ", formed at " + Position;
        }
    }
}
