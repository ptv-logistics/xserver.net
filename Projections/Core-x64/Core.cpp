// Proj.4-Core.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Core.h"
#include <proj_api.h>

PROJ4CORE_API(void*) initProjection(const char* const wkt)
{
	return pj_init_plus(wkt);
}

PROJ4CORE_API(bool) isLatLon(const void* const hdl)
{
	return pj_is_latlong((projPJ)hdl) ? true : false;
}

PROJ4CORE_API(void) freeProjection(const void* const hdl)
{
	pj_free((projPJ)hdl);
}

PROJ4CORE_API(int) transformPoints(const void* const src, const void* const dst, const long point_count, const int point_offset, double* const x, double* const y, double* const z)
{
	return pj_transform((projPJ)src, (projPJ)dst, point_count, point_offset, x, y, z);
}

PROJ4CORE_API(int) transformSimplePoints(const void* const src, const void* const dst, const long point_count, const int point_offset, double* const x, double* const y)
{
	return pj_transform((projPJ)src, (projPJ)dst, point_count, point_offset, x, y, NULL);
}

PROJ4CORE_API(int) transformPoint(const void* const src, const void* const dst, double& x, double& y, double& z)
{
	return pj_transform((projPJ)src, (projPJ)dst, 1, 0, &x, &y, &z);
}

PROJ4CORE_API(int) transformSimplePoint(const void* const src, const void* const dst, double& x, double& y)
{
	return pj_transform((projPJ)src, (projPJ)dst, 1, 0, &x, &y, NULL);
}