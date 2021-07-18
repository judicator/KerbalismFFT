using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using FarFutureTechnologies;
using KERBALISM;
using SystemHeat;

namespace KerbalismFFT
{
    class FFTFusionReactorKerbalismUpdater : PartModule
    {
		public static string brokerName = "FFTFusionReactor";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismFFT_Brokers_FusionReactor");

		[KSPField(isPersistant = true)]
		public bool FirstLoad = true;

		// This should correspond to the related FusionReactor module
		[KSPField(isPersistant = true)]
		public string reactorModuleID;

		[KSPField(isPersistant = true)]
		public int lastReactorModeIndex = 0;
		[KSPField(isPersistant = true)]
		public float MaxECGeneration = 0f;
		[KSPField(isPersistant = true)]
		public float MinThrottle = 0.1f;

		[KSPField(isPersistant = true)]
		public bool ReactorHasStarted = false;
		[KSPField(isPersistant = true)]
		public bool EmitterRunning = true;
		[KSPField(isPersistant = true)]
		public double EmitterMaxRadiation = 0d;
		[KSPField(isPersistant = true)]
		public bool LastReactorState = false;
		[KSPField(isPersistant = true)]
		public double ReactorStoppedTimestamp = 0d;
		[KSPField(isPersistant = true)]
		public double MinEmissionPercent = 0d;
		[KSPField(isPersistant = true)]
		public double EmissionDecayRate = 1d;

		protected static string reactorModuleName = "FusionReactor";
		protected FusionReactor reactorModule;

		protected bool modesListParsed = false;
		protected List<FusionReactorMode> modes;

		// Radiation source on part
		protected Emitter emitter;

