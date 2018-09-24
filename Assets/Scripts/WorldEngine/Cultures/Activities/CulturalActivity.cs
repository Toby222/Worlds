using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

[XmlInclude(typeof(CellCulturalActivity))]
public class CulturalActivity : CulturalActivityInfo
{
    [XmlAttribute]
    public float Value;

    [XmlAttribute]
    public float Contribution = 0;

    public CulturalActivity()
    {
    }

    public CulturalActivity(string id, string name, int rngOffset, float value, float contribution) : base(id, name, rngOffset)
    {
        Value = value;
        Contribution = contribution;
    }

    public CulturalActivity(CulturalActivity baseActivity) : base(baseActivity)
    {
        Value = baseActivity.Value;
        Contribution = baseActivity.Contribution;
    }
}
