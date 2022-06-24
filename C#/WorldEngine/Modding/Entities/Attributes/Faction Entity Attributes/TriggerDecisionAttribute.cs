﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class DecisionTriggerPriority
{
    public const string Decision = "decision_prio";
    public const string Action = "action_prio";
    public const string Event = "event_prio";

    public static readonly HashSet<string> Values =
        new HashSet<string> { Decision, Action, Event };
}

public class TriggerDecisionAttribute : EffectEntityAttribute
{
    private FactionEntity _factionEntity;

    private ModDecision _decisionToTrigger = null;

    private readonly IValueExpression<string> _decisionIdExp;
    private readonly IBaseValueExpression[] _parameterExps;

    public TriggerDecisionAttribute(FactionEntity factionEntity, IExpression[] arguments)
        : base(FactionEntity.TriggerDecisionAttributeId, factionEntity, arguments, 1)
    {
        _factionEntity = factionEntity;

        _decisionIdExp = ValueExpressionBuilder.ValidateValueExpression<string>(arguments[0]);

        if (arguments.Length > 1)
        {
            _parameterExps = new IBaseValueExpression[arguments.Length - 1];

            for (int i = 0; i < _parameterExps.Length; i++)
            {
                _parameterExps[i] =
                    ValueExpressionBuilder.ValidateValueExpression(arguments[i + 1]);
            }
        }
        else
        {
            _parameterExps = null;
        }
    }

    public override void Apply(IEffectTrigger trigger)
    {
        string decisionId = _decisionIdExp.Value;

        if (!ModDecision.Decisions.TryGetValue(decisionId, out _decisionToTrigger))
        {
            throw new System.Exception("Decision \"" + decisionId +
                "\" not present on list of available decisions. " +
                "Check the source mod for inconsistencies");
        }

        string triggerPrio = DecisionTriggerPriority.Decision;

        if (!DecisionTriggerPriority.Values.Contains(triggerPrio))
        {
            throw new System.Exception("Decision \"" + decisionId +
                "\": '" + triggerPrio + "' not a valid priority value. " +
                "Check the source mod for inconsistencies");
        }

        _decisionToTrigger.Set(trigger, Entity.Context, triggerPrio, _factionEntity.Faction, _parameterExps);
    }
}
