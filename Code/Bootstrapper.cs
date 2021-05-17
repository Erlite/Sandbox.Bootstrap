using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Sandbox.Bootstrap
{
	public class Bootstrapper
	{
		private static Bootstrapper _instance;
		private readonly BootstrapInterface _bootstrapInterface;
		private Dictionary<string, Assembly> _bootstrappedAssemblies;
		private Dictionary<string, string> _resolveLookup;
		
		public Bootstrapper()
		{
			_bootstrapInterface = new BootstrapInterface();
			_bootstrappedAssemblies = new Dictionary<string, Assembly>();
			_resolveLookup = new Dictionary<string, string>();

			AppDomain.CurrentDomain.AssemblyResolve += ResolveBootstrappedAssembly;
		}

		/// <summary>
		/// Gets an instance of the Bootstrapper if it exists, or creates and initializes one if not.
		/// </summary>
		/// <returns></returns>
		public static Bootstrapper GetOrCreate()
		{
			if (_instance != null)
			{
				return _instance;
			}

			_instance = new Bootstrapper();
			_instance.Initialize();
			return _instance;
		}
		
		private void Initialize()
		{
			BootstrapLog.Info("Initializing.");
			_bootstrapInterface.WrapReflection();
		}
		
		private Assembly? ResolveBootstrappedAssembly( object? sender, ResolveEventArgs args )
		{
			if (args.RequestingAssembly == null)
			{
				return null;
			}

			var name = args.RequestingAssembly.GetName();
			// Only resolve assemblies that have been loaded from this bootstrapper.
			if (!_bootstrappedAssemblies.ContainsKey( name.FullName ))
			{
				return null;
			}
			
			BootstrapLog.Info($"Attempting to resolve aasembly '{args.Name}' for bootstrapped addon '{name.FullName}'");

			if (!_resolveLookup.ContainsKey( name.FullName ))
			{
				return null;
			}

			var lookupType = _resolveLookup[name.FullName];

			foreach (var assembly in _bootstrapInterface.GetSandboxAssemblies())
			{
				if (assembly.DefinedTypes.Any( t => t.FullName != null && t.FullName == lookupType ))
				{
					BootstrapLog.Info($"Resolved '{args.Name}' with '{assembly.FullName}'.");
					return assembly;
				}
			}

			BootstrapLog.Error($"Could not resolve assembly '{args.Name}'.");
			return null;
		}

		public Assembly Boot( BootstrappedAddonBuilder bootstrapBuilder )
		{
			if (bootstrapBuilder == null)
			{
				throw new ArgumentNullException( nameof(bootstrapBuilder) );
			}
			
			BootstrapLog.Info($"Loading assembly at '{bootstrapBuilder.AssemblyPath}'" );
			BootstrapLog.Info($"Assembly resolve lookup type: '{bootstrapBuilder.LookupTypeName}'" );

			bootstrapBuilder.AssertValid();
			
			// Get the name of the assembly we're trying to load.
			AssemblyName name;
			try
			{
				name = AssemblyName.GetAssemblyName( bootstrapBuilder.AssemblyPath );
			}
			catch (Exception e)
			{
				BootstrapLog.Error(e, $"Failed to load assembly '{bootstrapBuilder.AssemblyPath}'.");
				return null;
			}

			if (_bootstrappedAssemblies.ContainsKey( name.FullName ))
			{
				BootstrapLog.Error($"Could not load assembly '{name.FullName}': already loaded.");
				return _bootstrappedAssemblies[name.FullName];
			}
			
			// Make it null for now. On assembly resolve we'll check it's in the dictionary to handle it.
			_bootstrappedAssemblies.Add(name.FullName, null);

			// Shouldn't ever happen, but just in case.
			if (_resolveLookup.ContainsKey( name.FullName ))
			{
				_resolveLookup[name.FullName] = bootstrapBuilder.LookupTypeName;
			}
			else
			{
				_resolveLookup.Add(name.FullName, bootstrapBuilder.LookupTypeName);
			}

			Assembly asm;
			// Now we load the assembly.
			try
			{
				asm = Assembly.LoadFrom(bootstrapBuilder.AssemblyPath);
			}
			catch (Exception e)
			{
				BootstrapLog.Error(e, $"Failed to load assembly '{name.FullName}' at '{bootstrapBuilder.AssemblyPath}'");
				return null;
			}
			
			// Woohoo we did it.
			// TODO: Call an entry point on it perhaps?
			BootstrapLog.Info($"Successfully loaded assembly '{name.FullName}'." );
			bootstrapBuilder.OnAssemblyLoaded?.Invoke( asm );
			return asm;
		}
	}
}
