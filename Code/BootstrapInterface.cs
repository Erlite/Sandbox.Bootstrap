using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Sandbox.Bootstrap
{
	/// <summary>
	/// Handles wrapping Sandbox classes via Reflection.
	/// Mostly used to setup bootstrapped assemblies correctly so Sandbox just handles them like normal addons, or to retrieve currently loaded addons.
	/// </summary>
	internal class BootstrapInterface
	{
		private bool _wrapped = false;

		// Sandbox.Global.ContextInterface
		private object _contextInterface;
		// Sandbox.ContextInterface.OnNewAssembly(Assembly, Assembly)
		private Action<Assembly, Assembly> _onNewAssembly;
		
		// Sandbox.AssemblyLibrary.All
		private FieldInfo _assemblyLibrary_All;
		// Sandbox.AssemblyWrapper.Assembly
		private FieldInfo _assemblyWrapper_Assembly;
		
		/// <summary>
		/// Setup all of the required Reflection to interface with Sandbox.
		/// </summary>
		/// <exception cref="InvalidOperationException"> Thrown if any of the required Reflection calls fail. If this occurs, Sandbox likely got updated. Create an issue on our github repo. </exception>
		internal void WrapReflection()
		{
			if (_wrapped)
			{
				return;
			}

			_wrapped = true;
			BootstrapLog.Info("Wrapping Sandbox internals using Reflection.");
			
			// Retrieve Sandbox's context interface from the Global class.
			_contextInterface = typeof(Global).GetProperty( "GameInterface", BindingFlags.Static | BindingFlags.NonPublic )
				?.GetValue( null, null );

			if (_contextInterface == null)
			{
				BootstrapLog.Error( "Bootstrapper is out of date! Could not retrieve Global.GameInterface!" );
				return;
			}

			// Create a delegate for the ContextInterface.OnNewAssembly method.
			var original = Type.GetType( $"Sandbox.ServerInterface, {typeof(Global).Assembly.FullName}" )
			 	?.GetMethod( "OnNewAssembly", BindingFlags.NonPublic | BindingFlags.Instance );
			
			if (original == null)
			{
				BootstrapLog.Error("Bootstrapper is out of date! Could not bind to Sandbox.ContextInterface.OnNewAssembly(Assembly, Assembly)!" );
				return;
			}

			_onNewAssembly = original.CreateDelegate<Action<Assembly, Assembly>>(_contextInterface);
			
			// Retrieve Sandbox's AssemblyLibrary
			_assemblyLibrary_All = Type.GetType( $"Sandbox.AssemblyLibrary, {typeof(Global).Assembly.FullName}"  )
				?.GetField( "All", BindingFlags.Static | BindingFlags.Public );

			if (_assemblyLibrary_All == null)
			{
				BootstrapLog.Error("Bootstrapper is out of date! Could not bind to Sandbox.AssemblyLibrary.All!" );
				return;
			}
			
			// Retrieve Sandbox's AssemblyLibrary
			_assemblyWrapper_Assembly = Type.GetType( $"Sandbox.AssemblyWrapper, {typeof(Global).Assembly.FullName}" )
				?.GetField( "Assembly", BindingFlags.Public | BindingFlags.Instance );

			if (_assemblyWrapper_Assembly == null)
			{
				BootstrapLog.Error( "Bootstrapper is out of date! Could not bind to Sandbox.AssemblyWrapper.Assembly!" );
				return;
			}
		}

		internal List<Assembly> GetSandboxAssemblies()
		{
			if (_assemblyLibrary_All != null && _assemblyWrapper_Assembly != null)
			{
				if (!(_assemblyLibrary_All.GetValue( null ) is IList all))
				{
					throw new InvalidOperationException( "Could not get list of assemblies from AssemblyLibrary.All" );
				}
				
				var assemblies = new List<Assembly>( all.Count );
				foreach (var obj in all)
				{
					var assembly = (Assembly)_assemblyWrapper_Assembly.GetValue( obj );
					assemblies.Add(assembly);
				}

				return assemblies;
			}
			
			throw new InvalidOperationException( "Cannot get all Sandbox assemblies: wrappers are not bound." );
		}
	}
}
