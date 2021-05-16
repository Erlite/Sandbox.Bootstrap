using System;
namespace Sandbox.Bootstrap
{
	/// <summary>
	/// Used to generate a template for a bootstrapped addon to load.
	/// </summary>
	public class BootstrappedAddonBuilder
	{
		public string AssemblyPath { get; internal set; }
		public string LookupTypeName { get; internal set; }

		/// <summary>
		/// The path of the assembly to load.
		/// </summary>
		/// <param name="path"> The path of the assembly to load. </param>
		public BootstrappedAddonBuilder WithAssemblyPath( string path )
		{
			if (string.IsNullOrWhiteSpace( path ))
			{
				throw new ArgumentException( $"{nameof(path)} cannot be null or empty." );
			}
			
			AssemblyPath = path;
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
		
		/// <summary>
		/// Call to bootstrap the addon.
		/// </summary>
		public void Bootstrap()
		{
			AssertValid();

			Bootstrapper.GetOrCreate().Boot( this );
		}
		
		internal void AssertValid()
		{
			if (string.IsNullOrWhiteSpace( LookupTypeName ))
			{
				throw new InvalidOperationException( "You must call WithLookupType() before attempting to bootstrap an addon." );
			}

			if (string.IsNullOrWhiteSpace( AssemblyPath ))
			{
				throw new InvalidOperationException( "You must call WithAssemblyPath() before attempting to bootstrap an addon." );
			}
		}
	}
}
