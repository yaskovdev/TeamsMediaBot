#include "resampler_api.h"

resampler* resampler_create()
{
    return new resampler();
}

void resampler_write_frame(resampler* resampler, const uint8_t* frame, const int length)
{
    resampler->write_frame(frame, length);
}

uint8_t* resampler_read_frame(resampler* resampler, int* length)
{
    return resampler->read_frame(length);
}

void resampler_delete(const resampler* resampler)
{
    delete resampler;
}
