#include "demuxer_api.h"
#include <iostream>

#include "demuxer.h"

demuxer* create_demuxer(const callback callback)
{
    return new demuxer(callback);
}

uint8_t* demuxer_read_frame(demuxer* demuxer, frame_metadata* metadata)
{
    return demuxer->read_frame(metadata);
}

void delete_frame_buffer(const uint8_t* buffer)
{
    delete[] buffer;
}

void delete_demuxer(const demuxer* demuxer)
{
    delete demuxer;
}
