using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Sandbox.Bootstrap
{
	public class Bootstrapper
	{
		private static Bootstrapper _instance;
		private readonly BootstrapInterface _bootstrapInterface;
		private readonly BootstrapMonoCecil _bootstrapMonoCecil;

		public Bootstrapper()
		{
			_bootstrapMonoCecil = new BootstrapMonoCecil();
			_bootstrapInterface = new BootstrapInterface();
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
		
		public Assembly Boot( BootstrappedAddonBuilder bootstrapBuilder )
		{
			if (bootstrapBuilder == null)
			{
				throw new ArgumentNullException( nameof(bootstrapBuilder) );
			}
			
			BootstrapLog.Info($"Loading assembly at '{bootstrapBuilder.AssemblyName}'" );

			bootstrapBuilder.AssertValid();
			
			// Get the name of the assembly we're trying to load.
			var relativePath = $"bootstrapped/{bootstrapBuilder.AssemblyName}/{bootstrapBuilder.AssemblyName}.dll";
			var absolutePath = FileSystem.Mounted.GetFullPath(relativePath);

			if (absolutePath == null)
			{
				BootstrapLog.Error($"Could not find assembly '{relativePath}'");
				return null;
			}

			AssemblyName name;
			try
			{
				name = AssemblyName.GetAssemblyName( absolutePath );
				BootstrapLog.Info($"Found assembly '{name.FullName}' at '{relativePath}', attempting to load.");
			}
			catch (Exception e)
			{
				BootstrapLog.Error(e, $"Failed to load assembly '{bootstrapBuilder.AssemblyName}'.");
				return null;
			}

			BootstrapLog.Info($"Valid filesystem: {FileSystem.Mounted.IsValid}");
			Assembly asm;
			// Now we load the assembly.
			try
			{
				using var dllFile = FileSystem.Mounted.OpenRead( relativePath );
				
				// Before loading, grab all of the dynamic addons currently loaded, and ask Sandbox.Bootstrap.MonoCecil to modify them. 
				BootstrapLog.Info("[Sandbox.Bootstrap.MonoCecil] Attempting to override references.");
				var oldReferences = new List<string>();
				var newReferences = new List<AssemblyName>();
				
				foreach (var sboxAssembly in _bootstrapInterface.GetSandboxAssemblies())
				{
					if (sboxAssembly != null)
					{
						var asmName = sboxAssembly.GetName();
						var split = asmName.Name!.Split('.');
						if (split.Length > 1 && split[0] == "Dynamic")
						{
							oldReferences.Add(split[1]);
							newReferences.Add(asmName);
						}
					}
				}
				
				var output = _bootstrapMonoCecil.ModifyAssemblyReference(dllFile, oldReferences.ToArray(), newReferences.ToArray());
				asm = _bootstrapInterface.GameAssemblyManager_LoadContext_LoadFromStream(output);
			}
			catch (Exception e)
			{
				BootstrapLog.Error(e, $"Failed to load assembly '{name.FullName}' at '{bootstrapBuilder.AssemblyName}'");
				return null; 
			}
			
			// Woohoo we did it.
			BootstrapLog.Info($"Successfully loaded assembly '{asm.FullName}'." );
			bootstrapBuilder.OnAssemblyLoaded?.Invoke( asm );
			return asm;
		}
		
		private void Initialize()
		{
			BootstrapLog.Info("Initializing.");
			_bootstrapMonoCecil.Initialize();
			_bootstrapInterface.WrapReflection();
			
			try
			{
				var resolutionHandler = Delegate.CreateDelegate(typeof(Func<AssemblyLoadContext,AssemblyName,Assembly>), this, "ResolveBootstrappedAssembly");
				_bootstrapInterface.BindGameAssemblyManager_LoadContext_Resolving(resolutionHandler);
			}
			catch (Exception e)
			{
				Log.Error(e, e.Message + "\n" + e.StackTrace);
			}
		}
		
		private Assembly ResolveBootstrappedAssembly( AssemblyLoadContext loadContext, AssemblyName name)
		{
			BootstrapLog.Info($"Attempting to resolve assembly '{name.FullName}'");

			foreach (var assembly in _bootstrapInterface.GetSandboxAssemblies())
			{
				if (name.Name == assembly.GetName().Name)
				{
					BootstrapLog.Info($"Resolved with '{assembly.FullName}'");
					return assembly;
				}
			}

			BootstrapLog.Error($"Could not resolve assembly '{name.FullName}'.");
			return null;
		}
	}
}