		public virtual void Start()
		{
			if (Lib.IsFlight() || Lib.IsEditor())
			{
				if (reactorModule == null)
				{
					reactorModule = FindReactorModule(part, reactorModuleID);
				}
				if (Features.Radiation && emitter == null)
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
					if (reactorModule != null)
					{
						MinThrottle = reactorModule.MinimumReactorPower;
						ParseModesList(part);
						MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
					}
					FirstLoad = false;
				}
				else
				{
					EmitterRunning = true;
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			ParseModesList(part);
		}

		// Fetch modes list from fusion reactor ConfigNode
		protected void ParseModesList(Part part)
		{
			if (!modesListParsed)
			{
				ConfigNode node = ModuleUtils.GetModuleConfigNode(part, reactorModuleName);
				if (node != null)
				{
					ConfigNode[] varNodes = node.GetNodes("FUSIONMODE");
					modes = new List<FusionReactorMode>();
					for (int i = 0; i < varNodes.Length; i++)
					{
						modes.Add(new FusionReactorMode(varNodes[i]));
					}
				}
				modesListParsed = true;
			}
		}

		public virtual void FixedUpdate()
		{
			if (reactorModule != null)
			{
				if (lastReactorModeIndex != reactorModule.currentModeIndex)
				{
					lastReactorModeIndex = reactorModule.currentModeIndex;
					if (Lib.IsEditor())
					{
						KFFTUtils.UpdateKerbalismPlannerUINow();
					}
					if (!modesListParsed)
					{
						ParseModesList(part);
					}
					MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
				}
				if (Lib.IsFlight())
				{
					if (Features.Radiation && emitter != null)
					{
						if (!ReactorHasStarted && !reactorModule.Enabled && EmitterRunning)
						{
							// Disable radiation source, because reactor has not started yet
							emitter.running = false;
							EmitterRunning = false;
						}
						if (!ReactorHasStarted && reactorModule.Enabled)
						{
							// Reactor has started - enable radiation source
							ReactorHasStarted = true;
							emitter.running = true;
							emitter.radiation = EmitterMaxRadiation;
						}
						if (LastReactorState != reactorModule.Enabled)
						{
							LastReactorState = reactorModule.Enabled;
							if (reactorModule.Enabled)
							{
								// Reactor has started again - set radiation source emission to maximum
								emitter.radiation = EmitterMaxRadiation;
								ReactorStoppedTimestamp = 0d;
							}
							else
							{
								// Reactor has stopped - save timestamp, when it happened
								ReactorStoppedTimestamp = Planetarium.GetUniversalTime();
							}
						}
						if (!reactorModule.Enabled && ReactorHasStarted && ReactorStoppedTimestamp > 0 && MinEmissionPercent < 100)
						{
							// Radiation decay
							double MinRadiation = EmitterMaxRadiation * MinEmissionPercent / 100;
							if (EmissionDecayRate <= 0)
							{
								emitter.radiation = MinRadiation;
								ReactorStoppedTimestamp = 0d;
							}
							else
							{
								double secondsPassed = Planetarium.GetUniversalTime() - ReactorStoppedTimestamp;
								if (secondsPassed > 0)
								{
									double NewRadiation = EmitterMaxRadiation * (100 - secondsPassed / EmissionDecayRate) / 100;
									if (NewRadiation <= MinRadiation)
									{
										NewRadiation = MinRadiation;
										ReactorStoppedTimestamp = 0d;
									}
									emitter.radiation = NewRadiation;
								}
							}
						}
					}
				}
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (reactorModule != null)
			{
				if (MaxECGeneration > 0)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", MaxECGeneration));
				}
				foreach (ResourceRatio ratio in modes[lastReactorModeIndex].inputs)
				{
					resourceChangeRequest.Add(new KeyValuePair<string, double>(ratio.ResourceName, -ratio.Ratio));
				}
				return brokerTitle;
			}
			return "ERR: no reactor";
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			ProtoPartModuleSnapshot reactor = KFFTUtils.FindPartModuleSnapshot(part_snapshot, reactorModuleName);
			if (reactor != null)
			{
				if (Lib.Proto.GetBool(reactor, "Enabled"))
				{
					float maxECGeneration = Lib.Proto.GetFloat(module_snapshot, "MaxECGeneration");
					float minThrottle = Lib.Proto.GetFloat(module_snapshot, "MinThrottle");
					int modeIndex = Lib.Proto.GetInt(module_snapshot, "lastReactorModeIndex");
					bool needToStopReactor = false;
					float curThrottle = 1.0f;

					if (maxECGeneration > 0)
					{
						VesselResources resources = KERBALISM.ResourceCache.Get(v);
						if (!(proto_part_module as FFTFusionReactorKerbalismUpdater).modesListParsed)
						{
							(proto_part_module as FFTFusionReactorKerbalismUpdater).ParseModesList(proto_part);
						}

						// Mininum reactor throttle
						// Some input/output resources will always be consumed/produced as long as minThrottle > 0
						if (minThrottle > 0)
						{
							ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
								brokerName,
								KERBALISM.ResourceBroker.BrokerCategory.Converter,
								brokerTitle));
							foreach (ResourceRatio ir in (proto_part_module as FFTFusionReactorKerbalismUpdater).modes[modeIndex].inputs)
							{
								recipe.AddInput(ir.ResourceName, ir.Ratio * minThrottle * elapsed_s);
								if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
								{
									// Input resource amount is zero - stop reactor
									needToStopReactor = true;
								}
							}
							recipe.AddOutput("ElectricCharge", minThrottle * maxECGeneration * elapsed_s, dump: true);
							resources.AddRecipe(recipe);
						}

						if (!needToStopReactor)
						{
							curThrottle -= minThrottle;
							if (curThrottle > 0)
							{
								ResourceRecipe recipe = new ResourceRecipe(KERBALISM.ResourceBroker.GetOrCreate(
									brokerName,
									KERBALISM.ResourceBroker.BrokerCategory.Converter,
									brokerTitle));
								foreach (ResourceRatio ir in (proto_part_module as FFTFusionReactorKerbalismUpdater).modes[modeIndex].inputs)
								{
									recipe.AddInput(ir.ResourceName, ir.Ratio * curThrottle * elapsed_s);
									if (resources.GetResource(v, ir.ResourceName).Amount < double.Epsilon)
									{
										// Input resource amount is zero - stop reactor
										needToStopReactor = true;
									}
								}
								recipe.AddOutput("ElectricCharge", curThrottle * maxECGeneration * elapsed_s, dump: false);
								resources.AddRecipe(recipe);
							}
						}
						// Disable reactor
						if (needToStopReactor)
						{
							Lib.Proto.Set(reactor, "Enabled", false);
							Lib.Proto.Set(reactor, "CurrentCharge", 0f);
							Lib.Proto.Set(reactor, "Charged", false);
						}
					}
					else
					{
						// Reactor disabled - radiation decay mechanics
						if (Features.Radiation &&
							Lib.Proto.GetBool(module_snapshot, "ReactorHasStarted") &&
							Lib.Proto.GetDouble(module_snapshot, "ReactorStoppedTimestamp") > 0 &&
							Lib.Proto.GetDouble(module_snapshot, "MinEmissionPercent") < 100)
						{
							ProtoPartModuleSnapshot emitter = KFFTUtils.FindPartModuleSnapshot(part_snapshot, "Emitter");
							if (emitter != null)
							{
								double EmitterMaxRadiation = Lib.Proto.GetDouble(module_snapshot, "EmitterMaxRadiation");
								double MinEmissionPercent = Lib.Proto.GetDouble(module_snapshot, "MinEmissionPercent");
								double EmissionDecayRate = Lib.Proto.GetDouble(module_snapshot, "EmissionDecayRate");
								double MinRadiation = EmitterMaxRadiation * MinEmissionPercent / 100;
								if (EmissionDecayRate <= 0)
								{
									Lib.Proto.Set(emitter, "radiation", MinRadiation);
									Lib.Proto.Set(module_snapshot, "ReactorStoppedTimestamp", 0d);
								}
								else
								{
									double secondsPassed = Planetarium.GetUniversalTime() - Lib.Proto.GetDouble(module_snapshot, "ReactorStoppedTimestamp");
									if (secondsPassed > 0)
									{
										double NewRadiation = EmitterMaxRadiation * (100 - secondsPassed / EmissionDecayRate) / 100;
										if (NewRadiation <= MinRadiation)
										{
											NewRadiation = MinRadiation;
											Lib.Proto.Set(module_snapshot, "ReactorStoppedTimestamp", 0d);
										}
										Lib.Proto.Set(emitter, "radiation", NewRadiation);
									}
								}
							}
						}
					}
				}
				return brokerTitle;
			}
			return "ERR: no reactor";
		}

		// Find associated Reactor module
		public FusionReactor FindReactorModule(Part part, string moduleName)
		{
			FusionReactor reactor = part.GetComponents<FusionReactor>().ToList().Find(x => x.ModuleID == moduleName);

			if (reactor == null)
			{
				KFFTUtils.LogError($"[{part}] No FusionReactor named {moduleName} was found, using first instance.");
				reactorModule = part.GetComponents<FusionReactor>().ToList().First();
			}
			if (reactor == null)
			{
				KFFTUtils.LogError($"[{part}] No FusionReactor was found.");
			}
			return reactor;
		}

		// Find Emitter module on part (Kerbalism radiation source)
		public Emitter FindEmitterModule(Part part)
		{
			Emitter emitter = part.GetComponents<Emitter>().ToList().First();
			if (emitter == null)
			{
				KFFTUtils.LogWarning($"[{part}] No radiation Emitter was found.");
			}
			return emitter;
		}
	}
}

