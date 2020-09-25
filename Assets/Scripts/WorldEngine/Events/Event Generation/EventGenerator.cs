﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine.Profiling;

/// <summary>
/// Object that generates events of a certain type during the simulation run
/// </summary>
public abstract class EventGenerator : Context, IWorldEventGenerator
{
    public const string AssignOnSpawn = "spawn";
    public const string AssignOnEvent = "event";
    public const string AssignOnStatusChange = "status_change";
    public const string AssignOnPolityContactChange = "polity_contact_change";

    public const string FactionTargetType = "faction";
    public const string GroupTargetType = "group";

    public const string TargetEntityId = "target";

    public static Dictionary<string, EventGenerator> Generators;

    /// <summary>
    /// Global UId counter
    /// </summary>
    public static long CurrentUId = StartUId;

    /// <summary>
    /// The UId to use for events generated by this generator
    /// </summary>
    public long UId;

    /// <summary>
    /// Name to use in the UI for events of this type
    /// </summary>
    public string Name;

    /// <summary>
    /// Hash to use for RNGs that use events of this type
    /// </summary>
    public int IdHash;

    /// <summary>
    /// Id for flag to set when this event has already been assigned to target
    /// </summary>
    public string EventSetFlag { get; private set; }

    public bool Repeteable = false;

    public string[] AssignOn = null;

    /// <summary>
    /// Conditions that decide if an event should be assigned to a target
    /// </summary>
    public IValueExpression<bool>[] AssignmentConditions;
    /// <summary>
    /// Conditions that decide if an event should trigger
    /// </summary>
    public IValueExpression<bool>[] TriggerConditions;

    /// <summary>
    /// Maximum time to pass before event triggers (since assignment date)
    /// </summary>
    public IValueExpression<float> TimeToTrigger;

    /// <summary>
    /// Effects to occur after an event triggers
    /// </summary>
    public IEffectExpression[] Effects;

    /// <summary>
    /// First UId to use for events loaded from mods
    /// </summary>
    protected const long StartUId = WorldEvent.PlantCultivationDiscoveryEventId + 1;

    public static void ResetGenerators()
    {
        Generators = new Dictionary<string, EventGenerator>();
    }

    public static void LoadEventFile(string filename)
    {
        foreach (EventGenerator generator in EventLoader.Load(filename))
        {
            if (Generators.ContainsKey(generator.Id))
            {
                Generators[generator.Id] = generator;
            }
            else
            {
                Generators.Add(generator.Id, generator);
            }
        }
    }

    public static void InitializeGenerators()
    {
        foreach (EventGenerator generator in Generators.Values)
        {
            generator.Initialize();
        }
    }

    public abstract void SetToAssignOnSpawn();
    public abstract void SetToAssignOnEvent();
    public abstract void SetToAssignOnStatusChange();
    public abstract void SetToAssignOnPolityContactChange();

    public virtual void Initialize()
    {
        EventSetFlag = Id + "_set";

        World.EventGenerators.Add(Id, this);

        foreach (string assignOn in AssignOn)
        {
            switch (assignOn)
            {
                case AssignOnSpawn:
                    SetToAssignOnSpawn();
                    break;

                case AssignOnEvent:
                    SetToAssignOnEvent();
                    break;

                case AssignOnStatusChange:
                    SetToAssignOnStatusChange();
                    break;

                case AssignOnPolityContactChange:
                    SetToAssignOnPolityContactChange();
                    break;

                default:
                    throw new System.Exception(
                        "Unhandled event assignOn type: " + assignOn);
            }
        }
    }

    public static EventGenerator GetGenerator(string id)
    {
        return !Generators.TryGetValue(id, out EventGenerator g) ? null : g;
    }

    public static EventGenerator BuildGenerator(string targetStr)
    {
        switch (targetStr)
        {
            case FactionTargetType:
                return new FactionEventGenerator();
            case GroupTargetType:
                return new CellGroupEventGenerator();
        }

        throw new System.ArgumentException("Invalid target type: " + targetStr);
    }

    protected bool CanAssignEventToTarget()
    {
        OpenDebugOutput("Evaluating Assigment Conditions:");

        if (AssignmentConditions != null)
        {
            foreach (IValueExpression<bool> exp in AssignmentConditions)
            {
                bool value = exp.Value;

                if (DebugEnabled)
                {
                    string expStr = exp.ToString();
                    string expPartialStr = exp.ToPartiallyEvaluatedString(true);

                    AddDebugOutput("  Condition: " + expStr +
                     "\n   - Partial eval: " + expPartialStr +
                     "\n   - Result: " + value);
                }

                if (!value)
                {
                    CloseDebugOutput("Assigment Result: False");
                    return false;
                }
            }
        }

        CloseDebugOutput("Assigment Result: True");
        return true;
    }

