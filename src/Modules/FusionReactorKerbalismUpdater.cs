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

		protected static string reactorModuleName = "FusionReactor";
		protected FusionReactor reactorModule;

		protected bool modesListParsed = false;
		protected List<FusionReactorMode> modes;

		public virtual void Start()
		{
			if (Lib.IsFlight() || Lib.IsEditor())
			{
				if (reactorModule == null)
				{
					reactorModule = FindReactorModule(part, reactorModuleID);
				}
				if (FirstLoad)
				{
					if (reactorModule != null)
					{
						MinThrottle = reactorModule.MinimumReactorPower;
						ParseModesList(part);
						MaxECGeneration = modes[lastReactorModeIndex].powerGeneration;
					}
					FirstLoad = false;
				}
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			//ParseModesList(part);
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
	}
}

