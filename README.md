PTV xServer .NET

Copyright (c) 2019 PTV Group, Karlsruhe, Germany.


What is PTV xServer .NET?
-------------------------

PTV xServer .NET is a Control for WPF and WinForms to build "slippy" maps for your application. 
The control is intended to work with PTV xMapServer for the basemap imagery, 
but it can also include gereric WMS and tile providers.


Important Notes
---------------

For license details, please refer to the file LICENSE.TXT, which should have 
been provided with this distribution.


Prerequisites
-------------

- PTV xServer.NET is built to run with the minimum target framework with WPF support, which is **.NET 3.5 Client Profile**.
- The source code is implemented with C# 7.0, which requires at least **Visual Studio 2017 Community Edition**.


External Resources
------------------

* [xserver.net-docs](https://ptv-logistics.github.io/xserver.net-docs) - API Documentation
* [Releases](https://github.com/ptv-logistics/xserver.net/releases) - Binaries
* [xserver.net-samples](https://github.com/ptv-logistics/xserver.net-samples/blob/master/README.md) - Code Samples and Demos
* [DemoCenter](https://xserverinternet.azurewebsites.net/xserver.net/) - PTV xServer.NET Demo Center
* [xserver.net on nuget](https://www.nuget.org/packages/Ptv.XServer.Net)


Release Notes
-------------

**Version 1.7.5.0**

  - Fixed unhandled exception for empty tiles.
  - Fixed initializing CoordinateReferenceSystem using just a WKT requires an ID to be set internally; Otherwise CoordinateTransformation using two such CoordinateReferenceSystem instances may end up in an identity transform.


**Version 1.7.4.0**

  - Fix for xServer 2.14 with empty profile
  - xServer-2 profile can be set using the XMapProfile property.


**Version 1.7.3.0 (2019/08/07)**

  - Fixed display for overview and mapgnifier map ('M' key).
  - Fixed toolbox registration for FormsMap by adding dependent framework assemblies to the nuget package.


**Version 1.7.1.0 (2019/06/19)**

  - Fixed crash when showing PTV traffic incidents.


**Version 1.7.0.0 (2019/01/02)**

  - Added ReferenceTime to xmap-1 tile provider
  - Added strong name key signing
  

**Version 1.6.0.0 (2018/08/14)**

  Fixes and Optimizations:
  - Code refactoring for C # 7.0
  - Optimized tile caching and pruning
  - Fixed rendering error if the Container-Control sets UseLayoutRounding
  - Some minor bugfixes
  - Code is now 100% warning-free(!)

  New features and breaking changes:
  - Added support for integration of XMap2 Layers, including Feature Layers
  - Added support for integration of Web Map Tile Services (WMTS)
  - The InifiniteZoom property (to avoid jitter at deep zoom levels)
    is now set to true by default. This setting should be compatible 
    with the previous setting. If you experience problems with your 
    custom layer implementation, you can set it back to false. 
    However, this property will be removed in future releases.
  - The MemoryPressureMode flag is ignored and marked as obsolete. 
  - The protocol for a provider returning tooltip information has changed. The provider now must implement
    IUntiledProviderWithMapObjects:GetImageStreamAndMapObjects().


**Version 1.5.0.0 (2016/04/18)**

  Bugfixes:
  - xMap ObjectInfos were still displayed after layer was disabled.
  - Incorrect Rendering for MapPolygon when InifiteZoom was activated.
  - MapRengtagle.GetEnvelope didn't consider the spatialReferenceId.
  - MapRectange.Equals() threw exception when rectangle was null.
  - XMapMetaInfo(baseUrl) didn't invoke the base constructor.

  Changes and Optimizations:
  - ConnectionLimit for ServicePointManager now isn't changed inside control classes.
  - Added china and world-map support for CompleteUrl().
  - Optimized tile loading and pruning.
  - Toolbox installer now also supports Visual Studio 2015.
  - Signing algorithm for SDK-Setup and DemoCenter.exe is now SHA256.


**Version 1.4.0.0 (2015/07/30)**

  Controls:
  - New interface IToolTips, which can be implemented by layers, to provide textual
    information around a dedicated location (commonly the mouse position).
    The UntiledLayer class implements this interface.
  
  - The layer responsible for showing content of Web Map Services is integrated into
    the Ptv.XServer.Controls.Map.dll. It allows re-projections of returned images to 
    achieve a matching of the content provided by accompanying layers.

  

**Version 1.3.0.0 (2014/11/21)**

  Controls:
  
  - The Map Controls are ready for access of PTV xServer internet, i.e. PTV xServer 
    functionality is available via Cloud.

  - New property XMapStyle in interface IMap added. It determines the coloring 
    of the background layer containing the town, street and areas like seas, 
    forests, industrial areas and so on. Internally, the provided style name is 
    textually extended to meet the requirements of the xServer configuration.

  - A new layer is implemented which shows the Here Satellite View. It is an 
    alternative to the still available, but no longer integrated Bing aerials 
    in DemoCenter.

  - At some certain zoom levels, the map images appeared blurred in former 
    versions; this bug is fixed.

  - The gadgets integrated in the Map Control (for example the layers gadget 
    or the zoom slider) do no longer raise any exception, when they are integrated 
    in a docking container, which may cause some re-initialization of these gadgets. 
    A proper releasing of all previously needed objects before re-initialization 
    prevents the exceptional situation.

  Visual Studio integration:
  
  - For WinForms and WPF different Map Controls are provided. In former versions, 
    only the WinForms map is integrated into the toolbox of Visual Studio. In the 
    new version, also the WPF Map is integrated.

  
**Version 1.2.0.0 (2013/06/12)**

  Breaking Changes

  - Map: IUntiledProvider.GetImageStream has been modified to support double 
    precision coordinates. 

    Unless a custom provider has been implemented in your application, this 
    change is of minor importance. Where a custom provider has been implemented,
    integer type casts may get you to the previous behaviour. This change was 
    made in order to provide extended 'deep zoom support' through the label 
    layer.

  - Projections: OGC_GEODECIMAL has been renamed to OG_GEODECIMAL to conform 
    with the PTV xServer naming. OGC_GEODECIMAL has been kept as an alias in the 
    Registry; however, Ptv.Components.Projections.CoordinateReferenceSystem.XServer 
    no longer defines OGC_GEODECIMAL as an attribute. In addition, some managed 
    transformations have been refactored. These refactoring remain imperceptible 
    if the transformations are properly accessed via CoordinateTransformation.Get.

  - XMap layers: Interface of class XMapLayerFactory has been refactored 
    concerning all variants of InsertXMapBaseLayers. Instead of specifying different 
    combinations of XMapServer settings in varying parameters, these settings are 
    concentrated in a new class XMapMetaInfo. An instance of this class is used to 
    specify the XMapServer settings, which can be reused in calls to methods 
    XMapLayerFactory.InsertRoadEditor and XMapLayerFactory.InsertPOI.

  Others

  - Introduced a utility class for calculating the distance between two points
  - Optimized performance for xSaaS access (or xServer https access, respectively)
  - Changed Shape Layer to simplify implementation of custom shape types
  - Enhanced TiledLayer-API for implementation of custom providers
  - Provided the ability to incorporate arbitrary xMap Server layers into the map. 
    A conceptual page in PTV xServer .NET's help file provides additional documentation.
  - Added three new samples demonstrating drag&drop routing, tooltip display for xMap
    Server content and different rendering approaches (tiled, non-tiled bitmap, vector)
  - Fixed a panning problem that occurred on lower zoom levels 
  - Fixed layer ordering issue when using ILayer.Priority
  - Complemented the interoperability "How Tos" with a C++/MFC sample
  - Added a new property 'LazyUpdate' to the Shape Layer to improve performance for
    a large number of complex shapes


**Version 1.1.0.0 (2013/03/11)**

  Breaking Changes

  - <none>
   
  Others

  - Added support for zoom levels up to level 23.
  - Improvements for Shape Layer API.
  - Added INotifyPropertyChanged for ILayer and INotifyCollectionChanged for LayersCollection.
  - Added support for xSaaS.
  - Added sample for ActiveX-Integration.
  - Some minor fixes for rendering glitches.


Version 1.0.0.0 (2012/12/18)

   First Release   
