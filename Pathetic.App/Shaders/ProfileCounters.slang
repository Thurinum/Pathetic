// Counting helpers for the profiler. The including shader declares, before including this file, the counters:
//
//   struct MyCounters { uint Rays; ... };            (mirrored by a C# struct with the same fields)
//   RWStructuredBuffer<MyCounters> profileCounters;
//
// and then counts with PROFILE_ADD(Rays, 1). Everything compiles away unless PROFILE_COUNTERS is defined.
#ifdef PROFILE_COUNTERS

// One atomic per wave instead of one per thread.
#define PROFILE_ADD(field, amount) \
    do { \
        uint profileSum = WaveActiveSum((uint)(amount)); \
        if (WaveIsFirstLane()) \
            InterlockedAdd(profileCounters[0].field, profileSum); \
    } while (false)

#define PROFILE_MAX(field, value) \
    do { \
        uint profileMax = WaveActiveMax((uint)(value)); \
        if (WaveIsFirstLane()) \
            InterlockedMax(profileCounters[0].field, profileMax); \
    } while (false)

// Adds the size of the wave once, so that (active lanes) / (slots) tells how well the wave is used.
#define PROFILE_WAVE_SLOTS(field) \
    do { \
        if (WaveIsFirstLane()) \
            InterlockedAdd(profileCounters[0].field, WaveGetLaneCount()); \
    } while (false)

#else

#define PROFILE_ADD(field, amount)
#define PROFILE_MAX(field, value)
#define PROFILE_WAVE_SLOTS(field)

#endif
