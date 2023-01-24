# NuGet packages and multi-platform support

Rx has tried a few strategies for cross-platform support over its history. The approach used in v5.0 turned out to cause problems for some UI frameworks: self-contained deployments would end up including copies of various WPF and Windows Forms components whether they used them or not, causing binaries to be tends of megabytes larger than they otherwise would have been.

This ADR describes a change in approach designed to address this.

## Status

Proposed

## Context

Rx has always run on multiple .NET platforms. Some parts of Rx (notably how schedulers interact with threads) depend on functionality that is slightly different across different flavours of .NET. Several different strategies for dealing with have been tried over the years:

* In v1, Microsoft built multiple different versions of the Rx libraries for different platforms. (This was long before .NET Standard or even before Portable Class Libraries.)
* v2 was designed to take advantage of the (then new, now defunct) Portable Class Library (PCL) concept. Assemblies were in one of these categories:
  * A portable core
    * `System.Reactive.Interfaces` (in the `Rx-Interfaces`](https://www.nuget.org/packages/Rx-Interfaces/2.2.5) NuGet package)—originally conceived as an assembly that would stay on v2.0.0.0 indefinitely, containing the canonical definitions of core interfaces
    * `System.Reactive.Core` (in the [`Rx-Core`](https://www.nuget.org/packages/Rx-Core/2.2.5) NuGet package)—platform-independent schedulers, and utility types for implementing Rx operators
    * `System.Reactive.Linq` (in the [`Rx-Linq`](https://www.nuget.org/packages/Rx-Linq/2.2.5) NuGet package)—implementations of LINQ operators for Rx
    * The [v2.0 beta announcement](http://web.archive.org/web/20130522225545/http://blogs.msdn.com/b/rxteam/archive/2012/03/12/reactive-extensions-v2-0-beta-available-now.aspx) mentions `System.Reactive.Providers`—expression tree support for Rx LINQ—but I don't see this on NuGet, so I'm not sure if it got rolled into something else
  * Platform-specific services, just one component, `System.Reactive.PlatformServices`, but the `Rx-PlatformServices` package contained builds for multiple target platforms including:
    * .NET Framework 4.0
    * .NET Framework 4.5
    * Silverlight 5
    * Windows Phone (7, 7.1, and 8)
    * PCL (various profiles, covering .NET Framework, Silverlight, and several versions of Windows Phone)
  * UI-framework-specific functionality
    * `System.Reactive.Windows.Threading` (in the [`Rx-Xaml`](https://www.nuget.org/packages/Rx-Xaml/2.2.5) NuGet package)—(WPF and WinRT schedulers and extension methods)
    * `System.Reactive.Windows.Forms` (in the [`Rx-WinForms`](https://www.nuget.org/packages/Rx-WinForms/2.2.5) NuGet package)—(Windows Forms schedulers and extension methods)
    * `System.Reactive.WindowsRuntime` (in the [`Rx-Metro`](https://www.nuget.org/packages/Rx-Metro/2.2.5) NuGet package)—(conversions between WinRT types and)
* v3 changed the NuGet package names, but essentially kept the same structure
  * The old `Rx-Main` metapackage is replaced by `System.Reactive`
  * `Rx-Interfaces` becomes `System.Reactive.Interfaces` (v3.0, not clear if there were actually interface changes)
  * `Rx-Core` becomes `System.Reactive.Core`
  * `Rx-Linq` becomes `System.Reactive.Linq`
  * `Rx-PlatformServices` becomes `System.Reactive.PlatformServices`
  * Likewise, the framework-specific libraries `System.Reactive.Windows.Threading`, `System.Reactive.Windows.Forms`, and `System.Reactive.WindowsRuntime` move into eponymous packages
* v4 made a major change: everything is now in a single `System.Reactive` package
* v5 stuck with the same structure as v4

The historical split between `System.Reactive.Windows.Threading` and `System.Reactive.WindowsRuntime` is a little baffling. It's not clear why some WinRT stuff ended up in its own library, and some shared a library with WPF.

Up as far as v2, the `System.Reactive.PlatformServices` was based on a concept called "platform enlightenments". The purpose of this was to avoid "lowest common denominator" problems. The portable core provided various schedulers, but some of these would be suboptimal on certain target platforms. Some platforms offered APIs that enabled better implementations, but because portable class libraries can access only the functionality common to their set of targets, these core libraries could not exploit that. However the core libraries provided an extensibility point where other libraries could, at runtime, plug in more specialized implementations suitable to the target platform that would provide better performance.

The major change was in v4. On the face of it, this was a case of merging all the separate components into a single one, meaning you need just a single NuGet package reference. However, the history behind this is more complex than it may first appear.

### History behind the decision to unify `System.Reactive` in v4

In v3, the core components contained both `net40` and `net45` targets. This could create an interesting problem in .NET applications that offered a plug-in model (e.g., Visual Studio extensions) reported in [issue #97](https://github.com/dotnet/reactive/issues/97). Imagine two plug-ins:

* Plug-in A
  * Targets .NET Framework 4.0
  * Has a reference to `System.Reactive.Linq` v3.0
* Plug-in B 
  * Targets .NET Framework 4.5
  * Has a reference to `System.Reactive.Linq` v3.0

Suppose the host application runs in .NET 4.5, and it loads Plug-in A. This is OK because loading of .NET 4.0 components in a .NET 4.5 host process is supported. This in turn will load `System.Reactive.Linq` v3.0. Since Plug-in A was built for .NET 4.0, it will have a copy of the `System.Reactive.Linq.dll` that was in the `lib\net40` folder in the `System.Reactive.Linq` NuGet package. So it gets to use the version it was built for.

So far, so good.

Now suppose the host loads Plug-in B. It will also want to load `System.Reactive.Linq`, but the CLR will notice that it has already loaded that component. The critical detail here is that the `System.Reactive.Linq.dll` files in the `lib\net40` and `lib\net45` folders have **the same name, public key token, and version number**. The .NET Framework assembly loader therefore considers them to have the same identity. So when Plug-in B comes to try to use `System.Reactive.Linq.dll`, the CLR will just let it use the same one that was loaded on behalf of Plug-in A. But if Plug-in B is relying on any functionality specific to the `net45` version of that component, it will be disappointed, with runtime errors such as `MethodNotFoundException`.

This tends not to be an issue with modern versions of .NET because we have [`AssemblyLoadContext`]([https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext) to solve this very problem. But with .NET Framework, this was a serious problem.

In 2016, Rx attempted to address this in [PR #212](https://github.com/dotnet/reactive/pull/212) by giving a slightly different version number to each target. For example, the `net45` DLL had version 3.0.1000.0, while the `net451` DLL was 3.0.2000.0, and so on. This plan is described in [issue #205](https://github.com/dotnet/reactive/issues/205).

At the time this was proposed, it was [acknowledged](https://github.com/dotnet/reactive/issues/205#issuecomment-228577028) that there was a potential problem with binding redirects. Binding redirects often specify version ranges, which means if you upgrade 3.x to 4.x, it's possible that 3.0.2000.0 would get upgraded to 4.0.1000.0, which could actually mean a downgrade in surface area (because the x.x.2000.0 versions might have target-specific functionality that the x.x.2000.0 versions do not).

These concerns turned out to be valid. [Issue #305](https://github.com/dotnet/reactive/issues/305) shows one example of the baffling kinds of problems that could emerge.

If I've understood correctly, part of the thinking here is that if all of Rx is in a single DLL, you won't have any problems in which different bits of Rx seems to disagree about which version is in use.

### Revisiting the decisions in 2023

There are two particularly significant factors that led to the decision in 2016 use per-target-framework version numbers (which in turn led to the decision to package Rx as a single DLL):

* There were builds for multiple .NET 4.x frameworks
* The `AssemblyLoadContext` was unavailable

The problem with .NET 4.x is that any component built for any .NET 4.x target could also end up loading on a later version of .NET 4.x (e.g., a `net40` component could load on `net462`) and once that happens in some process, that process will no longer be able to load a version of the same component that targets a newer framework. (E.g., once .NET 4.8 has loaded the `net40` build of some component, then it won't be able to go on to load the `net48` version of the same component, which is what leads us to wanting to give each build a slightly different name. That way they are technically difference components.)

But we only built a `net472` target now. So perhaps this problem has gone away.

One possible fly in the ointment is the `netstandard2.0` component, though. We could still end up with that loading first, blocking any attempt to load the `net472` version some time later. One thing we should ask is: does NuGet now do a better job of generating assembly binding redirects? If we had distinct version numbers for the `netstandard2.0` and `net472` would it now cope better?

When it comes to .NET 6.0 and later, this should all be a non-issue because better plug-in architectures exist: it's entirely possible for each plug-in to get its own components.


### Options

One possibility:
* .NET FX 4 build includes all WPF and Windows Forms code
* .NET 6.0 build has no code specific to WPF or Windows Forms
* UAP builds will contain UWP-specific code

Another:
* Just `netstandard2.0` component has a distinct version (it's the only thing that might conceivably get loaded into the same process as the .NET FX 4 component)

The reason for continuing to bundle WPF and Windows Forms code in the .NET FX 4 build is that the plug-in problem still exists: one might want the `netstandard2.0` version and the other the `net472` version


## Decision

TBD