﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class AIController
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Agent Owner { get; private set; }
        public Game Game { get; private set; }
        public ProceduralAI.ProceduralAI Brain { get; private set; }
        public BehaviorSensorySystem Senses { get; private set; }
        public BehaviorBlackboard Blackboard { get; private set; }   
        public bool IsEnabled { get; private set; }
        public WorldEntity TargetEntity => Senses.GetCurrentTarget();
        public WorldEntity InteractEntity => GetInteractEntityHelper();
        public WorldEntity AssistedEntity => GetAssistedEntityHelper();

        public AIController(Game game, Agent owner)
        {
            Game = game;
            Owner = owner;
            Senses = new ();
            Blackboard = new (owner);
            Brain = new (); // TODO Init ProceduralAIProfilePrototype
        }

        public bool IsOwnerValid()
        {
            if (Owner != null && Owner.IsInWorld && Owner.IsSimulated 
                && Owner.TestStatus(EntityStatus.PendingDestroy) == false 
                && Owner.TestStatus(EntityStatus.Destroyed) == false)
                return true;
            
            return false;
        }

        public bool GetDesiredIsWalkingState(MovementSpeedOverride speedOverride)
        {
            var locomotor = Owner?.Locomotor;            
            if (locomotor != null && locomotor.SupportsWalking)
            {
                if (speedOverride == MovementSpeedOverride.Walk)               
                    return true;
                 else if (speedOverride == MovementSpeedOverride.Default) 
                    return Senses.GetCurrentTarget() == null;
            }
            else
            {
                if (speedOverride == MovementSpeedOverride.Walk)
                    Logger.Warn("An AI agent's behavior has a movement context with a MovementSpeed of Walk, " +
                        $"but the agent's Locomotor doesn't support Walking.\nAgent [{Owner}]");
            }            
            return false;
        }
        
        private WorldEntity GetInteractEntityHelper()
        {
            throw new NotImplementedException();
        }
        
        private WorldEntity GetAssistedEntityHelper()
        {
            throw new NotImplementedException();
        }

        public float AggroRangeAlly 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideAlly) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideAlly] :
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeAlly];
        }

        public float AggroRangeHostile 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideHostile) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideHostile] : 
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeHostile]; 
        }

        public void OnAIActivated()
        {
            throw new NotImplementedException();
        }

        public void OnAIEnteredWorld()
        {
            throw new NotImplementedException();
        }

        public void OnAIAllianceChange()
        {
            throw new NotImplementedException();
        }

        public void OnAIEnabled()
        {
            throw new NotImplementedException();
        }
        
        public void ProcessInterrupts()
        {
            if (Brain == null) return;
            //  Some debug stuff
            // TODO Brain ProcessInterrupts
        }

        public void SetIsEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (enabled)
                OnAIEnabled();
            else
                OnAIDisabled();
        }
        
        private void OnAIDisabled()
        {
            throw new NotImplementedException();
        }
        
        private bool HasNotExceededMaxThinksPerFrame(TimeSpan timeOffset)
        {
            if (Game == null || Brain == null) return false;

            if (timeOffset == TimeSpan.Zero && Brain.LastThinkQTime == Game.NumQuantumFixedTimeUpdates && Brain.ThinkCountPerFrame > 4)
            {
                Logger.Warn($"Tried to schedule too many thinks on the same frame. Frame: {Game.NumQuantumFixedTimeUpdates}, " +
                    $"Agent: {Owner}, Think count: {Brain?.ThinkCountPerFrame}");
                return false;
            }

            return true;
        }

        internal void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype[] genericProceduralPowers)
        {
            throw new NotImplementedException();
        }

        internal void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype primaryPower)
        {
            throw new NotImplementedException();
        }
    }
}
