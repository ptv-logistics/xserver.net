PTV xServer .NET

Copyright (c) 2018 PTV Group, Karlsruhe, Germany.


What is PTV xServer .NET?
-------------------------

PTV xServer .NET is a supplement to the PTV xServer providing an abstraction 
and convenience layer for integrating PTV xServer into applications based on 
the Microsoft .NET Framework. 


Important Notes
---------------

For license details, please refer to the file LICENSE.TXT, which should have 
been provided with this distribution.


Prerequisites
-------------

- At least Windows Vista, Windows 7 or Windows 8 is recommended.

- At least Microsoft .NET 4.6.2 for building the corresponding binaries.

- At least Visual Studio C# 2008 Express Edition for developing applications
  based on PTV xServer .NET. With regards to the WPF integration and the 
  overall development experience we recommend at least Visual Studio 2010.

- All third-party libraries used by the SDK are included in the SDK package.


Release Notes 
-------------

Version 1.6.0.0 (2018/08/14)

  Bugfixes:
  - Some minor bugfixes
  - Fixed rendering error if the Container-Control set UseLayoutRounding

  Changes and Optimizations:
  - Added support for integration of Web Map Tile Services (WMTS)
  - Optimized tile caching and pruning
  - The InifiniteZoom property (to avoid jitter at deep zoom levels)
    is now set to true by default. This setting should be compatible 
    with the previous setting. If you experience problems with your 
    custom layer implementation, you can set it back to false. 
    However, this property will be removed in future releases.


Version 1.5.0.0 (2016/04/18)

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


Version 1.4.0.0 (2015/07/30)

  Controls:
  - New interface IToolTips, which can be implemented by layers, to provide textual
    information around a dedicated location (commonly the mouse position).
    The UntiledLayer class implements this interface.
  
  - The layer responsible for showing content of Web Map Services is integrated into
    the Ptv.XServer.Controls.Map.dll. It allows re-projections of returned images to 
    achieve a matching of the content provided by accompanying layers.

  

Version 1.3.0.0 (2014/11/21)

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

  
Version 1.2.0.0 (2013/06/12)

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


Version 1.1.0.0 (2013/03/11)

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


Known Issues of Current Release
-------------------------------

- Visual Studio versions have to be installed completely and have to be started
  once before running the PTV xServer .NET setup. Otherwise the toolbox installer
  which is integrated in the PTV xServer .Net setup will not work properly.

- Printing the code samples of the documentation via the provided link currently
  does not work.

- Changing the visibility of a map gadget at runtime has no effect on the map display.
  Currently, you can only change these settings at design time.

- Using the shape layer for a large number of dynamic objects (or complex objects) may 
  cause delays in the map's responsiveness. Unfortunately there is a limitation in 
  .NET's WPF layer that cannot be fixed to number. If you experience performance problems
  when using the shape layer we recommend to build an own rendering layer. Please refer
  to the documentation for additional information.
