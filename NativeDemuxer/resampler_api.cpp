#include "resampler_api.h"

resampler* resampler_create()
{
    return new resampler();
}

uint8_t* resampler_resample_frame(resampler* resampler, const uint8_t* src_frame, const int src_length, frame_metadata* dst_metadata)
{
    return resampler->resample_frame(src_frame, src_length, dst_metadata);
}

void resampler_delete(const resampler* resampler)
{
    delete resampler;
}
