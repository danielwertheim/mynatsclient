using System.Reflection;

#if DEBUG
[assembly: AssemblyProduct("MyNatsClient (Debug)")]
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyProduct("MyNatsClient (Release)")]
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyDescription("MyNatsClient - a simple client for NATS.")]
[assembly: AssemblyCompany("danielwertheim")]
[assembly: AssemblyCopyright("Copyright © 2016 Daniel Wertheim")]