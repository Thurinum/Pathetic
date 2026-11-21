#ifdef PROFILE_COUNTERS

RWStructuredBuffer<uint> profileCounters;
static uint threadCounts[PROFILE_COUNTER_COUNT];
groupshared uint groupCounts[PROFILE_COUNTER_COUNT];

bool IsMaxCounter(uint counter)
{
    return ((PROFILE_MAX_COUNTER_MASK >> counter) & 1u) != 0u;
}

void ProfileAdd(uint counter, uint amount)
{
    threadCounts[counter] += amount;
}

void ProfileMax(uint counter, uint value)
{
    threadCounts[counter] = max(threadCounts[counter], value);
}

void ProfileRay(uint bounce)
{
    ProfileAdd(PROFILE_Rays, 1);

    if (bounce == 1)
        ProfileAdd(PROFILE_RaysBounce1, 1);
    else if (bounce == 2)
        ProfileAdd(PROFILE_RaysBounce2, 1);
    else if (bounce > 2)
        ProfileAdd(PROFILE_RaysBounce3Plus, 1);
}

void ProfileBegin(uint groupIndex)
{
    for (uint i = 0; i < PROFILE_COUNTER_COUNT; ++i)
        threadCounts[i] = 0;

    if (groupIndex == 0)
    {
        for (uint i = 0; i < PROFILE_COUNTER_COUNT; ++i)
            groupCounts[i] = 0;
    }

    GroupMemoryBarrierWithGroupSync();
}

void ProfileEnd(uint groupIndex)
{
    uint waveSlots = WaveActiveMax(threadCounts[PROFILE_NodeVisits]) * WaveGetLaneCount();
    if (WaveIsFirstLane())
        InterlockedAdd(profileCounters[PROFILE_NodeVisitSlots], waveSlots);

    for (uint i = 0; i < PROFILE_COUNTER_COUNT; ++i)
    {
        if (IsMaxCounter(i))
            InterlockedMax(groupCounts[i], threadCounts[i]);
        else
            InterlockedAdd(groupCounts[i], threadCounts[i]);
    }

    GroupMemoryBarrierWithGroupSync();

    if (groupIndex != 0)
        return;

    for (uint i = 0; i < PROFILE_COUNTER_COUNT; ++i)
    {
        if (IsMaxCounter(i))
            InterlockedMax(profileCounters[i], groupCounts[i]);
        else
            InterlockedAdd(profileCounters[i], groupCounts[i]);
    }
}

#define PROFILE_ADD(name, amount) ProfileAdd(PROFILE_##name, amount)
#define PROFILE_MAX(name, value) ProfileMax(PROFILE_##name, value)
#define PROFILE_RAY(bounce) ProfileRay(bounce)

#else

void ProfileBegin(uint groupIndex) {}
void ProfileEnd(uint groupIndex) {}

#define PROFILE_ADD(name, amount)
#define PROFILE_MAX(name, value)
#define PROFILE_RAY(bounce)

#endif
