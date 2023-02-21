# Rx Roadmap for 2023

As of February 2023, `System.Reactive` (aka Rx.NET, Rx for short in this document) has become moribund. This document describes the plan for bringing it back to life.

## Current problems

There are five main problems we want to address. First, there have been no recent releases. Second, the build has fallen behind current tooling. Third, Rx causes unnecessary bloat if you use it in conjunction with certain modern build techniques such as self-contained deployments and ahead-of-time compilation. Fourth, the backlog of issues has been neglected. Fifth, the asynchronous Rx code in this repo is in an experimental state, and has never been released in any form despite demand.

### No recent releases

This table shows the most recent releases of the various libraries in this repo:

| Library | Version | Date |
|---|---|---|
| `System.Reactive` (Rx) | [5.0.0](https://github.com/dotnet/reactive/releases/tag/rxnet-v5.0.0) | 2020/11/10 |
| `System.Interactive` (Ix) | [6.0.1](https://github.com/dotnet/reactive/releases/tag/ixnet-v6.0.1) | 2022/02/01 |
| `System.Linq.Async` | 6.0.1 | 2022/02/01 |
| `System.Reactive.Async` (AsyncRx) | None | None |

Note that the `System.Linq.Async` family of NuGet packages is built from the `Ix.NET.sln` repository, which is why it has exactly the same release date and version as `Ix`, and also why there is no distinct release for it in GitHub. It is not strictly an Rx feature, but then again neither is `System.Reactive`.

Note that for years, Rx had been on its own series of version numbers, and it was largely coincidence that it happened to be on v4 shortly before .NET 5.0 was released. However, the Rx.NET 5.0.0 release declares itself to be "part of the .NET 5.0 release wave." This has led to the unfortunate perception that there should have been a new release of Rx for each version of .NET, with matching version numbers. There is no technical requirement for this—Rx 5.0.0 works just fine on .NET 7.0. The real issue here is just that development has stopped—there are bugs and feature requests, but there has been no new Rx release for well over 2 years.

### Build problems on current tooling

The tools used to build Rx—notably the .NET SDK and Visual Studio—evolve fairly quickly, with the effect that if you install the latest versions of the tools and open up the `System.Reactive.sln`, you will not get a clean build. Problems include:

* VS complains that the project targets versions of .NET that are not installed, unless you install out-of-support .NET Core SDKs (going back to .NET Core 2.1)
* If you do install the older .NET and .NET Core SDKs, Visual Studio will emit warnings telling you that the relevant frameworks are out of support and will not receive security updates
* There are numerous problems with the UWP tests:
  * The project enabling tests to execute on UWP (`Tests.System.Reactive.Uwp.DeviceRunner.csproj`) won't load due to incompatibilities with the version of `Coverlet.Collector` in use
  * If you remove the reference to `Coverlet.Collector`, Visual Studio reports that the UWP test project's references to the `System.Reactive` and `Microsoft.Reactive.Testing` projects are not allowed (with no explanation as to why) although it appears to build anyway
  * When you attempt to run the unit tests in Visual Studio's Test Explorer, none of the UWP tests run—they all just show a blue exclamation mark, with no explanation provided as to why they were not run
  * Rx uses the [xUnit.net](https://xunit.net/) unit testing tools, and it appears that the UWP support in these tools no longer works—if you follow the [xUnit instructions for creating a brand new UWP project from scratch and adding test](https://xunit.net/docs/getting-started/uwp/devices-runner) it does not work;  it appears that the only update to the [xUnit UWP test runner](https://github.com/xunit/devices.xunit/commits/master) since November 2019 was a change to the logo; in fact the last release was in January 2019 (back when Visual Studio 2017 was still the latest version), with almost all subsequent updates to the repo being dependabot updates rather than new development
* Some of the certificates used for building certain tests have expired
* There are some warnings relating to C#'s nullable reference types feature
* Newer analyzer rules cause a huge number of warnings and diagnostic messages to appear
* Visual Studio modifies some Xamarin-related auto-generated files due to changes in the tool versions

### Unnecessary bloat with certain build techniques

If you use .NET to build a desktop application, and if your create a self-contained deployments, adding a reference to Rx can cause tens of megabytes of components to be added to your application unnecessarily. This has caused problems for the Avalonia team (see https://github.com/AvaloniaUI/Avalonia/issues/9549) with the result that they have removed Rx from their codebase (see https://github.com/AvaloniaUI/Avalonia/pull/9749).

There are a few instances in which projects have effectively built in their own miniature versions of Rx because of the problems that can occur when using the real `System.Interactive`.

The basic problem is that the current Rx codebase makes WPF and Windows Forms support available to any application that targets a form of .NET in which those frameworks are available.

It might not be obvious why that's a problem. In fact, for .NET Framework it's not a problem: if you target .NET Framework, you will be deploying your application to a machine that already has all of the WPF and Windows Forms components, because those are installed as an integral part of .NET Framework. So there's no real downside to Rx baking in its support—the UI-framework-specific code accounts for a tiny fraction of the overall size of Rx, and it could be argued that the complexity of separating support for these libraries out into separate libraries offers no meaningful component size benefits.

The first few versions of .NET Core did not offer Windows Forms and WPF. However, .NET Core 3.0 brought these frameworks to the .NET Core lineage, and they continue to be available in the latest (non-.NET-Framework) versions of .NET. This is when baking support for them into Rx becomes problematic, because there are often good reasons to include copies of runtime libraries in application installers. Taking a dependency on WPF can cause a copy of all of the WPF libraries to be included as part of the application.

Rx's NuGet packages includes both `net5.0` and `net5.0-windows` targets. The `net5.0` one does not include the Windows Forms or WPF Rx features, whereas the `net5.0-windows` one does. Any application that targets `net5.0-windows` will end up with the `net5.0-windows` flavour of Rx, which means it will end up with dependencies on WPF and Windows Forms. If an application running on Windows uses some other UI framework such as Avalonia, it will still target `net5.0-windows` which means it will end up with a dependency on both WPF and Windows Forms despite using neither. And in either self-contained deployments, or certain ahead-of-time scenarios, this means those libraries will get deployed as part of the application, and they are tens of megabytes in size!

 
### Unaddressed backlog of issues

There are over 101 items on the Issues list. These need to be triaged, and a roadmap formed that will guide if and when to address them.

 
### AsyncRx.NET unreleased

The experimental AsyncRx.NET project was added to the repository in 2017. There has not yet been a release (even a preview) of these libraries. A lot of people would like to use them.


## UWP is a problem

Before proceeding with proposals, it's important to understand how many problems UWP causes, and also to understand why Rx still supports it despite the problems.

The [Universal Windows Platform, UWP](https://learn.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide) was originally conceived as a way to build applications that would run on not just on the normal editions of Windows (specifically, Windows 10, at the time UWP was introduced) but also various other Windows 10 based operating systems including XBox, HoloLens and the (defunct) Windows 10 Mobile platforms.

.NET on UWP is a bit strange. It uses neither the old .NET Framework, nor the current .NET runtime. UWP has its origins in the older Windows 8 store app platform (briefly but memorably called Metro), which had its very own CLR. The Windows 8 versions had TFMs of `netcore`, `netcore45` and `netcore451` (not to be confused with the `netcoreappX.X` family of TFMs used by .NET Core), and UWP recognizes the `netcore50` moniker (which has absolutely nothing to do with .NET 5.0) although this is considered deprecated, with `uap`, `uap10.0` or SDK-version-specific forms such as `uap10.0.16299` being preferred. This particular lineage of .NET supported ahead-of-time (AoT) compilation long before it came to more mainstream versions of .NET (mainly because the early ARM-based Windows 8 devices needed AoT to deliver acceptable app startup time). It also has a load of constraints arising from the goal for Windows Store Apps to be 'trusted'. Certain fairly basic functionality such as creating new threads was off limits, to prevent individual applications from having an adverse impact on the overall performance or power consumption of small devices.

This causes problems for libraries that need to do things with threads, with Rx's schedulers being a prime example. What's the `NewThreadScheduler` supposed to do on a platform that doesn't allow applications to create new threads directly? There are workarounds, such as using the thread pool, but those workarounds are suboptimal if you're running on unrestricted platforms. This leads to a choice between using an unsatisfactory lowest common denominator, or somehow arranging for different behaviour on UWP vs other platforms.

Microsoft's own [guidance for writing Windows apps](https://learn.microsoft.com/en-us/windows/apps/get-started/) strongly de-emphasizes UWP. In the version of that page visible in February 2023, It is hiding as the fourth and final tab in the "Other app types" section, and if you show that tab, it ends by warning you that you will not have access to APIs in the newer Windows App SDK, and encourages you to use WinUI instead of UWP. It even goes as far as to encourage you to migrate your UWP app to WinUI 3.

Nonetheless, UWP is still supported, and Microsoft continues to update the tooling for UWP. You can create UWP applications in Visual Studio 2022. They ensure that many libraries and tools (including their own MSTest test framework) operate correctly when used with UWP. However, it is very much end-of-lifecycle support: everything works, but nothing is being updated. In particular, the project system for .NET UWP apps has not been updated to use the modern [.NET project SDK](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview) system that all other .NET projects have.

It's this lack of .NET project SDK support that makes UWP such a problem for multi-target libraries such as Rx.

The tools supplied by Microsoft provide do not provide a good way of writing multi-target projects that create NuGet package targeting more normal target framework monikers (TFMs) such as `netstandard2.0` or `net6.0` and also UWP-specific TFMs such as `uap10.0`. The only practical way to do this is to use the [MSBuildSdkExtras](https://github.com/novotnyllc/MSBuildSdkExtras) project developed by Claire Novotny. That project does a remarkable job of getting the tools to do something they have don't really want to do, but it can never do a perfect job, because it is trying to provide functionality that should really be intrinsic to the tooling. For example, the [MSBuildSdkExtras readme](https://github.com/novotnyllc/MSBuildSdkExtras/tree/b58e1d25b530e02ce4d1b937ccf99082019cdc47#important-to-note) notes that you can't run `dotnet build` at the command line if you're using this; you have to run MSBuild instead.

The lack of official support for UWP in a .NET project SDK world is also behind at least one of the build problems described above. Fundamentally, getting UWP to play in this world requires some hacks, and as a result, a few things don't quite work properly.

UWP is also not widely supported in the .NET ecosystem. It does not appear to be possible to use  xUnit with UWP on current versions of Visual Studio. We appear to have only two options: drop support for UWP, or move from xUnit to MSTest.

So why on earth are we supporting UWP if Microsoft is strongly discouraging its use, and pushing people onto WinUI instead?

### Why UWP in 2020?

Back when Rx 5.0 was released, the need for UWP support was stronger, because the WinUI libraries being recommended now in 2023 were relatively new. UWP was the most practical option for building Windows applications in .NET that made use of features that Windows exposes through its WinRT-style APIs. For example, if you wanted to write a .NET app that could be offered through the Windows Store, and which made use of the camera, you had to use UWP.

Since UWP was the only practical option for many scenarios, and since a lowest common denominator approach (such as a single .NET Standard 2.0 library with no special UWP support) would produce a suboptimal experience for some platforms, or possibly a broken experience on UWP, building UWP-specific targets for Rx was the best option.

### Why UWP in 2023?

Over two years have passed since Rx 5.0 was released. Microsoft actively discourages the use of UWP, so why do we still target it? Well one obvious answer is that there has been no new release of Rx in the intervening time. However, it's not as simple as that.

Although Microsoft pushes people strongly in the direction of WinUI v3, the fact is that if you want to write a .NET Windows Store application today, certain Windows features are only available to you if you target UWP. For example, full access to the camera (including direct access to the raw pixels from captured images) is currently (February 2023) only possible with UWP. The OCR (optical character recognition) features built into Windows are also only accessible to .NET applications if you target UWP.

We would dearly love to rip out all of the UWP-specific code in Rx, because it would simplify the build and fix a lot of problems. But since developers still sometimes find themselves with no option but to use UWP, Rx needs to continue to work on UWP. Ideally we would do both: provide full support for UWP but with all UWP-specific code separated out of the main solution. But regardless of how we achieve it, one way or another we need to continue to provide support for running on UWP for at least the next couple of versions.

## Plans for upcoming release

The question is whether we try to solve all of the problems in one step, or whether we produce any interim releases that get us part way to a solution. Our current view is that we will not attempt to do everything at once. We would like to get automated tests running against .NET 6.0 and .NET 7.0 in place as soon as possible so that we can verify that these platforms are supported. And we would like to make preview builds of AsyncRx.NET available as quickly as possible.

The issues around UI framework dependencies are arguably trickier, because it's not obvious what is the best way to solve them. One option is to return to something resembling the "platform enlightenments" design in which we minimize the number of different target platforms supported by the core libraries. (An extreme option would be for the main Rx components to target only .NET Standard 2.0.) However, there are various subtle issues surrounding this, and we would need to be certain that we were not re-introducing the old bug (https://github.com/dotnet/reactive/issues/97) in which plug-in hosts (notably Visual Studio) could load different plug-ins that had different opinions over whether to load the `netstandard2.0` or the `net4x` flavour or Rx, resulting in unresolvable conflicts and runtime errors. We don't want to delay everything else while we experiment with strategies for this problem, so although we want to resolve these particular problems as soon as possible, we want to fix the other sooner.

The first priority has to be to update the build so that it is possible to work on Rx with current development tools. We can't fix the other issues if we can't work on Rx. This will entail removing older, unsupported targets. So the next drop is likely to target:

* `net6.0` (with test projects also targetting `net7.0`)
* `net6.0-windows.XXXXX` (TBD: should it be `net6.0-windows.18362`, the oldest TFM we can target with VS 2022, which corresponds to Windows 10 version 1903, which shipped in May of 2019 and went out of support in November of 2020, or `net6.0-windows.10.0.20348` because that's the oldest SDK version that is supported by all versions of Windows 10 and Windows 11 that are currently still in support)
* `net472`
* `netstandard2.0`
* `uap10.0.XXXXX` (again, TBD exactly which SDK version, with 18362 and 20348 being the obvious candidates depending on whether we want "oldest technically possible" or "newest while still targetting all applicable versions of Windows that are still in support")
