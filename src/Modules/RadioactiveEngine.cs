using System;
using System.Collections.Generic;
using System.Linq;
using KERBALISM;

namespace KerbalismFFT
{
    class FFTRadioactiveEngine : PartModule
    {
		[KSPField(isPersistant = true)]
		public bool FirstLoad = true;

		[KSPField(isPersistant = true)]
		public string engineID1;
		[KSPField(isPersistant = true)]
		public string engineID2;
		[KSPField(isPersistant = false)]
		public FloatCurve EmissionPercentEngine1 = new FloatCurve();
		[KSPField(isPersistant = false)]
		public FloatCurve EmissionPercentEngine2 = new FloatCurve();

		[KSPField(isPersistant = true)]
		public bool EngineHasStarted = false;
		[KSPField(isPersistant = true)]
		public bool EmitterRunning = true;
		[KSPField(isPersistant = true)]
		public double GoalEmission = 0d;
		[KSPField(isPersistant = true)]
		public double EmitterMaxRadiation = 0d;
		[KSPField(isPersistant = true)]
		public double EmitterRadiationBeforeEngineShutdown = 0d;
		[KSPField(isPersistant = true)]
		public bool LastEngineState = false;
		[KSPField(isPersistant = true)]
		public double LastUpdateTime = 0d;
		[KSPField(isPersistant = true)]
		public double MinEmissionPercent = 0d;
		[KSPField(isPersistant = true)]
		public double EmissionDecayRate = 1d;

		protected ModuleEnginesFX engineModule1;
		protected ModuleEnginesFX engineModule2;

		protected Emitter emitter;

		public virtual void Start()
		{
			if (Features.Radiation && (Lib.IsFlight() || Lib.IsEditor()))
			{
				if (engineID1 != null && engineModule1 == null)
				{
					engineModule1 = FindEngineModule(part, engineID1);
				}
				if (engineID2 != null && engineModule2 == null)
				{
					engineModule2 = FindEngineModule(part, engineID2);
				}
				if (emitter == null)
				{
					emitter = FindEmitterModule(part);
				}
				if (FirstLoad)
				{
					if (emitter != null)
					{
						EmitterMaxRadiation = emitter.radiation;
						if (EmitterMaxRadiation < 0)
						{
							EmitterMaxRadiation = 0d;
						}
					}
					FirstLoad = false;
				}
				else
				{
					EmitterRunning = true;
				}
			}
		}

		public virtual void FixedUpdate()
		{
			if (Lib.IsFlight() && Features.Radiation && emitter != null)
			{
				bool EngineIgnited = false;
				double MinEmission = MinEmissionPercent * EmitterMaxRadiation / 100;
				if (engineModule1 != null && engineModule1.EngineIgnited)
				{
					EngineIgnited = true;
				}
				if (engineModule2 != null && engineModule2.EngineIgnited)
				{
					EngineIgnited = true;
				}
				if (!EngineHasStarted && !EngineIgnited && EmitterRunning)
				{
					// Disable radiation source, because engine has not started yet
					emitter.running = false;
					EmitterRunning = false;
				}
				if (!EngineHasStarted && EngineIgnited)
				{
					// Engine has started - enable radiation source
					EngineHasStarted = true;
					emitter.radiation = 0d;
					emitter.running = true;
				}
				if (EngineHasStarted && EngineIgnited)
				{
					// Update radiation emission value according to engine throttle
					double emission1 = 0d;
					double emission2 = 0d;
					if (engineModule1 != null && engineModule1.EngineIgnited)
					{
						emission1 = EmitterMaxRadiation * EmissionPercentEngine1.Evaluate(engineModule1.currentThrottle * 100f) / 100d;
					}
					if (engineModule2 != null && engineModule2.EngineIgnited)
					{
						emission2 = EmitterMaxRadiation * EmissionPercentEngine2.Evaluate(engineModule2.currentThrottle * 100f) / 100d;
					}
					GoalEmission = Math.Max(emission1, emission2);
					if (GoalEmission < MinEmission)
                    {
						GoalEmission = MinEmission;
					}
					if (GoalEmission > emitter.radiation)
                    {
						emitter.radiation = GoalEmission;
					}
				}
				if (EngineHasStarted && !EngineIgnited)
				{
					// Engine was shut down
					GoalEmission = MinEmission;
				}
				if (EngineHasStarted && emitter.radiation > GoalEmission)
				{
					// Radiation decay
					if (EmissionDecayRate <= 0)
					{
						emitter.radiation = GoalEmission;
					}
					else
					{
						double secondsPassed = Planetarium.GetUniversalTime() - LastUpdateTime;
						if (secondsPassed > 0)
						{
							double newEmission = emitter.radiation;
							newEmission -= EmitterMaxRadiation * (secondsPassed / EmissionDecayRate) / 100;
							if (newEmission < GoalEmission)
							{
								newEmission = GoalEmission;
							}
							emitter.radiation = newEmission;
						}
					}
				}
				LastUpdateTime = Planetarium.GetUniversalTime();
			}
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			if (Features.Radiation && Lib.Proto.GetBool(module_snapshot, "EngineHasStarted"))
			{
				ProtoPartModuleSnapshot emitter = KFFTUtils.FindPartModuleSnapshot(part_snapshot, "Emitter");
				if (emitter != null)
				{
					double MinEmissionPercent = Lib.Proto.GetDouble(module_snapshot, "EmitterMaxRadiation");
					double EmitterMaxRadiation = Lib.Proto.GetDouble(module_snapshot, "EmitterMaxRadiation");
					double EmissionDecayRate = Lib.Proto.GetDouble(module_snapshot, "EmissionDecayRate");
					double GoalEmission = MinEmissionPercent * EmitterMaxRadiation / 100;
					if (EmissionDecayRate <= 0)
					{
						Lib.Proto.Set(emitter, "radiation", GoalEmission);
					}
					else
					{
						double secondsPassed = Planetarium.GetUniversalTime() - Lib.Proto.GetDouble(module_snapshot, "LastUpdateTime");
						if (secondsPassed > 0)
						{
							double newEmission = Lib.Proto.GetDouble(emitter, "radiation");
							newEmission -= EmitterMaxRadiation * (secondsPassed / EmissionDecayRate) / 100;
							if (newEmission < GoalEmission)
							{
								newEmission = GoalEmission;
							}
							Lib.Proto.Set(emitter, "radiation", newEmission);
						}
					}
				}
			}
			Lib.Proto.Set(module_snapshot, "LastUpdateTime", Planetarium.GetUniversalTime());
			return "radioactive engine";
		}

		// Find ModuleEnginesFX module
		public ModuleEnginesFX FindEngineModule(Part part, string moduleName)
		{
			ModuleEnginesFX engine = part.GetComponents<ModuleEnginesFX>().ToList().Find(x => x.engineID == moduleName);
			if (engine == null)
			{
				KFFTUtils.LogError($"[FFTRadioactiveEngine][{part}] No ModuleEnginesFX named {moduleName} was found.");
			}
			return engine;
		}

		// Find Emitter module on part (Kerbalism radiation source)
		public Emitter FindEmitterModule(Part part)
		{
			Emitter emitter = part.GetComponents<Emitter>().ToList().First();
			if (emitter == null)
			{
				KFFTUtils.LogError($"[FFTRadioactiveEngine][{part}] No radiation Emitter was found.");
			}
			return emitter;
		}
	}
}
