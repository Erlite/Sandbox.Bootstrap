using System;
using System.Reflection;
namespace Sandbox.Bootstrap
{
	/// <summary>
	/// Used to generate a template for a bootstrapped addon to load.
	/// </summary>
	public class BootstrappedAddonBuilder
	{
		internal string AssemblyName { get; set; }
		internal string LookupTypeName { get; set; }
		
		internal Action<Assembly> OnAssemblyLoaded { get; set; }

		/// <summary>
		/// The name of the assembly to load.
		/// .dll and .pdb files will need to be in sbox/bootstrapped/assemblyName/
		/// </summary>
		/// <param name="assemblyName"> The path of the assembly to load. </param>
		public BootstrappedAddonBuilder WithAssemblyName( string assemblyName )
		{
			if (string.IsNullOrWhiteSpace( assemblyName ))
			{
				throw new ArgumentException( $"{nameof(assemblyName)} cannot be null or empty." );
			}
			
			AssemblyName = assemblyName;
			return this;
		}

		/// <summary>
		/// The type to use as a lookup reference when resolving a normal Sandbox addon.
		/// It is recommended to feed in the "main" entry point of the addon/gamemode that will be loading the bootstrapped addon.
		/// </summary>
		/// <param name="type"> The type to use to resolve the calling assembly. </param>
		public BootstrappedAddonBuilder WithLookupType( Type type )
		{
			if (type == null)
			{
				throw new ArgumentNullException( nameof(type) );
			}
			
			LookupTypeName = type.FullName;
			return this;
		}

		public BootstrappedAddonBuilder OnLoaded( Action<Assembly> onLoaded )
		{
			OnAssemblyLoaded = onLoaded;
			return this;
		}
		
		/// <summary>
		/// Call to bootstrap the addon.
		/// </summary>
		public Assembly Bootstrap()
		{
			AssertValid();

			return Bootstrapper.GetOrCreate().Boot( this );
		}
		
		internal void AssertValid()
		{
			if (string.IsNullOrWhiteSpace( LookupTypeName ))
			{
				throw new InvalidOperationException( "You must call WithLookupType() before attempting to bootstrap an addon." );
			}

			if (string.IsNullOrWhiteSpace( AssemblyName ))
			{
				throw new InvalidOperationException( "You must call WithAssemblyPath() before attempting to bootstrap an addon." );
			}
		}
	}
}
