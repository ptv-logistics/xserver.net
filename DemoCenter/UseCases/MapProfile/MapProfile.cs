//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using Ptv.XServer.Controls.Map;


namespace Ptv.XServer.Demo.UseCases.MapProfile
{
    public class MapProfileUseCase
    {
        public void ChangeMapProfile(WpfMap wpfMap, string mapProfile)
        {
            if (mapProfile.Equals("default"))
                Reset(wpfMap);
            else
                wpfMap.XMapStyle = mapProfile;
        }

        public void Reset(WpfMap wpfMap)
        {
            wpfMap.XMapStyle = null;
        }
    }
}
