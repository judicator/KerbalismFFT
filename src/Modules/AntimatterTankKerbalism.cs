using KSP.Localization;
using System.Collections.Generic;
using FarFutureTechnologies;
using KERBALISM;

namespace KerbalismFFT
{
	public class FFTModuleAntimatterTankKerbalism: ModuleAntimatterTank
	{
		public static string brokerName = "FFTAntimatterTank";
		public static string brokerTitle = Localizer.Format("#LOC_KerbalismFFT_Brokers_AntimatterTank");

		[KSPField(isPersistant = true)]
		public float ThermalFluxToAddOnLoad = 0f;

		public override void OnAwake()
		{
			base.OnAwake();
			if (Lib.IsFlight())
			{
				GameEvents.onPartUnpack.Add(new EventData<Part>.OnEvent(GoOffRails));
			}
		}

		void OnDestroy()
		{
			// Clean up events when the item is destroyed
			GameEvents.OnVesselRollout.Remove(OnVesselRollout);
			GameEvents.onPartUnpack.Remove(GoOffRails);
		}

		public virtual void GoOffRails(Part p)
		{
			if (ThermalFluxToAddOnLoad > 0)
			{
				KFFTUtils.Log("Antimatter containment for tank " + part.partInfo.title + " on vessel " + vessel.GetDisplayName() + " was turned off due to EC loss. " + ThermalFluxToAddOnLoad.ToString() + " KW of heat was added to part as a resut of antimatter detonation.");
				part.AddThermalFlux(ThermalFluxToAddOnLoad);
				ThermalFluxToAddOnLoad = 0f;
			}
		}

		// Estimate resources production/consumption for Kerbalism planner
		// This will be called by Kerbalism in the editor (VAB/SPH), possibly several times after a change to the vessel
		public string PlannerUpdate(List<KeyValuePair<string, double>> resourceChangeRequest, CelestialBody body, Dictionary<string, double> environment)
		{
			if (GetResourceAmount(FuelName) > 0.0 && ContainmentEnabled && ContainmentCost > 0f)
			{
				resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ContainmentCost));
			}
			return brokerTitle;
		}

		// Simulate resources production/consumption for unloaded vessel
		public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
		{
			// If containment enabled
			if (Lib.Proto.GetBool(module_snapshot, "ContainmentEnabled"))
			{
				float ContainmentCost = (proto_part_module as FFTModuleAntimatterTankKerbalism).ContainmentCost;
				if (ContainmentCost > 0)
				{
					double EC = KERBALISM.ResourceCache.Get(v).GetResource(v, "ElectricCharge").Amount;
					resourceChangeRequest.Add(new KeyValuePair<string, double>("ElectricCharge", -ContainmentCost));
					if (EC < ContainmentCost)
                    {
						Lib.Proto.Set(module_snapshot, "ContainmentEnabled", false);
						Message.Post(
							Severity.danger,
							Localizer.Format(
								"#LOC_KerbalismFFT_AntimatterTank_Detonation_Msg",
								v.GetDisplayName())
						);
					}
				}
			}
			else
            {
				float DetonationKJPerUnit = (proto_part_module as FFTModuleAntimatterTankKerbalism).DetonationKJPerUnit;
				float DetonationRate = (proto_part_module as FFTModuleAntimatterTankKerbalism).DetonationRate;
				string FuelName = (proto_part_module as FFTModuleAntimatterTankKerbalism).FuelName;

				ResourceInfo antimatter = KERBALISM.ResourceCache.GetResource(v, FuelName);
				double detonatedAmount = elapsed_s * DetonationRate;
				if (antimatter.Amount < detonatedAmount)
				{
					detonatedAmount = antimatter.Amount;
				}
				antimatter.Consume(detonatedAmount, KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, brokerTitle));
				float ThermalFluxToAddOnLoad = Lib.Proto.GetFloat(module_snapshot, "ThermalFluxToAddOnLoad");
				ThermalFluxToAddOnLoad += (float) detonatedAmount * DetonationKJPerUnit;
				Lib.Proto.Set(module_snapshot, "ThermalFluxToAddOnLoad", ThermalFluxToAddOnLoad);
			}
			return brokerTitle;
		}

		// Calculate resources production/consumption for active vessel
		public string ResourceUpdate(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
		{
			if (ContainmentEnabled && ContainmentCost > 0f)
			{
				ResourceInfo ec = KERBALISM.ResourceCache.GetResource(vessel, "ElectricCharge");
				double chargeRequest = ContainmentCost * TimeWarp.fixedDeltaTime;
				ec.Consume(chargeRequest, KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, brokerTitle));
			}
			return brokerTitle;
		}

		public new void DoCatchup()
		{
			// Do nothing
		}

		protected new void ConsumeCharge()
		{
			if (ContainmentEnabled && ContainmentCost > 0f)
			{
				ResourceInfo ec = KERBALISM.ResourceCache.GetResource(vessel, "ElectricCharge");
				double chargeRequest = ContainmentCost * TimeWarp.fixedDeltaTime;
//				ec.Consume(chargeRequest, KERBALISM.ResourceBroker.GetOrCreate(brokerName, KERBALISM.ResourceBroker.BrokerCategory.VesselSystem, brokerTitle));
				if (ec.Amount < chargeRequest)
				{
					SetPoweredState(false);
				}
				else
				{
					SetPoweredState(true);
				}
			}
		}
	}
}
