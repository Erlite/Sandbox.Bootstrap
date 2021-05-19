# Sandbox.Bootstrap

An ongoing attempt at a bootstrapper that lets you load any assembly with dependencies on s&box servers. 

# Usage

* Create a solution that references Sandbox binaries.
* Add any addons it depends on as projects of the solution
* Reference whatever you want from Nuget/DLLs
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

# Ongoing Attempt

I got it to load the assembly correctly, currently working on getting CoreCLR to accept the resolved assembly.
![image](https://user-images.githubusercontent.com/25248023/118828840-e974ae80-b8bd-11eb-9c9b-3ff5a3f6b227.png)
