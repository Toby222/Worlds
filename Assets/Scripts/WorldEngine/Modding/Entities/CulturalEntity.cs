﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public abstract class CulturalEntity<T> : DelayedSetEntity<T>
{
    public const string PreferencesAttributeId = "preferences";
    public const string SkillsAttributeId = "skills";
    public const string SkillsActivitiesId = "activities";
    public const string KnowledgesAttributeId = "knowledges";
    public const string DiscoveriesAttributeId = "discoveries";

    private CulturalPreferencesEntity _preferencesEntity = null;
    private CulturalSkillsEntity _skillsEntity = null;
    private CulturalActivitiesEntity _activitiesEntity = null;
    private CulturalKnowledgesEntity _knowledgesEntity = null;
    private ICulturalDiscoveriesEntity _discoveriesEntity = null;

    public CulturalEntity(Context c, string id, IEntity parent) : base(c, id, parent)
    {
    }

    public CulturalEntity(
        ValueGetterMethod<T> getterMethod, Context c, string id, IEntity parent)
        : base(getterMethod, c, id, parent)
    {
    }

    public CulturalEntity(
        TryRequestGenMethod<T> tryRequestGenMethod, Context c, string id, IEntity parent)
        : base(tryRequestGenMethod, c, id, parent)
    {
    }

    protected virtual CulturalPreferencesEntity CreateCulturalPreferencesEntity() => 
        new AssignableCulturalPreferencesEntity(
            GetCulture,
            Context,
            BuildAttributeId(PreferencesAttributeId),
            this);

    private CulturalSkillsEntity CreateCulturalSkillsEntity() =>
        new CulturalSkillsEntity(
            GetCulture,
            Context,
            BuildAttributeId(SkillsAttributeId),
            this);

    private EntityAttribute GetPreferencesAttribute()
    {
        _preferencesEntity =
            _preferencesEntity ?? CreateCulturalPreferencesEntity();

        return _preferencesEntity.GetThisEntityAttribute();
    }

    private EntityAttribute GetSkillsAttribute()
    {
        _skillsEntity =
            _skillsEntity ?? CreateCulturalSkillsEntity();

        return _skillsEntity.GetThisEntityAttribute();
    }

    private EntityAttribute GetKnowledgesAttribute()
    {
        _knowledgesEntity =
            _knowledgesEntity ?? new CulturalKnowledgesEntity(
                GetCulture,
                Context,
                BuildAttributeId(KnowledgesAttributeId),
                this);

        return _knowledgesEntity.GetThisEntityAttribute();
    }

    protected virtual ICulturalDiscoveriesEntity CreateCulturalDiscoveriesEntity() =>
        new CulturalDiscoveriesEntity(
            GetCulture,
            Context,
            BuildAttributeId(DiscoveriesAttributeId),
            this);

    private EntityAttribute GetDiscoveriesAttribute()
    {
        _discoveriesEntity =
            _discoveriesEntity ?? CreateCulturalDiscoveriesEntity();

        return _discoveriesEntity.GetThisEntityAttribute();
    }

    public override EntityAttribute GetAttribute(string attributeId, IExpression[] arguments = null)
    {
        switch (attributeId)
        {
            case PreferencesAttributeId:
                return GetPreferencesAttribute();

            case SkillsAttributeId:
                return GetSkillsAttribute();

            case KnowledgesAttributeId:
                return GetKnowledgesAttribute();

            case DiscoveriesAttributeId:
                return GetDiscoveriesAttribute();
        }

        return base.GetAttribute(attributeId, arguments);
    }

    protected override void ResetInternal()
    {
        _preferencesEntity?.Reset();
        _knowledgesEntity?.Reset();
        _discoveriesEntity?.Reset();
    }

    public abstract Culture GetCulture();
}
