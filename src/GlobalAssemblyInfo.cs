using System.Runtime.InteropServices;
using System.Reflection;

#if DEBUG
[assembly: AssemblyProduct("MyNatsClient (Debug)")]
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyProduct("MyNatsClient (Release)")]
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyDescription("MyNatsClient - a simple client for NATS.")]
[assembly: AssemblyCompany("Daniel Wertheim")]
[assembly: AssemblyCopyright("Copyright © 2016 Daniel Wertheim")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.0.0.*")]
[assembly: AssemblyInformationalVersion("0.0.0.*")]