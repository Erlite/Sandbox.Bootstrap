using System;
namespace Sandbox.Bootstrap
{
	internal static class BootstrapLog
	{
		private static readonly Logger _log;
		static BootstrapLog()
		{
			_log = new Logger( "Sandbox.Bootstrap" );
		}

		internal static void Info( string message ) => _log.Info( message );

		internal static void Warning( string message ) => _log.Warning( message );

		internal static void Error( Exception ex, string message ) => _log.Error( ex, message );
		
		internal static void Error( string message ) => _log.Error( message );
	}
}
