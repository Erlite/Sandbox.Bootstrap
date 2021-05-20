using System;
using System.IO;
using System.Reflection;

namespace Sandbox.Bootstrap
{
    /// <summary>
    /// Handles rewriting bootstrapped assemblies to modify the simple name of referenced Sandbox addons.
    /// Allows a sanity check from CoreCLR to pass.
    /// </summary>
    public class BootstrapMonoCecil
    {
        private Assembly _sandboxMonoCecilAssembly;
        private Func<Stream, string[], AssemblyName[], Stream> _monocecil_ModifyAssemblyReference;

        internal bool Initialize()
        {
            BootstrapLog.Info("Initializing Sandbox.Bootstrap.MonoCecil.dll");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (_sandboxMonoCecilAssembly == null)
                {
                    return null;
                }

                if (args.RequestingAssembly!.FullName == _sandboxMonoCecilAssembly.FullName)
                {
                    var dnlib = FileSystem.Mounted.ReadAllBytes("bootstrap_monocecil/dnlib.dll");
                    return Assembly.Load(dnlib.ToArray());
                }

                return null;
            };
            
            try
            {
                var bootstrap = FileSystem.Mounted.ReadAllBytes("bootstrap_monocecil/Sandbox.Bootstrap.MonoCecil.dll");
                _sandboxMonoCecilAssembly = Assembly.Load(bootstrap.ToArray());
                
                _monocecil_ModifyAssemblyReference = _sandboxMonoCecilAssembly.GetType("Sandbox.Bootstrap.MonoCecil.AssemblyReferenceEditor")
                    ?.GetMethod("ModifyAssemblyReference", BindingFlags.Public | BindingFlags.Static)
                    ?.CreateDelegate<Func<Stream, string[], AssemblyName[], Stream>>(null);

                if (_sandboxMonoCecilAssembly == null || _monocecil_ModifyAssemblyReference == null)
                {
                    BootstrapLog.Error("Failed to bind to Sandbox.Bootstrap.MonoCecil.AssemblyReferenceEditor. Make sure your Sandbox.Bootstrap install is correct.");
                    return false;
                }
                
                BootstrapLog.Info("Succesfully loaded, ready to modify assembly references.");
                return true;
            }
            catch (Exception ex)
            {
                BootstrapLog.Error(ex, "An exception has occured while trying to bind to Sandbox.Bootstrap.MonoCecil.AssemblyReferenceEditor. Make sure your Sandbox.Bootstrap install is correct.");
                return false;
            }
        }
        
        internal Stream ModifyAssemblyReference(Stream assembly, string[] oldName, AssemblyName[] newName) => _monocecil_ModifyAssemblyReference.Invoke(assembly, oldName, newName);
    }
    
}