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

			// AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => 
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

			foreach (var sandboxAssembly in _bootstrapInterface.GetSandboxAssemblies())
			{
				var sboxName = sandboxAssembly.GetName();
				var split = sboxName.Name.Split('.');
				
				if (split.Length > 1 && split[1].ToLower() == name.Name)
				{
					return sandboxAssembly;
				}
			}

			BootstrapLog.Error($"Could not resolve assembly '{name.FullName}'.");
			return null;
		}

		public Assembly Boot( BootstrappedAddonBuilder bootstrapBuilder )
		{
			if (bootstrapBuilder == null)
			{
				throw new ArgumentNullException( nameof(bootstrapBuilder) );
			}
			
			BootstrapLog.Info($"Loading assembly at '{bootstrapBuilder.AssemblyName}'" );
			BootstrapLog.Info($"Assembly resolve lookup type: '{bootstrapBuilder.LookupTypeName}'" );

			bootstrapBuilder.AssertValid();
			
			// Get the name of the assembly we're trying to load.
			AssemblyName name;
			var path = $"./addons/sandbox_bootstrap/bootstrapped/{bootstrapBuilder.AssemblyName}/{bootstrapBuilder.AssemblyName}";
			try
			{
				name = AssemblyName.GetAssemblyName( $"{path}.dll" );
				BootstrapLog.Info($"Found assembly '{name.FullName}' at '{path}.dll', attempting to load.");
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
				using var dllFile = FileSystem.Mounted.OpenRead( $"/bootstrapped/{bootstrapBuilder.AssemblyName}/{bootstrapBuilder.AssemblyName}.dll" );
				using var writableStream = new MemoryStream();
				dllFile.CopyTo(writableStream);
				
				// Before loading, grab all of the dynamic addons currently loaded, and ask Sandbox.Bootstrap.MonoCecil to modify them. 
				BootstrapLog.Info("[Sandbox.Bootstrap.MonoCecil] Attempting to override references.");
				foreach (var sboxAssembly in _bootstrapInterface.GetSandboxAssemblies())
				{
					if (sboxAssembly != null)
					{
						var asmName = sboxAssembly.GetName();
						var split = asmName.Name!.Split('.');
						if (split.Length > 1 && split[0] == "Dynamic")
						{
							var modified = _bootstrapMonoCecil.ModifyAssemblyReference(writableStream, split[1], asmName);
							if (modified)
							{
								BootstrapLog.Info($"[Sandbox.Bootstrap.MonoCecil] Replaced '{split[1]}' with '{asmName.Name}'");
							}
						}
					}
				}

				asm = _bootstrapInterface.GameAssemblyManager_LoadContext_LoadFromStream(writableStream);
				Log.Info("All Refs");
				foreach (var refAsm in asm.GetReferencedAssemblies())
				{
					Log.Info(refAsm.Name);
				}
			}
			catch (Exception e)
			{
				BootstrapLog.Error(e, $"Failed to load assembly '{name.FullName}' at '{bootstrapBuilder.AssemblyName}'\n{e.StackTrace}");
				return null;
			}
			
			// Woohoo we did it.
			// TODO: Call an entry point on it perhaps?
			BootstrapLog.Info($"Successfully loaded assembly '{asm.FullName}'." );
			bootstrapBuilder.OnAssemblyLoaded?.Invoke( asm );
			return asm;
		}
	}
}
