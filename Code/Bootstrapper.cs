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

			if (!_resolveLookup.ContainsKey( name.FullName ))
			{
				return null;
			}

			var lookupType = _resolveLookup[name.FullName];

			foreach (var assembly in _bootstrapInterface.GetSandboxAssemblies())
			{
				if (assembly.DefinedTypes.Any( t => t.FullName != null && t.FullName == lookupType ))
				{
					return assembly;
				}
			}

			return null;
		}

		public void Boot( BootstrappedAddonBuilder bootstrapBuilder )
		{
			if (bootstrapBuilder == null)
			{
				throw new ArgumentNullException( nameof(bootstrapBuilder) );
			}
			
			bootstrapBuilder.AssertValid();
			
			// Get the name of the assembly we're trying to load.
			var name = string.Empty;
			try
			{
				AssemblyName.GetAssemblyName(bootstrapBuilder.
			}
			catch (Exception e)
			{
				Console.WriteLine( e );
				throw;
			}
		}
	}
}
