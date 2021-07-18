using System;
using System.Reflection;
using UnityEngine;
using KERBALISM.Planner;

namespace KerbalismFFT
{
	public static class KFFTUtils
	{
		public static void Log(string msg)
		{
			Debug.Log("[KerbalismFFT] " + msg);
		}

		public static void LogWarning(string msg)
		{
			Debug.LogWarning("[KerbalismFFT] " + msg);
		}

		public static void LogError(string msg)
		{
			Debug.LogError("[KerbalismFFT] " + msg);
		}

		public static void UpdateKerbalismPlannerUINow()
		{
			// Dirty hack - calling internal method from another assembly via reflection
			// Wish I could find another way to update Planner UI...
			// And no, onEditorShipModified event will not do the job, as it will also restart heat simulation process
			string className = typeof(Planner).AssemblyQualifiedName;
			ReflectionStaticCall(className, "RefreshPlanner");
		}

		public static void ReflectionStaticCall(string ClassName, string MethodName)
		{
			var staticClass = Type.GetType(ClassName);
			if (staticClass != null)
			{
				try
				{
					staticClass.GetMethod(MethodName, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
				}
				catch (Exception ex)
				{
					LogError("Static class method " + ClassName + "." + MethodName + " reflection call failed. Exception: " + ex.Message + "\n" + ex.ToString());
				}
			}
		}

		// Find PartModule snapshot (used for unloaded vessels as they only have Modules snapshots)
		public static ProtoPartModuleSnapshot FindPartModuleSnapshot(ProtoPartSnapshot p, string PartModuleName)
		{
			ProtoPartModuleSnapshot m = null;
			for (int i = 0; i < p.modules.Count; i++)
			{
				if (p.modules[i].moduleName == PartModuleName)
				{
					m = p.modules[i];
					break;
				}
			}
			if (m == null)
			{
				LogError($" Part [{p.partInfo.title}] No {PartModuleName} was found in part snapshot.");
			}
			return m;
		}
	}
}
