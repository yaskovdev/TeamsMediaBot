#pragma once

#include "demuxer.h"
#include "common.h"

extern "C" __declspec(dllexport) demuxer* create_demuxer(callback callback);

extern "C" __declspec(dllexport) uint8_t* demuxer_read_frame(demuxer* demuxer, frame_metadata* metadata);

extern "C" __declspec(dllexport) void delete_frame_buffer(const uint8_t* buffer);

extern "C" __declspec(dllexport) void delete_demuxer(const demuxer* demuxer);
