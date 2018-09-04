﻿using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class PolityProminenceCluster : ISynchronizable
{
    public const int MaxClusterSize = 50;
    public const int MinSplitClusterSize = 25;

    [XmlAttribute("Pid")]
    public long PolityId;

    [XmlAttribute("Id")]
    public long Id;

    public DelayedLoadXmlSerializableDictionary<long, PolityProminence> Prominences = new DelayedLoadXmlSerializableDictionary<long, PolityProminence>();

    public int Size
    {
        get
        {
            return Prominences.Count;
        }
    }

    private Polity _polity;

    public PolityProminenceCluster(PolityProminence startProminence)
    {
        Id = startProminence.Id;
        PolityId = startProminence.PolityId;
        _polity = startProminence.Polity;

        AddProminence(startProminence);
    }

    public void AddProminence(PolityProminence prominence)
    {
        Prominences.Add(prominence.Id, prominence);
        prominence.Cluster = this;
    }

    public void RemoveProminence(PolityProminence prominence)
    {
        Prominences.Remove(prominence.Id);
        prominence.Cluster = null;
    }

    public bool HasPolityProminence(PolityProminence prominence)
    {
        return Prominences.ContainsKey(prominence.Id);
    }

    public bool HasPolityProminence(CellGroup group)
    {
        return Prominences.ContainsKey(group.Id);
    }

    public PolityProminence GetPolityProminence(CellGroup group)
    {
        if (Prominences.ContainsKey(group.Id))
        {
            return Prominences[group.Id];
        }

        return null;
    }

    public PolityProminenceCluster Split(PolityProminence startProminence)
    {
        PolityProminenceCluster splitCluster = new PolityProminenceCluster(startProminence);

        HashSet<CellGroup> groupsAlreadyTested = new HashSet<CellGroup>
        {
            startProminence.Group
        };

        Queue<PolityProminence> prominencesToExplore = new Queue<PolityProminence>(Prominences.Count + 1);
        prominencesToExplore.Enqueue(startProminence);

        splitCluster.AddProminence(startProminence);

        bool continueExploring = true;
        while (continueExploring)
        {
            PolityProminence prominenceToExplore = prominencesToExplore.Dequeue();

            foreach (CellGroup nGroup in prominenceToExplore.Group.Neighbors.Values)
            {
                if (groupsAlreadyTested.Contains(nGroup))
                    continue;

                if (HasPolityProminence(nGroup))
                {
                    PolityProminence prominenceToAdd = Prominences[nGroup.Id];

                    RemoveProminence(prominenceToAdd);
                    splitCluster.AddProminence(prominenceToAdd);

                    if (Size <= MinSplitClusterSize)
                    {
                        continueExploring = false;
                        break;
                    }

                    prominencesToExplore.Enqueue(prominenceToAdd);
                }

                groupsAlreadyTested.Add(nGroup);
            }
        }

        return splitCluster;
    }

    private PolityProminence GetProminenceOrThrow(long id)
    {
        CellGroup group = _polity.World.GetGroup(id);

        if (group == null)
        {
            string message = "Missing Group with Id " + id + " in PolityProminenceCluster of Polity with Id " + PolityId;
            throw new System.Exception(message);
        }

        PolityProminence prominence = group.GetPolityProminence(_polity);

        if (prominence == null)
        {
            string message = "Missing polity prominence with Id " + id + " in PolityProminenceCluster of Polity with Id " + PolityId;
            throw new System.Exception(message);
        }

        return prominence;
    }

    public void FinalizeLoad()
    {
        Prominences.FinalizeLoad(GetProminenceOrThrow);
    }

    public void Synchronize()
    {
    }
}
