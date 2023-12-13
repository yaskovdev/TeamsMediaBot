#pragma once

#include "demuxer.h"
#include "common.h"

extern "C" __declspec(dllexport) demuxer* demuxer_create(callback callback);

extern "C" __declspec(dllexport) uint8_t* demuxer_read_frame(demuxer* demuxer, frame_metadata* metadata);

extern "C" __declspec(dllexport) void demuxer_frame_buffer_delete(const uint8_t* buffer);

extern "C" __declspec(dllexport) void demuxer_delete(const demuxer* demuxer);
