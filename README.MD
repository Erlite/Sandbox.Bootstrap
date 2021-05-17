# Sandbox.Bootstrap

A failed attempt at a bootstrapper that lets you load any assembly with dependencies on s&box servers. 

# Usage

* Create a solution that references Sandbox binaries.
* Add any addons it depends on as projects of the solution
* Reference whatever you want from Nuget
* Compile the "bootstrapped" addon.

In your s&box addon:

```cs
var assembly = new BootstrappedAddonBuilder()
	.WithAssemblyPath( "MyBootstrappedAddon.dll" ) // The bootstrapped addon assembly.
	.WithLookupType( typeof(DevTestGame) ) // Used to resolve this addon assembly with the dynamically built one.
	.OnLoaded( asm =>
	{
		try
		{
			Activator.CreateInstance( asm.FullName!, "MyBootstrappedAddon.Class1" );
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to activate Class1");
		}
	})
	.Bootstrap();
```

# Why it failed.

Technically it works. As long as you don't reference any dynamically built s&box addon, as you are greeted with this once `Bootstrap.ResolveBootstrappedAssembly()` is raised:

```
Resolved 'devtest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' with 'Dynamic.Dev Test.103, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
System.NotSupportedException: Resolving to a collectible assembly is not supported.
```

So it works but without being able to reference s&box addons it's going to be a pain. I'll probably work more on it later.
