//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using Ptv.XServer.Controls.Routing;

namespace Ptv.XServer.Demo.UseCases.RoutingDragAndDrop
{
    class RoutingDragAndDropUseCase : UseCase
    {
        protected override void Enable()
        {
            wpfMap.Layers.Add(new RouteLayer(wpfMap));
        }

        protected override void Disable()
        {
            wpfMap.Layers.Remove(wpfMap.Layers["Route"]);
        }
    }
}