    public bool CanTriggerEvent()
    {
        Profiler.BeginSample("EventGenerator - CanTriggerEvent - Id:" + Id);

        OpenDebugOutput("Evaluating Trigger Conditions:");

        Profiler.BeginSample("EventGenerator - CanAssignEventToTarget");

        // Always validate that the target is still valid
        if (!CanAssignEventToTarget())
        {
            Profiler.EndSample(); // "EventGenerator - CanAssignEventToTarget"
            Profiler.EndSample(); // "EventGenerator - CanTriggerEvent"

            CloseDebugOutput("Trigger Result: False");
            return false;
        }

        Profiler.EndSample(); // "EventGenerator - CanAssignEventToTarget"

        Profiler.BeginSample("EventGenerator - TriggerConditions");

        if (TriggerConditions != null)
        {
            foreach (IValueExpression<bool> exp in TriggerConditions)
            {
                bool value = exp.Value;

                if (DebugEnabled)
                {
                    string expStr = exp.ToString();
                    string expPartialStr = exp.ToPartiallyEvaluatedString(true);

                    AddDebugOutput("  Condition: " + expStr +
                     "\n   - Partial eval: " + expPartialStr +
                     "\n   - Result: " + value);
                }

                if (!value)
                {
                    Profiler.EndSample(); // "EventGenerator - TriggerConditions"
                    Profiler.EndSample(); // "EventGenerator - CanTriggerEvent"

                    CloseDebugOutput("Trigger Result: False");
                    return false;
                }
            }
        }

        Profiler.EndSample(); // "EventGenerator - TriggerConditions"
        Profiler.EndSample(); // "EventGenerator - CanTriggerEvent"

        CloseDebugOutput("Trigger Result: True");
        return true;
    }

    protected long CalculateEventTriggerDate(World world)
    {
        float timeToTrigger = TimeToTrigger.Value;

        if (timeToTrigger < 0)
        {
            throw new System.Exception(
                "ERROR: EventGenerator.CalculateEventTriggerDate - timeToTrigger less than 0" +
                "\n - event id: " + Id +
                "\n - timeToTrigger expression: " + TimeToTrigger.ToPartiallyEvaluatedString() +
                "\n - time to trigger (days): " + timeToTrigger);
        }

        long targetDate = world.CurrentDate + (long)timeToTrigger + 1;

        if ((targetDate <= world.CurrentDate) || (targetDate > World.MaxSupportedDate))
        {
            // log details about invalid date
            UnityEngine.Debug.LogWarning("EventGenerator.CalculateEventTriggerDate - target date (" + Manager.GetDateString(targetDate) +
                ") less than or equal to world's current date (" + Manager.GetDateString(world.CurrentDate) +
                ")\n - event id: " + Id +
                "\n - timeToTrigger expression: " + TimeToTrigger.ToPartiallyEvaluatedString() +
                "\n - time to trigger (days): " + timeToTrigger);

            return long.MinValue;
        }

        return targetDate;
    }

    public void TriggerEvent()
    {
        foreach (IEffectExpression exp in Effects)
        {
            exp.Apply();
        }
    }

    protected abstract WorldEvent GenerateEvent(long triggerDate);

    protected bool TryGenerateEventAndAssign(
        World world,
        WorldEvent originalEvent)
    {
        if (!CanAssignEventToTarget())
        {
            return false;
        }

        long triggerDate = CalculateEventTriggerDate(world);

        if (triggerDate < 0)
        {
            // Do not generate an event. CalculateTriggerDate() should have
            // logged more details...
            UnityEngine.Debug.LogWarning(
                "EventGenerator.TryGenerateEventAndAssign - failed to generate a valid trigger date: " +
                triggerDate);
            return false;
        }

        if (originalEvent == null)
        {
            originalEvent = GenerateEvent(triggerDate);
        }
        else
        {
            originalEvent.Reset(triggerDate);
        }

        world.InsertEventToHappen(originalEvent);

        return true;
    }

    public string GetEventGeneratorId()
    {
        return Id;
    }
}
