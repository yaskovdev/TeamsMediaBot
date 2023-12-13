#include "demuxer_api.h"
#include <iostream>

#include "demuxer.h"

demuxer* demuxer_create(const callback callback)
{
    return new demuxer(callback);
}

uint8_t* demuxer_read_frame(demuxer* demuxer, frame_metadata* metadata)
{
    return demuxer->read_frame(metadata);
}

void demuxer_frame_buffer_delete(const uint8_t* buffer)
{
    delete[] buffer;
}

void demuxer_delete(const demuxer* demuxer)
{
    delete demuxer;
}
