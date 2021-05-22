# Sandbox.Bootstrap

A bootstrapper that lets you load any assembly with dependencies on s&box servers. 

# Usage

* Create a solution that references Sandbox binaries.
* Add any addons it depends on as projects of the solution
* Reference whatever you want from Nuget/DLLs
* Compile the "bootstrapped" addon.

In your s&box addon:

```cs
var assembly = new BootstrappedAddonBuilder()
	.WithAssemblyPath( "MyBootstrappedAddon.dll" ) // The bootstrapped addon assembly.
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

`Hello, World!` is called from MyBootstrappedAddon.dll. MyBootstrappedAddon references my `devtest` gamemode which contains the HelloWorld() method. This goes to show this works, I'll be cleaning it up and making it dev-friendly soon.
![image](https://user-images.githubusercontent.com/25248023/119033340-6599ef80-b9ad-11eb-95e7-b859fc99e579.png)
