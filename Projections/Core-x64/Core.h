#ifndef __PROJ4CORE_H__
#define __PROJ4CORE_H__

#ifdef PROJ4CORE_EXPORTS
#define PROJ4CORE_API(TYPE) __declspec(dllexport) TYPE __stdcall
#else
#define PROJ4CORE_API __declspec(dllimport) __stdcall
#endif

PROJ4CORE_API(void*) initProjection(const char* const wkt);
PROJ4CORE_API(bool) isLatLon(const void* const hdl);
PROJ4CORE_API(void) freeProjection(const void* const hdl);
PROJ4CORE_API(int) transformPoints(const void* const src, const void* const dst, const long point_count, const int point_offset, double* const x, double* const y, double* const z);
PROJ4CORE_API(int) transformSimplePoints(const void* const src, const void* const dst, const long point_count, const int point_offset, double* const x, double* const y);
PROJ4CORE_API(int) transformPoint(const void* const src, const void* const dst, double& x, double& y, double& z);
PROJ4CORE_API(int) transformSimplePoint(const void* const src, const void* const dst, double& x, double& y);

#endif //__PROJ4CORE_H__