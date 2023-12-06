#include "resampler_api.h"

resampler* resampler_create()
{
    return new resampler();
}

void resampler_write_frame(resampler* resampler, const uint8_t* frame, const int length, const int timestamp)
{
    resampler->write_frame(frame, length, timestamp);
}

uint8_t* resampler_read_frame(resampler* resampler, frame_metadata* metadata)
{
    return resampler->read_frame(metadata);
}

void resampler_delete(const resampler* resampler)
{
    delete resampler;
}
